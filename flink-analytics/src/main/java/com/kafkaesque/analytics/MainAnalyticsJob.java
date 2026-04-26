package com.kafkaesque.analytics;

import com.kafkaesque.analytics.model.OrderPlaced;
import com.kafkaesque.analytics.model.WindowedMetric;

import org.apache.flink.connector.kafka.sink.KafkaRecordSerializationSchema;
import org.apache.flink.connector.kafka.sink.KafkaSink;
import org.apache.flink.connector.kafka.source.KafkaSource;
import org.apache.flink.connector.kafka.source.enumerator.initializer.OffsetsInitializer;
import org.apache.flink.streaming.api.datastream.DataStream;
import org.apache.flink.streaming.api.environment.StreamExecutionEnvironment;
import org.apache.flink.streaming.api.functions.ProcessFunction;
import org.apache.flink.util.Collector;
import org.apache.flink.api.java.utils.ParameterTool;

import java.math.BigDecimal;
import java.math.RoundingMode;
import java.time.Duration;
import java.util.ArrayList;
import java.util.List;

/**
 * MainAnalyticsJob is the entry point for the Flink stream processing job.
 * 
 * In Flink, a Job is a directed acyclic graph (DAG) of transformations that
 * process data streams.
 * This job consumes order events from Kafka, processes them, and emits metrics
 * to another topic.
 * 
 * Flink Core Concepts Used:
 * - StreamExecutionEnvironment: The runtime context for executing Flink jobs
 * (similar to ASP.NET Core Host)
 * - DataStream: An infinite flow of data records that flows through the
 * processing pipeline
 * - KafkaSource: Flink's new connector for reading from Kafka (replaces
 * deprecated FlinkKafkaConsumer)
 * - KafkaSink: Flink's connector for writing to Kafka (using the new 1.18 API)
 * - ProcessFunction: Low-level operator for per-record processing (can emit
 * multiple outputs)
 * - Checkpointing: Flink's mechanism for exactly-once state consistency
 * 
 * Note: This is a simplified version using ProcessFunction for demonstration.
 * For production with proper windowing, use TumblingEventTimeWindows with keyed
 * streams.
 */
public class MainAnalyticsJob {

    /**
     * The main entry point - this is where the Flink job DAG is constructed.
     * 
     * Think of this like Program.cs in an ASP.NET Core app - it's where you
     * configure
     * the entire processing pipeline before calling Run().
     * 
     * @param args Command line arguments (Flink accepts job submission args here)
     * @throws Exception If the job fails to execute
     */
    public static void main(String[] args) throws Exception {
        ParameterTool params = ParameterTool.fromPropertiesFile("application.properties");

        String bootstrapServers = params.get("kafka.bootstrap.servers");
        String sourceTopic = params.get("flink.source.topic");
        String sinkTopic = params.get("flink.sink.topic");
        String groupId = params.get("flink.source.group.id");
        long checkpointInterval = params.getLong("flink.checkpoint.interval", 60000L);

        // StreamExecutionEnvironment is Flink's runtime context
        // Similar to HostingEnvironment in ASP.NET Core - manages configuration,
        // checkpointing, parallelism, and execution of the entire pipeline
        StreamExecutionEnvironment env = StreamExecutionEnvironment.getExecutionEnvironment();

        // Enable checkpointing every 60 seconds for exactly-once state guarantees
        // Checkpointing is Flink's transaction log - it snapshots state so the job can
        // resume from the last consistent point if it fails
        env.enableCheckpointing(checkpointInterval);

        // Create KafkaSource using Flink 1.18's new unified connector API
        // This replaces the deprecated FlinkKafkaConsumer
        KafkaSource<OrderPlaced> source = KafkaSource.<OrderPlaced>builder()
                .setBootstrapServers(bootstrapServers)
                .setTopics(sourceTopic)
                .setGroupId(groupId)
                .setStartingOffsets(OffsetsInitializer.earliest())
                .setValueOnlyDeserializer(new OrderPlacedDeserializer())
                .build();

        // Read from Kafka with no watermarks for now (simpler)
        // For event-time processing with windows, you'd add WatermarkStrategy here
        DataStream<OrderPlaced> orders = env.fromSource(source,
                org.apache.flink.api.common.eventtime.WatermarkStrategy.noWatermarks(), "Kafka Source");

        // Process orders using ProcessFunction
        // For production, use proper windowing (TumblingEventTimeWindows)
        DataStream<WindowedMetric> metrics = orders
                .process(new OrderAggregator());

        // Create KafkaSink to write metrics to output topic
        KafkaSink<WindowedMetric> sink = KafkaSink.<WindowedMetric>builder()
                .setBootstrapServers(bootstrapServers)
                .setRecordSerializer(KafkaRecordSerializationSchema.builder()
                        .setTopic(sinkTopic)
                        .setValueSerializationSchema(new WindowedMetricSerializer())
                        .build())
                .build();

        metrics.sinkTo(sink);

        // Execute the job - this submits the DAG to the Flink cluster
        // Think of this like Host.Run() in ASP.NET Core
        env.execute("Flink Analytics Job - Real-time Order Metrics");
    }

    /**
     * OrderAggregator processes orders and aggregates metrics.
     * 
     * ProcessFunction is Flink's lowest-level operator:
     * - Called for each incoming event
     * - Can emit zero or more output events
     * - Has access to timers for scheduled callbacks
     * - Has access to state for storing data across events
     * 
     * For production, use windowing with keyed streams instead of this simple
     * buffer.
     */
    private static class OrderAggregator extends ProcessFunction<OrderPlaced, WindowedMetric> {

        // Buffer to store orders - in production, use keyed state or RocksDB
        private transient List<OrderPlaced> ordersBuffer;

        @Override
        public void open(org.apache.flink.configuration.Configuration parameters) {
            ordersBuffer = new ArrayList<>();
        }

        /**
         * Process each incoming order.
         * 
         * @param order The incoming order from Kafka
         * @param ctx   Processing context (for timers, state, output)
         * @param out   Output collector for emitting metrics
         */
        @Override
        public void processElement(OrderPlaced order, Context ctx, Collector<WindowedMetric> out) throws Exception {
            ordersBuffer.add(order);

            // Log receipt (in production, use proper logging)
            System.out.println("Received order: " + order.getOrderId());

            // For demonstration - emit metrics every 10 orders
            // In production, use time-windowed tumbling windows with KeyedProcessFunction
            if (ordersBuffer.size() % 10 == 0) {
                long count = ordersBuffer.size();
                BigDecimal total = BigDecimal.ZERO;
                for (OrderPlaced o : ordersBuffer) {
                    if (o.getTotal() != null) {
                        total = total.add(o.getTotal());
                    }
                }

                BigDecimal avg = count > 0
                        ? total.divide(BigDecimal.valueOf(count), 2, RoundingMode.HALF_UP)
                        : BigDecimal.ZERO;

                long now = System.currentTimeMillis();

                WindowedMetric metric = new WindowedMetric(
                        now - 60000, // window start (1 minute ago)
                        now, // window end (now)
                        "1m", // window size identifier
                        count, // order count
                        total, // total revenue
                        avg, // average order value
                        now // processed timestamp
                );

                out.collect(metric);
            }
        }
    }
}