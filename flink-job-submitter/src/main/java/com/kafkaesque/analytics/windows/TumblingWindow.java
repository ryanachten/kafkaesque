package com.kafkaesque.analytics.windows;

import com.kafkaesque.analytics.model.WindowedMetric;
import org.apache.flink.api.common.functions.ReduceFunction;
import org.apache.flink.streaming.api.functions.windowing.ProcessWindowFunction;
import org.apache.flink.streaming.api.windowing.windows.TimeWindow;
import org.apache.flink.util.Collector;
import Schemas.OrderPlacedItem;

import java.math.BigDecimal;
import java.math.RoundingMode;

/**
 * Implements tumbling window aggregation for order metrics.
 *
 * Tumbling windows are fixed-size, non-overlapping time windows.
 * Each order belongs to exactly one window based on its event timestamp.
 *
 * The window size is configured via flink.window.size.minutes,
 * allowing the same class to handle 1-minute, 1-hour, or 24-hour windows.
 *
 * Key components:
 * - MetricReduceFunction: Combines accumulators incrementally as orders arrive
 * - MetricWindowFunction: Called when window fires to calculate final metrics
 * - WindowAggregateAccumulator: Holds running count and revenue totals
 *
 * This design uses incremental aggregation for efficiency:
 * 1. Each order creates an accumulator with count=1
 * 2. ReduceFunction combines accumulators as orders arrive (fast)
 * 3. ProcessWindowFunction emits final metric when window fires (once)
 *
 * This is much more efficient than storing all events and processing at window end.
 */
public class TumblingWindow {

    /**
     * ReduceFunction that combines WindowAggregateAccumulator instances.
     *
     * Called incrementally as each event arrives in the window.
     * This is more efficient than storing all events because
     * memory usage stays constant regardless of window size.
     *
     * Combines:
     * - orderCount: Simply adds the counts together
     * - totalRevenue: Adds the revenue totals together
     */
    public static class MetricReduceFunction implements ReduceFunction<WindowAggregateAccumulator> {

        @Override
        public WindowAggregateAccumulator reduce(WindowAggregateAccumulator acc1, WindowAggregateAccumulator acc2) {
            acc1.orderCount += acc2.orderCount;
            acc1.totalRevenue = acc1.totalRevenue.add(acc2.totalRevenue);
            return acc1;
        }
    }

    /**
     * ProcessWindowFunction that emits the final WindowedMetric.
     *
     * Called once when the window fires (after watermark passes window end).
     * Calculates average order value and constructs the metric output.
     *
     * @param key The key (unused - we use single "all" key for simplicity)
     * @param context Window context containing window start/end times
     * @param elements The accumulated metrics (should be one element)
     * @param out Collector for outputting the WindowedMetric
     */
    public static class MetricWindowFunction extends ProcessWindowFunction<WindowAggregateAccumulator, WindowedMetric, String, TimeWindow> {

        private final String windowSizeLabel;

        /**
         * Create with window size label for metric output.
         *
         * @param windowSizeLabel Label for window size: "1m", "1h", "24h"
         */
        public MetricWindowFunction(String windowSizeLabel) {
            this.windowSizeLabel = windowSizeLabel;
        }

        @Override
        public void process(String key, Context context, Iterable<WindowAggregateAccumulator> elements, Collector<WindowedMetric> out) throws Exception {
            WindowAggregateAccumulator acc = elements.iterator().next();
            TimeWindow window = context.window();

            long windowStart = window.getStart();
            long windowEnd = window.getEnd();
            long processedAt = System.currentTimeMillis();

            /**
             * Calculate average order value.
             * Uses BigDecimal for accurate decimal arithmetic (not floating point).
             * Rounds to 2 decimal places for currency.
             */
            BigDecimal avgOrderValue = acc.orderCount > 0
                    ? acc.totalRevenue.divide(BigDecimal.valueOf(acc.orderCount), 2, RoundingMode.HALF_UP)
                    : BigDecimal.ZERO;

            /**
             * Create the WindowedMetric output.
             * Uses configured windowSizeLabel for differentiation.
             */
            WindowedMetric metric = new WindowedMetric(
                    windowStart,
                    windowEnd,
                    windowSizeLabel,
                    acc.orderCount,
                    acc.totalRevenue.toPlainString(),
                    avgOrderValue.toPlainString(),
                    processedAt
            );

            /**
             * Log window firing for debugging/monitoring.
             * In production, use proper logging framework.
             */
            System.out.println("Window fired: " + windowSizeLabel + " window [" + windowStart + ", " + windowEnd + "] - count=" + acc.orderCount);
            System.out.println("Emitting metric: orderCount=" + acc.orderCount + ", totalRevenue=" + acc.totalRevenue + ", avg=" + avgOrderValue);

            out.collect(metric);
        }
    }

    /**
     * Accumulator for window aggregation.
     *
     * Holds running totals that are combined as events arrive.
     * Using BigDecimal for accurate monetary calculations.
     */
    public static class WindowAggregateAccumulator {
        public long orderCount = 0;
        public BigDecimal totalRevenue = BigDecimal.ZERO;
    }

    /**
     * Wrapper class for order data (kept for potential future use with keyed streams).
     * Currently we use WindowAggregateAccumulator directly.
     */
    public static class OrderWrapper {
        public String orderShortCode;
        public BigDecimal total;

        public OrderWrapper(String orderShortCode, BigDecimal total) {
            this.orderShortCode = orderShortCode;
            this.total = total;
        }
    }

    /**
     * Convert an OrderPlaced to a WindowAggregateAccumulator.
     *
     * Creates an accumulator with:
     * - orderCount: 1 (this order)
     * - totalRevenue: Sum of item counts
     *
     * In a production system, this would use the order's total amount.
     * Here we use item counts as a proxy for order value.
     *
     * @param order The order to convert
     * @return WindowAggregateAccumulator with this order's data
     */
    public static WindowAggregateAccumulator toAccumulator(Schemas.OrderPlaced order) {
        WindowAggregateAccumulator acc = new WindowAggregateAccumulator();
        acc.orderCount = 1;
        BigDecimal total = BigDecimal.ZERO;
        if (order.getItems() != null) {
            for (OrderPlacedItem item : order.getItems()) {
                int count = item.getCount();
                total = total.add(BigDecimal.valueOf(count));
            }
        }
        acc.totalRevenue = total;
        return acc;
    }
}