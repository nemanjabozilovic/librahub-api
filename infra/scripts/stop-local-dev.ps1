# PowerShell script to stop only infrastructure services

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent (Split-Path -Parent $ScriptDir)

Write-Host "Stopping infrastructure services..." -ForegroundColor Yellow

Set-Location $RootDir
docker-compose down

Write-Host "`nInfrastructure services stopped." -ForegroundColor Green
Write-Host "`nNote: If you have API services running in terminal, stop them manually (Ctrl+C)." -ForegroundColor Cyan

