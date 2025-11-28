# Start API Server
Write-Host "Starting Task Management API..." -ForegroundColor Green
Write-Host "API will be available at:" -ForegroundColor Cyan
Write-Host "  - HTTP: http://localhost:5119" -ForegroundColor Green
Write-Host "  - HTTPS: https://localhost:7000" -ForegroundColor Green
Write-Host "  - Swagger: http://localhost:5119/swagger" -ForegroundColor Green
Write-Host ""

Set-Location -Path "$PSScriptRoot\TaskManagement.API"
dotnet run
