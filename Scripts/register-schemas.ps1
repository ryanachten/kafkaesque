# Schema Registration Script
# Registers JSON schemas with the Kafka Schema Registry
# Run this when setting up the environment or when schemas change

Write-Host "Registering schemas with Schema Registry..." -ForegroundColor Cyan

# The schema is already registered, so this script will show it's already there
$schemaJson = '{"type":"object","properties":{"items":{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"count":{"type":"integer","minimum":1}},"required":["name","count"]},"minItems":1}},"required":["items"]}'
$body = "{`"schema`": `"$schemaJson`", `"schemaType`": `"JSON`"}"

try {
    $response = Invoke-WebRequest -Uri "http://localhost:8081/subjects/orders-value/versions" -Method POST -Body $body -ContentType "application/vnd.schemaregistry.v1+json" -ErrorAction Stop
    $result = $response.Content | ConvertFrom-Json
    Write-Host "`nSuccess! Order schema registered (ID: $($result.id))" -ForegroundColor Green
} catch {
    if ($_.Exception.Message -like "*409*") {
        Write-Host "`nInfo: Order schema is already registered" -ForegroundColor Yellow
    } else {
        Write-Host "`nError: Failed to register schema" -ForegroundColor Red
        Write-Host $_.Exception.Message
    }
}

Write-Host "`nDone!" -ForegroundColor Cyan
