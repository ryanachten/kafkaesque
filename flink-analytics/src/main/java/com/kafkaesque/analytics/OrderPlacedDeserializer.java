package com.kafkaesque.analytics;

import com.kafkaesque.analytics.model.OrderPlaced;
import org.apache.flink.api.common.serialization.DeserializationSchema;
import org.apache.flink.api.common.typeinfo.TypeInformation;

/**
 * OrderPlacedDeserializer converts bytes from Kafka into OrderPlaced Java objects.
 */
public class OrderPlacedDeserializer implements DeserializationSchema<OrderPlaced> {

    @Override
    public OrderPlaced deserialize(byte[] message) {
        try {
            String json = new String(message);
            return parseOrderPlaced(json);
        } catch (Exception e) {
            return null;
        }
    }

    private OrderPlaced parseOrderPlaced(String json) {
        OrderPlaced order = new OrderPlaced();
        order.setOrderId(extractJsonValue(json, "orderId"));
        order.setCustomerId(extractJsonValue(json, "customerId"));
        order.setStatus(extractJsonValue(json, "status"));
        
        String ts = extractJsonValue(json, "timestamp");
        if (ts != null) order.setTimestamp(Long.parseLong(ts));
        
        String total = extractJsonValue(json, "total");
        if (total != null) order.setTotal(new java.math.BigDecimal(total));
        
        return order;
    }

    private String extractJsonValue(String json, String key) {
        String searchKey = "\"" + key + "\"";
        int keyIndex = json.indexOf(searchKey);
        if (keyIndex < 0) return null;
        
        int colonIndex = json.indexOf(":", keyIndex);
        if (colonIndex < 0) return null;
        
        int start = colonIndex + 1;
        while (start < json.length() && Character.isWhitespace(json.charAt(start))) start++;
        
        if (start >= json.length()) return null;
        
        if (json.charAt(start) == '"') {
            int end = json.indexOf('"', start + 1);
            return json.substring(start + 1, end);
        } else {
            int end = start;
            while (end < json.length() && json.charAt(end) != ',' && json.charAt(end) != '}') end++;
            return json.substring(start, end).trim();
        }
    }

    @Override
    public TypeInformation<OrderPlaced> getProducedType() {
        return TypeInformation.of(OrderPlaced.class);
    }

    @Override
    public boolean isEndOfStream(OrderPlaced nextElement) {
        return false;
    }
}