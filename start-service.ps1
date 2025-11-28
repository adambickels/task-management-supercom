# Start Windows Service
Write-Host "Starting Task Management Windows Service..." -ForegroundColor Cyan
Write-Host "Service will:" -ForegroundColor Yellow
Write-Host "  - Check for overdue tasks every 5 minutes" -ForegroundColor White
Write-Host "  - Publish reminders to RabbitMQ" -ForegroundColor White
Write-Host "  - Log reminder messages" -ForegroundColor White
Write-Host ""
Write-Host "Make sure RabbitMQ is running at localhost:5672" -ForegroundColor Yellow
Write-Host ""

Set-Location -Path "$PSScriptRoot\TaskManagement.Service"
dotnet run
