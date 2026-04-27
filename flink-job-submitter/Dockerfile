FROM flink:1.18.1-java17

WORKDIR /opt/flink-jobs

COPY target/flink-analytics-1.0.0-SNAPSHOT.jar /opt/flink-jobs/flink-analytics.jar
COPY target/*.jar /opt/flink-jobs/
COPY src/main/resources/application.properties /opt/flink-jobs/application.properties

ARG FLINK_JOB_JAR="flink-analytics.jar"
ENV FLINK_JOB_JAR=${FLINK_JOB_JAR}

ENTRYPOINT ["bash", "-c", "flink run -d /opt/flink-jobs/$FLINK_JOB_JAR"]