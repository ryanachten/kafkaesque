# Kafkaesque

Repository for learning Kafka, event streaming and event-driven architectural patterns.

## Architecture

```mermaid
graph LR
    subgraph InfraTools["Infrastructure Tools"]
        TopicConfig[kafka-topics.yml]
        TopicRegTool[Topic Register Tool]
        SchemaFiles[Avro Schemas]
        SchemaGen[Schema Generator]
        SchemaRegTool[Schema Register Tool]
        
        TopicConfig --> TopicRegTool
        SchemaFiles --> SchemaGen
        SchemaGen --> |Generates C# Classes| SchemaFiles
    end

    subgraph Client
        HTTP[HTTP Client]
    end

    subgraph OrderService["Order Service (Producer)"]
        API[REST API]
        OrderSvc[Order Service]
        OrderRepo[Order Repository]
        OutboxRepo[Outbox Repository]
        DB[(PostgreSQL<br/>orders DB)]
        OutboxWorker[Outbox Worker<br/>Background Service]
        
        API --> OrderSvc
        OrderSvc --> OrderRepo
        OrderSvc --> OutboxRepo
        OrderRepo --> DB
        OutboxRepo --> DB
        OutboxWorker --> OutboxRepo
    end

    subgraph KafkaInfra["Kafka Infrastructure"]
        SchemaReg[Schema Registry]
        Broker[Kafka Broker]
        Topic[Topic: order.placed]
        
        Broker --> Topic
    end

    subgraph FulfillmentService["Fulfillment Service (Consumer)"]
        Consumer[Order Consumer<br/>Background Service]
        WorkerPool[Order Worker Pool<br/>Background Service]
        Queue[Bounded Channel Queue]
        Worker1[Worker 1]
        Worker2[Worker 2]
        Worker3[Worker 3]
        FulfillSvc[Fulfillment Service]
        
        Consumer --> |Enqueues Orders| Queue
        Queue --> WorkerPool
        WorkerPool --> Worker1
        WorkerPool --> Worker2
        WorkerPool --> Worker3
        Worker1 --> FulfillSvc
        Worker2 --> FulfillSvc
        Worker3 --> FulfillSvc
    end

    TopicRegTool --> |Creates Topics| Broker
    SchemaRegTool --> |Registers Schemas| SchemaReg
    HTTP -->|POST /orders| API
    OutboxWorker -->|Publishes Events| Broker
    OutboxWorker -.->|Validates Schema| SchemaReg
    Topic -->|Consumes Events| Consumer
    Consumer -.->|Validates Schema| SchemaReg

  
```

**Key Components:**
- **Order Service**: REST API that creates orders and stores them in PostgreSQL with an outbox pattern for reliable event publishing
- **Outbox Worker**: Background service that polls the outbox table and publishes events to Kafka
- **Kafka Infrastructure**: Message broker with Schema Registry for Avro schema validation
- **Fulfillment Service**: Consumer service that processes order events from Kafka using a worker pool pattern
  - **Order Consumer**: Consumes events from Kafka and maps them to domain models
  - **Order Worker Pool**: Manages a bounded queue and configurable worker threads for concurrent order processing
  - **Fulfillment Service**: Processes individual orders (simulates fulfillment with random delays)
- **Infrastructure Tools**: 
  - **Topic Register**: Automatically registers Kafka topics from configuration on startup
  - **Schema Register**: Registers Avro schemas with the Schema Registry
  - **Schema Generator**: Generates C# classes from Avro schema definitions


## Getting Started

### Prerequisites
- Docker and Docker Compose
- .NET (with tools installed via `dotnet tool restore`)

### Running services
- Run the following bash script to start up the infrastructure:
   ```bash
   ./run.sh
   ```
- When making changes, we can rebuild and run a specific container by supplying it as an argument to the run script
   ```bash
   ./run.sh order-service # fulfillment-service, schema-register, topic-register etc
   ```

### Managing topics
Topics are automatically created on startup via the `topic-register` service, which reads from [kafka-topics.yml](Schemas/kafka-topics.yml).

**Adding new topics:**
1. Edit [kafka-topics.yml](Schemas/kafka-topics.yml) to add your topic definition
2. Restart the infrastructure: `./run.sh`

Topics can also be managed manually through the Kafka Control Center at http://localhost:9021.

### Viewing logs
Structured logs from services are sent to Seq in development mode. Access the Seq web UI at http://localhost:5341 to search, filter, and analyze logs.

### Updating schemas
See the following guidance for [creating or updating schemas](./Schemas/README.md) 