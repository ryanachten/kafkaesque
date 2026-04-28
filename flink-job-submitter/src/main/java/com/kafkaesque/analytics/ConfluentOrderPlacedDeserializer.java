package com.kafkaesque.analytics;

import io.confluent.kafka.schemaregistry.client.SchemaRegistryClient;
import org.apache.avro.io.BinaryDecoder;
import org.apache.avro.io.DecoderFactory;
import org.apache.avro.specific.SpecificDatumReader;
import org.apache.flink.api.common.serialization.DeserializationSchema;
import org.apache.flink.api.common.typeinfo.TypeInformation;
import Schemas.OrderPlaced;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.net.URI;

public class ConfluentOrderPlacedDeserializer implements DeserializationSchema<OrderPlaced> {
    
    private final String schemaRegistryUrl;
    private final org.apache.avro.Schema readerSchema;
    
    public ConfluentOrderPlacedDeserializer(String schemaRegistryUrl) {
        this.schemaRegistryUrl = schemaRegistryUrl;
        this.readerSchema = OrderPlaced.getClassSchema();
    }
    
    @Override
    public OrderPlaced deserialize(byte[] message) throws IOException {
        if (message == null) {
            return null;
        }
        
        if (message.length < 5) {
            throw new IOException("Invalid message: too short");
        }
        
        int schemaId = ((message[1] & 0xFF) << 24) | 
                     ((message[2] & 0xFF) << 16) | 
                     ((message[3] & 0xFF) << 8) | 
                     (message[4] & 0xFF);
        
        try {
            SchemaRegistryClient client = new SchemaRegistryClient(schemaRegistryUrl);
            String schemaJson = client.getSchemaById(schemaId).getSchema();
            org.apache.avro.Schema writerSchema = new org.apache.avro.Schema.Parser().parse(schemaJson);
            
            InputStream inputStream = new ByteArrayInputStream(message, 5, message.length - 5);
            BinaryDecoder decoder = DecoderFactory.get().binaryDecoder(inputStream, null);
            
            SpecificDatumReader<OrderPlaced> reader = new SpecificDatumReader<>(writerSchema, readerSchema);
            return reader.read(null, decoder);
        } catch (Exception e) {
            throw new IOException("Deserialization failed for schema ID " + schemaId + ": " + e.getMessage(), e);
        }
    }
    
    @Override
    public boolean isEndOfStream(OrderPlaced element) {
        return false;
    }
    
    @Override
    public TypeInformation<OrderPlaced> getProducedType() {
        return TypeInformation.of(OrderPlaced.class);
    }
}