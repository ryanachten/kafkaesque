package com.kafkaesque.analytics.functions;

import java.time.Instant;
import org.apache.flink.api.common.eventtime.SerializableTimestampAssigner;
import Schemas.OrderPlaced;

/**
 * Extracts event timestamps from OrderPlaced events for event-time processing.
 *
 * In Flink, event-time windowing requires knowing when events actually occurred
 * (event time), not when they were processed (processing time).
 * This is critical for windowing when events may arrive out of order due to
 * network delays or late-arriving data.
 *
 * The SerializableTimestampAssigner interface is used with WatermarkStrategy to:
 * 1. Extract timestamps from each event for window assignment
 * 2. Enable watermark generation to track event time progress
 *
 * The extracted timestamp is used to determine which window an event belongs to,
 * and when watermarks can advance to trigger window firing.
 */
public class OrderTimestampExtractor implements SerializableTimestampAssigner<OrderPlaced> {

    /**
     * Extract the event timestamp from an OrderPlaced event.
     *
     * The timestamp field in the order represents when the order was placed,
     * which is the basis for windowing decisions in event-time processing.
     *
     * @param element The OrderPlaced event to extract timestamp from
     * @param recordTimestamp The default timestamp (may be used as fallback)
     * @return Unix timestamp in milliseconds from the order's timestamp field
     */
    @Override
    public long extractTimestamp(OrderPlaced element, long recordTimestamp) {
        Instant ts = element.getTimestamp();
        return ts != null ? ts.toEpochMilli() : recordTimestamp;
    }
}