FROM flink:1.18.1-java17

WORKDIR /opt/flink-jobs
COPY target/flink-job-submitter-1.0.0-SNAPSHOT.jar /opt/flink-jobs/flink-job-submitter.jar
COPY src/main/resources/application.properties /opt/flink-jobs/application.properties

ARG FLINK_JOB_JAR="flink-job-submitter.jar"
ENV FLINK_JOB_JAR=${FLINK_JOB_JAR}

ENTRYPOINT ["bash", "-c", "exec flink run -d \"/opt/flink-jobs/$FLINK_JOB_JAR\""]