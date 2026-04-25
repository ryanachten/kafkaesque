using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;
using OrderService.Models;
using OrderService.Repositories;
using static Common.Constants;
using Common;
using Schemas;

namespace OrderService.Services;

public sealed class OrderFulfilledConsumer : IHostedService, IDisposable
{
    private IConsumer<string, OrderFulfilled>? _consumer;
    private IProducer<string, OrderFulfilled>? _deadLetterProducer;
    private readonly IServiceProvider _serviceProvider;
    private CachedSchemaRegistryClient? _schemaRegistryClient;
    private readonly ILogger<OrderFulfilledConsumer> _logger;
    private readonly CancellationTokenSource _tokenSource;
    private readonly ConsumerRetryConfiguration _retryConfig;
    private readonly KafkaConfiguration _kafkaConfig;
    private Task? _executingTask;

    public OrderFulfilledConsumer(
        IOptions<KafkaConfiguration> kafkaOptions,
        IOptions<ConsumerRetryConfiguration> retryOptions,
        IServiceProvider serviceProvider,
        ILogger<OrderFulfilledConsumer> logger)
    {
        _kafkaConfig = kafkaOptions.Value;
        _retryConfig = retryOptions.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _tokenSource = new CancellationTokenSource();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = _kafkaConfig.SchemaRegistryUrl
            };

            _schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

            var consumerConfig = new ConsumerConfig
            {
                GroupId = "order-service-fulfilled-consumer",
                BootstrapServers = _kafkaConfig.BootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                MaxPollIntervalMs = _retryConfig.MaxPollIntervalMs
            };

            _consumer = new ConsumerBuilder<string, OrderFulfilled>(consumerConfig)
                .SetValueDeserializer(new AvroDeserializer<OrderFulfilled>(_schemaRegistryClient).AsSyncOverAsync())
                .SetErrorHandler(OnErrorHandler)
                .Build();

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _kafkaConfig.BootstrapServers,
                Acks = Acks.All
            };

            var avroSerializerConfig = new AvroSerializerConfig
            {
                AutoRegisterSchemas = false,
                UseLatestVersion = true,
                SubjectNameStrategy = SubjectNameStrategy.Record
            };

            _deadLetterProducer = new ProducerBuilder<string, OrderFulfilled>(producerConfig)
                .SetValueSerializer(new AvroSerializer<OrderFulfilled>(_schemaRegistryClient, avroSerializerConfig))
                .Build();

            _consumer.Subscribe(Topics.OrderFulfilled);

            _executingTask = ExecuteAsync(_tokenSource.Token);

            _logger.LogInformation("OrderFulfilledConsumer started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to start OrderFulfilledConsumer: {Ex}", ex);
        }

        return Task.CompletedTask;
    }

    private void OnErrorHandler(IClient client, Error error)
    {
        if (error.IsFatal)
        {
            _logger.LogCritical("Fatal consumer error: {Reason}", error.Reason);
            return;
        }
        _logger.LogWarning("Consumer non-fatal error: {Reason}", error.Reason);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_tokenSource is not null)
            await _tokenSource.CancelAsync();

        if (_executingTask is not null)
        {
            await _executingTask;
        }
    }

    public void Dispose()
    {
        _consumer?.Dispose();
        _deadLetterProducer?.Dispose();
        _schemaRegistryClient?.Dispose();
        _tokenSource?.Dispose();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_consumer is null) return;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = _consumer.Consume(stoppingToken);
                    if (response?.Message?.Value is OrderFulfilled orderFulfilled)
                    {
                        var metadata = EventMetadata.FromKafkaHeaders(response.Message.Headers);
                        _logger.LogInformation(
                            "Received OrderFulfilled event for order {OrderShortCode}, EventId: {EventId}",
                            orderFulfilled.OrderShortCode,
                            metadata.EventId);

                        using var scope = _serviceProvider.CreateScope();
                        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

                        var alreadyProcessed = false;
                        try
                        {
                            alreadyProcessed = await orderRepository.IsStatus(orderFulfilled.OrderShortCode, OrderStatus.FULFILLED);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                "Failed to check order {OrderShortCode} status: {Ex}. Proceeding with normal processing",
                                orderFulfilled.OrderShortCode,
                                ex);
                        }

                        if (alreadyProcessed)
                        {
                            _consumer.Commit(response);
                            _logger.LogInformation(
                                "Order {OrderShortCode} already FULFILLED, skipping duplicate",
                                orderFulfilled.OrderShortCode);
                            continue;
                        }

                        var retryCount = 0;
                        var delay = _retryConfig.InitialRetryDelayMs;
                        var processedSuccessfully = false;

                        while (retryCount < _retryConfig.MaxRetryAttempts && !processedSuccessfully)
                        {
                            try
                            {
                                await orderRepository.UpdateStatus(orderFulfilled.OrderShortCode, OrderStatus.FULFILLED);
                                processedSuccessfully = true;
                            }
                            catch (Exception ex)
                            {
                                retryCount++;
                                if (retryCount >= _retryConfig.MaxRetryAttempts)
                                {
                                    _logger.LogError(
                                        "Failed to update order {OrderShortCode} after {Attempts} attempts: {Ex}. Sending to dead-letter queue",
                                        orderFulfilled.OrderShortCode,
                                        retryCount,
                                        ex);
                                    await SendToDeadLetterQueue(orderFulfilled, metadata, ex, response);
                                }
                                else
                                {
                                    _logger.LogWarning(
                                        "Failed to update order {OrderShortCode} (attempt {Attempt}/{MaxAttempts}): {Ex}. Retrying in {DelayMs}ms",
                                        orderFulfilled.OrderShortCode,
                                        retryCount,
                                        _retryConfig.MaxRetryAttempts,
                                        ex,
                                        delay);
                                    await Task.Delay(delay);
                                    delay = (int)(delay * _retryConfig.BackoffMultiplier);
                                    delay = Math.Min(delay, _retryConfig.MaxRetryDelayMs);
                                }
                            }
                        }

                        if (processedSuccessfully)
                        {
                            _consumer.Commit(response);
                            _logger.LogInformation(
                                "Updated order {OrderShortCode} to FULFILLED and committed offset",
                                orderFulfilled.OrderShortCode);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError("Error consuming message: {Message}", ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _consumer?.Close();
        }
    }

    private async Task SendToDeadLetterQueue(OrderFulfilled order, EventMetadata metadata, Exception exception, ConsumeResult<string, OrderFulfilled> response)
    {
        if (_deadLetterProducer is null) return;

        try
        {
            var dlqMetadata = new DeadLetterMetadata(
                Topics.OrderFulfilled,
                response.Partition.Value,
                response.Offset.Value,
                exception);

            var headers = metadata.ToKafkaHeaders();
            headers.AddDeadLetterHeaders(dlqMetadata);

            await _deadLetterProducer.ProduceAsync(Constants.DeadLetterTopics.OrderService, new Message<string, OrderFulfilled>
            {
                Key = order.OrderShortCode,
                Value = order,
                Headers = headers
            });

            _logger.LogWarning(
                "Sent order {OrderShortCode} to dead-letter queue at {DeadLetterTopic}",
                order.OrderShortCode,
                Constants.DeadLetterTopics.OrderService);
        }
        catch (Exception dlqEx)
        {
            _logger.LogCritical(
                "Failed to send order {OrderShortCode} to dead-letter queue: {Ex}",
                order.OrderShortCode,
                dlqEx);
        }
    }
}