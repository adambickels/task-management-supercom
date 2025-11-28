# Task Management Application - Setup Script
# This script helps set up the application for first-time use

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Task Management Application Setup" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check .NET SDK
Write-Host "Checking .NET SDK..." -NoNewline
try {
    $dotnetVersion = dotnet --version
    Write-Host " OK (Version: $dotnetVersion)" -ForegroundColor Green
} catch {
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "Please install .NET 10 SDK from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

# Check Node.js
Write-Host "Checking Node.js..." -NoNewline
try {
    $nodeVersion = node --version
    Write-Host " OK (Version: $nodeVersion)" -ForegroundColor Green
} catch {
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "Please install Node.js from https://nodejs.org/" -ForegroundColor Red
    exit 1
}

# Check SQL Server
Write-Host "Checking SQL Server connection..." -NoNewline
$connectionString = "Server=localhost;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
try {
    $connection = New-Object System.Data.SqlClient.SqlConnection
    $connection.ConnectionString = $connectionString
    $connection.Open()
    $connection.Close()
    Write-Host " OK" -ForegroundColor Green
} catch {
    Write-Host " WARNING" -ForegroundColor Yellow
    Write-Host "Cannot connect to SQL Server. Make sure SQL Server is installed and running." -ForegroundColor Yellow
    Write-Host "You may need to update the connection string in appsettings.json files." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Step 1: Restore NuGet Packages" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Set-Location -Path $PSScriptRoot
dotnet restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to restore NuGet packages" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Step 2: Build Solution" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

dotnet build --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to build solution" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Step 3: Apply Database Migrations" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Set-Location -Path "TaskManagement.API"

try {
    dotnet ef database update --project ..\TaskManagement.Infrastructure\TaskManagement.Infrastructure.csproj
    Write-Host "Database migrations applied successfully!" -ForegroundColor Green
} catch {
    Write-Host "Failed to apply migrations. Make sure SQL Server is running and connection string is correct." -ForegroundColor Yellow
}

Set-Location -Path $PSScriptRoot

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Step 4: Install Frontend Dependencies" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Set-Location -Path "task-management-ui"
npm install

if ($LASTEXITCODE -ne 0) {
    Write-Host "Failed to install frontend dependencies" -ForegroundColor Red
    Set-Location -Path $PSScriptRoot
    exit 1
}

Set-Location -Path $PSScriptRoot

Write-Host ""
Write-Host "============================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Make sure RabbitMQ is installed and running (http://localhost:15672)" -ForegroundColor White
Write-Host "2. Run the API:" -ForegroundColor White
Write-Host "   cd TaskManagement.API" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Run the Windows Service (in a new terminal):" -ForegroundColor White
Write-Host "   cd TaskManagement.Service" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Run the Frontend (in a new terminal):" -ForegroundColor White
Write-Host "   cd task-management-ui" -ForegroundColor Gray
Write-Host "   npm run dev" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Access the application:" -ForegroundColor Green
Write-Host "   - Frontend: http://localhost:5173" -ForegroundColor Gray
Write-Host "   - API: http://localhost:5119" -ForegroundColor Gray
Write-Host "   - Swagger: http://localhost:5119/swagger" -ForegroundColor Gray
Write-Host ""
Write-Host "For detailed instructions, see README.md" -ForegroundColor Yellow
Write-Host ""
