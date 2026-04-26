package com.kafkaesque.analytics.model;

import java.math.BigDecimal;

/**
 * OrderPlaced represents an order that has been placed in the system.
 */
public class OrderPlaced {
    
    private String orderId;
    private String customerId;
    private long timestamp;
    private BigDecimal total;
    private String status;

    public OrderPlaced() {}

    public OrderPlaced(String orderId, String customerId, long timestamp, 
                     BigDecimal total, String status) {
        this.orderId = orderId;
        this.customerId = customerId;
        this.timestamp = timestamp;
        this.total = total;
        this.status = status;
    }

    public String getOrderId() { return orderId; }
    public void setOrderId(String orderId) { this.orderId = orderId; }

    public String getCustomerId() { return customerId; }
    public void setCustomerId(String customerId) { this.customerId = customerId; }

    public long getTimestamp() { return timestamp; }
    public void setTimestamp(long timestamp) { this.timestamp = timestamp; }

    public BigDecimal getTotal() { return total; }
    public void setTotal(BigDecimal total) { this.total = total; }

    public String getStatus() { return status; }
    public void setStatus(String status) { this.status = status; }
}