# Start React Frontend
Write-Host "Starting Task Management Frontend..." -ForegroundColor Cyan
Write-Host "Frontend will be available at:" -ForegroundColor Yellow
Write-Host "  - http://localhost:5173" -ForegroundColor Green
Write-Host ""
Write-Host "Make sure the API is running at http://localhost:5000" -ForegroundColor Yellow
Write-Host ""

Set-Location -Path "$PSScriptRoot\task-management-ui"
npm run dev
