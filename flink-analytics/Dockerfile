FROM flink:1.18.1-java17

WORKDIR /opt/flink-analytics

COPY target/flink-analytics-1.0.0-SNAPSHOT.jar /opt/flink-analytics/flink-analytics.jar
COPY src/main/resources/application.properties /opt/flink-analytics/application.properties

ENV BOOTSTRAP_SERVERS=broker:29092
ENV SCHEMA_REGISTRY_URL=http://schema-registry:8081

EXPOSE 8081

ENTRYPOINT ["java", "-jar", "/opt/flink-analytics/flink-analytics.jar"]