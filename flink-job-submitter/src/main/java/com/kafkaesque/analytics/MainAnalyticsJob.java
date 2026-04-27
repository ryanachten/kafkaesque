package com.kafkaesque.analytics;

import Schemas.OrderPlaced;
import com.kafkaesque.analytics.model.WindowedMetric;
import com.kafkaesque.analytics.functions.OrderTimestampExtractor;
import com.kafkaesque.analytics.windows.TumblingWindow;

import org.apache.flink.connector.kafka.sink.KafkaRecordSerializationSchema;
import org.apache.flink.connector.kafka.sink.KafkaSink;
import org.apache.flink.connector.kafka.source.KafkaSource;
import org.apache.flink.connector.kafka.source.enumerator.initializer.OffsetsInitializer;
import org.apache.flink.formats.avro.registry.confluent.ConfluentRegistryAvroDeserializationSchema;
import org.apache.flink.formats.avro.registry.confluent.ConfluentRegistryAvroSerializationSchema;
import org.apache.flink.streaming.api.datastream.DataStream;
import org.apache.flink.streaming.api.environment.StreamExecutionEnvironment;
import org.apache.flink.streaming.api.windowing.assigners.TumblingEventTimeWindows;
import org.apache.flink.streaming.api.windowing.time.Time;
import org.apache.flink.api.common.eventtime.WatermarkStrategy;
import org.apache.flink.api.common.eventtime.SerializableTimestampAssigner;
import org.apache.flink.api.common.serialization.DeserializationSchema;
import org.apache.flink.api.common.serialization.SerializationSchema;
import org.apache.flink.api.java.utils.ParameterTool;

/**
 * MainAnalyticsJob is the entry point for the Flink stream processing job.
 *
 * In Flink, a Job is a directed acyclic graph (DAG) of transformations that
 * process data streams.
 * This job consumes order events from Kafka, processes them, and emits windowed metrics
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
 * - WatermarkStrategy: Strategy for handling event-time processing and late events
 * - TumblingEventTimeWindows: Fixed-size time windows that don't overlap
 * - AggregateFunction: Incremental aggregation function for efficient window computation
 * - ProcessWindowFunction: Called when a window fires to emit final results
 * - Checkpointing: Flink's mechanism for exactly-once state consistency
 *
 * This implementation uses event-time processing with watermarks for proper windowing.
 * Each order is assigned a timestamp from its event data, and watermarks track the progress
 * of time to determine when windows can fire.
 */
public class MainAnalyticsJob {

    /**
     * The main entry point - this is where the Flink job DAG is constructed.
     *
     * Think of this like Program.cs in an ASP.NET Core app - it's where you
     * configure the entire processing pipeline before calling Run().
     *
     * Pipeline flow:
     * 1. Create StreamExecutionEnvironment (runtime context)
     * 2. Configure KafkaSource to read from order.placed topic
     * 3. Apply WatermarkStrategy with timestamp extractor and tolerance
     * 4. Map orders to WindowAggregateAccumulator (count + revenue)
     * 5. Key by "all" (single partition for simplicity)
     * 6. Apply configured tumbling window
     * 7. Reduce (aggregate all accumulators in window)
     * 8. ProcessWindowFunction (calculate average, emit metric)
     * 9. Configure KafkaSink to write to order.analytics topic
     * 10. Execute the job
     *
     * @param args Command line arguments (Flink accepts job submission args here)
     * @throws Exception If the job fails to execute
     */
    public static void main(String[] args) throws Exception {
        ParameterTool params = ParameterTool.fromPropertiesFile("application.properties");

        String bootstrapServers = params.get("kafka.bootstrap.servers");
        String schemaRegistryUrl = params.get("kafka.schema.registry.url");
        String sourceTopic = params.get("flink.source.topic");
        String sinkTopic = params.get("flink.sink.topic");
        String groupId = params.get("flink.source.group.id");
        long checkpointInterval = params.getLong("flink.checkpoint.interval", 60000L);
        long watermarkInterval = params.getLong("flink.watermark.interval", 1000L);
        int watermarkToleranceSeconds = params.getInt("flink.watermark.tolerance.seconds", 60);
        int streamIdlenessMinutes = params.getInt("flink.stream.idleness.minutes", 1);
        int windowSizeMinutes = params.getInt("flink.window.size.minutes", 1);

        /**
         * Convert window size to label for metric output.
         * Maps config value to display label: 1->"1m", 60->"1h", 1440->"24h"
         */
        String windowSizeLabel = windowSizeMinutesToLabel(windowSizeMinutes);

        System.out.println("Starting Flink Analytics Job...");
        System.out.println("Kafka Source: " + sourceTopic + ", Sink: " + sinkTopic);
        System.out.println("Window size: " + windowSizeLabel);

        /**
         * Configure Avro serialization/deserialization using Confluent Schema Registry.
         * This allows schema evolution without breaking consumers/producers.
         */
        DeserializationSchema<OrderPlaced> orderPlacedDeserializer =
            ConfluentRegistryAvroDeserializationSchema.forSpecific(OrderPlaced.class, schemaRegistryUrl);
        SerializationSchema<WindowedMetric> windowedMetricSerializer =
            ConfluentRegistryAvroSerializationSchema.forSpecific(WindowedMetric.class, sinkTopic, schemaRegistryUrl);

        /**
         * StreamExecutionEnvironment is Flink's runtime context.
         * Similar to HostingEnvironment in ASP.NET Core - manages configuration,
         * checkpointing, parallelism, and execution of the entire pipeline.
         */
        StreamExecutionEnvironment env = StreamExecutionEnvironment.getExecutionEnvironment();

        /**
         * Enable checkpointing every 60 seconds for exactly-once state guarantees.
         * Checkpointing is Flink's transaction log - it snapshots state so the job can
         * resume from the last consistent point if it fails.
         */
        env.enableCheckpointing(checkpointInterval);

        /**
         * Set auto watermark interval from config.
         * Watermarks are emitted periodically to advance event time progress.
         */
        env.getConfig().setAutoWatermarkInterval(watermarkInterval);

        /**
         * Create KafkaSource using Flink 1.18's new unified connector API.
         * This replaces the deprecated FlinkKafkaConsumer.
         * Reads from the order.placed topic with the configured deserializer.
         */
        KafkaSource<OrderPlaced> source = KafkaSource.<OrderPlaced>builder()
                .setBootstrapServers(bootstrapServers)
                .setTopics(sourceTopic)
                .setGroupId(groupId)
                .setStartingOffsets(OffsetsInitializer.latest())
                .setValueOnlyDeserializer(orderPlacedDeserializer)
                .build();

        /**
         * Create timestamp extractor to extract event time from order data.
         * This is critical for event-time windowing - windows are triggered
         * based on event timestamps, not processing time.
         */
        SerializableTimestampAssigner<OrderPlaced> timestampAssigner = new OrderTimestampExtractor();

        /**
         * Configure WatermarkStrategy for event-time processing.
         *
         * - forBoundedOutOfOrderness: Allows events up to configured tolerance
         *   to be included in windows. Events later than this are considered "late"
         *   and would be dropped or sent to a side output.
         *
         * - withTimestampAssigner: Extracts the event timestamp from each order
         *
         * - withIdleness: If no events for configured duration, mark the stream
         *   as idle so watermarks can progress despite backpressure.
         *
         * This is crucial for accurate window aggregation when events may
         * arrive slightly out of order due to network delays.
         */
        WatermarkStrategy<OrderPlaced> watermarkStrategy = WatermarkStrategy
                .<OrderPlaced>forBoundedOutOfOrderness(java.time.Duration.ofSeconds(watermarkToleranceSeconds))
                .withTimestampAssigner(timestampAssigner)
                .withIdleness(java.time.Duration.ofMinutes(streamIdlenessMinutes));

        /**
         * Create the source DataStream from Kafka with watermarks.
         * The watermark strategy is applied to this source.
         */
        DataStream<OrderPlaced> orders = env.fromSource(source, watermarkStrategy, "Kafka Source");

        /**
         * Map each order to a WindowAggregateAccumulator.
         * This creates an accumulator with count=1 and total revenue from the order.
         * We use reduce() later to combine accumulators efficiently.
         */
        DataStream<TumblingWindow.WindowAggregateAccumulator> accumulations = orders
                .map(order -> TumblingWindow.toAccumulator(order));

        /**
         * Apply configured tumbling window aggregation.
         *
         * Key concepts:
         * - keyBy("all"): All orders go to single key (simple aggregation)
         * - TumblingEventTimeWindows.of(Time.minutes(N)): Configured window size
         *   that don't overlap
         * - Window fires when watermark passes window end + tolerance
         *
         * The reduce function combines accumulators incrementally as orders arrive.
         * The process function calculates final metrics when window fires.
         */
        DataStream<WindowedMetric> metrics = accumulations
                .keyBy(acc -> "all")
                .window(TumblingEventTimeWindows.of(Time.minutes(windowSizeMinutes)))
                .reduce(
                        new TumblingWindow.MetricReduceFunction(),
                        new TumblingWindow.MetricWindowFunction(windowSizeLabel)
                );

        /**
         * Create KafkaSink to write metrics to output topic.
         * Uses Confluent Schema Registry for Avro serialization.
         */
        KafkaSink<WindowedMetric> sink = KafkaSink.<WindowedMetric>builder()
                .setBootstrapServers(bootstrapServers)
                .setRecordSerializer(KafkaRecordSerializationSchema.builder()
                        .setTopic(sinkTopic)
                        .setValueSerializationSchema(windowedMetricSerializer)
                        .build())
                .build();

        metrics.sinkTo(sink);

        System.out.println("Job configured - submitting to Flink cluster");

        /**
         * Execute the job - this submits the DAG to the Flink cluster.
         * Think of this like Host.Run() in ASP.NET Core.
         * The job name appears in the Flink dashboard for identification.
         */
        env.execute("Flink Analytics Job - Real-time Order Metrics (" + windowSizeLabel + " window)");
    }

    /**
     * Convert window size in minutes to label string.
     *
     * @param windowSizeMinutes Window size in minutes (1, 60, 1440)
     * @return Label string: "1m", "1h", or "24h"
     */
    private static String windowSizeMinutesToLabel(int windowSizeMinutes) {
        if (windowSizeMinutes >= 1440) {
            return "24h";
        } else if (windowSizeMinutes >= 60) {
            return "1h";
        } else {
            return "1m";
        }
    }
}