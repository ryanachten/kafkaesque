#!/bin/bash

SERVICE=$1

if [ -z "$SERVICE" ]; then
  echo "Starting all services..."
  docker compose -f docker-compose.kafka.yml -f docker-compose.yml up -d
else
  echo "Rebuilding service: $SERVICE"
  docker compose -f docker-compose.kafka.yml -f docker-compose.yml up -d --build $SERVICE
fi