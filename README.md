# Kafkaesque

Repository for learning Kafka, event streaming and event-driven architectural patterns.

## Getting Started

### Prerequisites
- Docker and Docker Compose
- .NET (with tools installed via `dotnet tool restore`)

### Starting the project
- Run the following bash script to start up the infrastructure:
   ```bash
   ./run-local.sh
   ```

### Creating topics
Currently the topics aren't created automatically when first starting the the Kafka instance (TODO). Instead we need to create these manually through the control center
- Open http://localhost:9021 to view the Kafka control center
- Navigate to the cluster and then to the topic
- Create a new topic `order.placed` with 3 partitions
- Restart the consumer containers


## Development
If you've made changes locally, you won't want to restart the entire stack. To rebuild only the modified service in question, you can use this command:

- To rebuild only producer: `docker-compose  -f docker-compose.kafka.yml -f docker-compose.yml up -d --build order-service`
- To rebuild only consumer: `docker-compose  -f docker-compose.kafka.yml -f docker-compose.yml up -d --build order-consumer`
- To rebuild only schema register: `docker-compose -f docker-compose.kafka.yml -f docker-compose.yml up -d --build schema-register`