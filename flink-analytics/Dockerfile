FROM flink:1.18.1-java17

WORKDIR /opt/flink-analytics

COPY target/flink-analytics-1.0.0-SNAPSHOT.jar /opt/flink-analytics/flink-analytics.jar
COPY src/main/resources/application.properties /opt/flink-analytics/application.properties

ENTRYPOINT ["flink", "run", "-d", "/opt/flink-analytics/flink-analytics.jar"]