-- Migration: 001_add_fulfilled_at.sql
-- Description: Add fulfilled_at and status columns to orders table
-- Created: April 22, 2026

-- Add fulfilled_at column to track fulfillment completion time
ALTER TABLE orders ADD COLUMN IF NOT EXISTS fulfilled_at TIMESTAMPTZ;

-- Add status column to track order status
ALTER TABLE orders ADD COLUMN IF NOT EXISTS status VARCHAR(20) NOT NULL DEFAULT 'PENDING';

-- Create index on status for efficient filtering
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);

-- Create index on fulfilled_at for efficient date-based queries
CREATE INDEX IF NOT EXISTS idx_orders_fulfilled_at ON orders(fulfilled_at);