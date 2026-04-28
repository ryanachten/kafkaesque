package com.kafkaesque.analytics;

import com.kafkaesque.analytics.model.WindowedMetric;
import org.apache.flink.api.common.serialization.SerializationSchema;

import java.util.Map;
import io.confluent.kafka.serializers.KafkaAvroSerializer;

public class ConfluentWindowedMetricSerializer implements SerializationSchema<WindowedMetric> {
    
    private final String schemaRegistryUrl;
    
    public ConfluentWindowedMetricSerializer(String schemaRegistryUrl) {
        this.schemaRegistryUrl = schemaRegistryUrl;
    }
    
    @Override
    public byte[] serialize(WindowedMetric metric) {
        KafkaAvroSerializer serializer = new KafkaAvroSerializer();
        serializer.configure(Map.of(
            "schema.registry.url", schemaRegistryUrl
        ), false);
        return serializer.serialize(null, metric);
    }
}