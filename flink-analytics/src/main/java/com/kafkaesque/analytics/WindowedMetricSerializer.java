package com.kafkaesque.analytics;

import com.kafkaesque.analytics.model.WindowedMetric;
import org.apache.flink.api.common.serialization.SerializationSchema;

/**
 * WindowedMetricSerializer converts WindowedMetric objects to bytes for Kafka.
 */
public class WindowedMetricSerializer implements SerializationSchema<WindowedMetric> {

    @Override
    public byte[] serialize(WindowedMetric element) {
        StringBuilder sb = new StringBuilder();
        sb.append("{\"windowStart\":").append(element.getWindowStart());
        sb.append(",\"windowEnd\":").append(element.getWindowEnd());
        sb.append(",\"windowSize\":\"").append(element.getWindowSize()).append("\"");
        sb.append(",\"orderCount\":").append(element.getOrderCount());
        sb.append(",\"totalRevenue\":\"").append(element.getTotalRevenue()).append("\"");
        sb.append(",\"avgOrderValue\":\"").append(element.getAvgOrderValue()).append("\"");
        sb.append(",\"processedAt\":").append(element.getProcessedAt());
        sb.append("}");
        return sb.toString().getBytes();
    }
}