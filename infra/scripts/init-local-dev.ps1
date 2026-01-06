# PowerShell script to initialize local development environment with only infrastructure services
# API services should be run manually in terminal (like in Visual Studio)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)

Write-Host "Initializing LibraHub local development environment (Infrastructure only)..." -ForegroundColor Green

# Check if Docker is running
Write-Host "`nChecking Docker..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "Docker is running" -ForegroundColor Green
} catch {
    Write-Host "Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Start only infrastructure services from root docker-compose.yml
Write-Host "`nStarting infrastructure services (PostgreSQL, RabbitMQ, Redis, pgAdmin, MinIO, Papercut SMTP)..." -ForegroundColor Yellow
Set-Location $RootDir
docker-compose up -d

Write-Host "Waiting for infrastructure services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

# Check service health
Write-Host "`nChecking infrastructure service health..." -ForegroundColor Yellow
$postgresHealthy = docker exec librahub-postgres pg_isready -U librahub_admin 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "PostgreSQL is ready" -ForegroundColor Green
} else {
    Write-Host "PostgreSQL is not ready yet" -ForegroundColor Yellow
}

$rabbitmqHealthy = docker exec librahub-rabbitmq rabbitmq-diagnostics -q ping 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "RabbitMQ is ready" -ForegroundColor Green
} else {
    Write-Host "RabbitMQ is not ready yet" -ForegroundColor Yellow
}

$redisHealthy = docker exec librahub-redis redis-cli ping 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "Redis is ready" -ForegroundColor Green
} else {
    Write-Host "Redis is not ready yet" -ForegroundColor Yellow
}

Write-Host "`n==========================================" -ForegroundColor Green
Write-Host "Infrastructure services are ready!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

Write-Host "`nInfrastructure services:" -ForegroundColor Cyan
Write-Host "  - PostgreSQL: localhost:5432" -ForegroundColor White
Write-Host "    Connection String: Host=localhost;Port=5432;Database=librahub_identity;Username=librahub_admin;Password=L1br@Hub_DB_2026!S3cur3_P@ss" -ForegroundColor Gray
Write-Host '  - RabbitMQ Management: http://localhost:15672 (librahub_mq/R@bb1tMQ_L1br@Hub_2026!S3cur3_P@ss)' -ForegroundColor White
Write-Host "    Connection String: amqp://librahub_mq:R%40bb1tMQ_L1br%40Hub_2026%21S3cur3_P%40ss@localhost:5672/" -ForegroundColor Gray
Write-Host "  - Redis: localhost:6379" -ForegroundColor White
Write-Host "    Connection String: localhost:6379" -ForegroundColor Gray
Write-Host "  - pgAdmin: http://localhost:5050 (admin@librahub.com/admin)" -ForegroundColor White
Write-Host "  - MinIO Console: http://localhost:9001 (minioadmin/minioadmin)" -ForegroundColor White
Write-Host "  - MinIO API: http://localhost:9000" -ForegroundColor White
Write-Host "    Endpoint: localhost:9000" -ForegroundColor Gray
Write-Host "  - Papercut SMTP: http://localhost:8082" -ForegroundColor White
Write-Host "    SMTP Host: localhost:25" -ForegroundColor Gray

Write-Host "`n==========================================" -ForegroundColor Yellow
Write-Host "Next Steps: Run API Services Manually" -ForegroundColor Yellow
Write-Host "==========================================" -ForegroundColor Yellow

Write-Host "`nTo run API services in terminal (like Visual Studio), use:" -ForegroundColor Cyan
Write-Host "`n1. Identity Service:" -ForegroundColor White
Write-Host "   cd services\Identity\src\LibraHub.Identity.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray

Write-Host "`n2. Catalog Service:" -ForegroundColor White
Write-Host "   cd services\Catalog\src\LibraHub.Catalog.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray

Write-Host "`n3. Content Service:" -ForegroundColor White
Write-Host "   cd services\Content\src\LibraHub.Content.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray

Write-Host "`n4. Orders Service:" -ForegroundColor White
Write-Host "   cd services\Orders\src\LibraHub.Orders.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray

Write-Host "`n5. Library Service:" -ForegroundColor White
Write-Host "   cd services\Library\src\LibraHub.Library.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray

Write-Host "`n6. Notifications Service:" -ForegroundColor White
Write-Host "   cd services\Notifications\src\LibraHub.Notifications.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray

Write-Host "`n7. Gateway Service:" -ForegroundColor White
Write-Host "   cd services\Gateway\src\LibraHub.Gateway.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray

Write-Host "`nOr use Visual Studio to run multiple services at once." -ForegroundColor Cyan

Write-Host "`nAPI Service URLs (when running locally):" -ForegroundColor Cyan
Write-Host "  - Gateway: http://localhost:5000" -ForegroundColor White
Write-Host "  - Identity: http://localhost:60950 (or check launchSettings.json)" -ForegroundColor White
Write-Host "  - Catalog: http://localhost:60960 (or check launchSettings.json)" -ForegroundColor White
Write-Host "  - Content: http://localhost:60970 (or check launchSettings.json)" -ForegroundColor White
Write-Host "  - Orders: http://localhost:60980 (or check launchSettings.json)" -ForegroundColor White
Write-Host "  - Library: http://localhost:60990 (or check launchSettings.json)" -ForegroundColor White
Write-Host "  - Notifications: http://localhost:61000 (or check launchSettings.json)" -ForegroundColor White

Write-Host "`nTo stop infrastructure services, run:" -ForegroundColor Cyan
Write-Host '  .\infra\scripts\stop-local-dev.ps1' -ForegroundColor White
Write-Host "`nOr manually:" -ForegroundColor Cyan
Write-Host "  docker-compose down" -ForegroundColor White

