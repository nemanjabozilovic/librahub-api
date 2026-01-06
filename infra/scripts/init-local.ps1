# PowerShell script to initialize local development environment and start all services

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)

Write-Host "Initializing LibraHub local development environment..." -ForegroundColor Green

# Check if Docker is running
Write-Host "`nChecking Docker..." -ForegroundColor Yellow
try {
    docker ps | Out-Null
    Write-Host "Docker is running" -ForegroundColor Green
} catch {
    Write-Host "Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Start infrastructure services
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

# Start all service APIs
Write-Host "`nStarting all service APIs..." -ForegroundColor Yellow

$Services = @(
    "services\Identity",
    "services\Catalog",
    "services\Content",
    "services\Orders",
    "services\Library",
    "services\Notifications",
    "services\Gateway"
)

foreach ($Service in $Services) {
    $ServiceName = Split-Path -Leaf $Service
    Write-Host "`nStarting $ServiceName service..." -ForegroundColor Cyan
    Set-Location "$RootDir\$Service"
    docker-compose up -d
    if ($LASTEXITCODE -eq 0) {
        Write-Host "$ServiceName started successfully" -ForegroundColor Green
    } else {
        Write-Host "Failed to start $ServiceName" -ForegroundColor Red
    }
}

Write-Host "`nWaiting for all services to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host "`n==========================================" -ForegroundColor Green
Write-Host "Initialization complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host "`nInfrastructure services:" -ForegroundColor Cyan
Write-Host "  - PostgreSQL: localhost:5432" -ForegroundColor White
Write-Host '  - RabbitMQ Management: http://localhost:15672 (librahub_mq/R@bb1tMQ_L1br@Hub_2026!S3cur3_P@ss)' -ForegroundColor White
Write-Host "  - Redis: localhost:6379" -ForegroundColor White
Write-Host "  - pgAdmin: http://localhost:5050 (admin@librahub.com/admin)" -ForegroundColor White
Write-Host "  - MinIO Console: http://localhost:9001 (minioadmin/minioadmin)" -ForegroundColor White
Write-Host "  - MinIO API: http://localhost:9000" -ForegroundColor White
Write-Host "  - Papercut SMTP: http://localhost:8082" -ForegroundColor White
Write-Host "`nAPI Services:" -ForegroundColor Cyan
Write-Host "  - Gateway: http://localhost:5000" -ForegroundColor White
Write-Host "  - Identity: http://localhost:60950" -ForegroundColor White
Write-Host "  - Catalog: http://localhost:60960" -ForegroundColor White
Write-Host "  - Content: http://localhost:60970" -ForegroundColor White
Write-Host "  - Orders: http://localhost:60980" -ForegroundColor White
Write-Host "  - Library: http://localhost:60990" -ForegroundColor White
Write-Host "  - Notifications: http://localhost:61000" -ForegroundColor White
Write-Host "`nTo view logs, run:" -ForegroundColor Cyan
Write-Host "  docker-compose logs -f [service-name]" -ForegroundColor White
Write-Host "`nTo stop all services, run:" -ForegroundColor Cyan
Write-Host '  .\infra\scripts\stop-local.ps1' -ForegroundColor White

