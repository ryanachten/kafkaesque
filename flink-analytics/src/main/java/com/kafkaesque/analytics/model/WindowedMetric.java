package com.kafkaesque.analytics.model;

import java.math.BigDecimal;

/**
 * WindowedMetric represents aggregated metrics for a time window.
 */
public class WindowedMetric {
    
    private long windowStart;
    private long windowEnd;
    private String windowSize;
    private long orderCount;
    private BigDecimal totalRevenue;
    private BigDecimal avgOrderValue;
    private long processedAt;

    public WindowedMetric() {}

    public WindowedMetric(long windowStart, long windowEnd, String windowSize,
                         long orderCount, BigDecimal totalRevenue, 
                         BigDecimal avgOrderValue, long processedAt) {
        this.windowStart = windowStart;
        this.windowEnd = windowEnd;
        this.windowSize = windowSize;
        this.orderCount = orderCount;
        this.totalRevenue = totalRevenue;
        this.avgOrderValue = avgOrderValue;
        this.processedAt = processedAt;
    }

    public long getWindowStart() { return windowStart; }
    public void setWindowStart(long windowStart) { this.windowStart = windowStart; }

    public long getWindowEnd() { return windowEnd; }
    public void setWindowEnd(long windowEnd) { this.windowEnd = windowEnd; }

    public String getWindowSize() { return windowSize; }
    public void setWindowSize(String windowSize) { this.windowSize = windowSize; }

    public long getOrderCount() { return orderCount; }
    public void setOrderCount(long orderCount) { this.orderCount = orderCount; }

    public BigDecimal getTotalRevenue() { return totalRevenue; }
    public void setTotalRevenue(BigDecimal totalRevenue) { this.totalRevenue = totalRevenue; }

    public BigDecimal getAvgOrderValue() { return avgOrderValue; }
    public void setAvgOrderValue(BigDecimal avgOrderValue) { this.avgOrderValue = avgOrderValue; }

    public long getProcessedAt() { return processedAt; }
    public void setProcessedAt(long processedAt) { this.processedAt = processedAt; }
}