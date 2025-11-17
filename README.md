# Kafka Example

# Getting started

- Run `docker-compose up -d` to spin up the Confluent Kafka stack
- Open http://localhost:9021 to view the Confluent Kakfa control centre
- To rebuild only producer: `docker-compose up -d --build kafkaproducer`
- To rebuild only consumer: `docker-compose up -d --build kafkaconsumer`
