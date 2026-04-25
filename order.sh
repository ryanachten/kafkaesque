#!/bin/bash

ORDER_SERVICE_HOST="http://localhost:5295"

curl -X POST "$ORDER_SERVICE_HOST/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "529f0699-0367-43a8-93e3-b792b2eb2dd8",
    "items": [
      {
        "productId": "03eea945-aa44-4155-bed1-68e9e420efad",
        "count": 1
      }
    ]
  }'
