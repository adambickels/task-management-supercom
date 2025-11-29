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

# Check and clean RabbitMQ queues
Write-Host "Checking RabbitMQ..." -NoNewline
try {
    $rabbitMQUri = "http://localhost:15672/api/overview"
    $credential = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("guest:guest"))
    $response = Invoke-WebRequest -Uri $rabbitMQUri -Method Get -Headers @{Authorization="Basic $credential"} -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    Write-Host " OK" -ForegroundColor Green
    
    # Clean up existing queues to prevent configuration conflicts
    Write-Host "Cleaning up existing RabbitMQ queues..." -NoNewline
    try {
        $queueUri = "http://localhost:15672/api/queues/%2F/task_reminders"
        Invoke-WebRequest -Uri $queueUri -Method Delete -Headers @{Authorization="Basic $credential"} -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null
        Write-Host " Done" -ForegroundColor Green
    } catch {
        Write-Host " Skipped (queue doesn't exist)" -ForegroundColor Gray
    }
} catch {
    Write-Host " WARNING" -ForegroundColor Yellow
    Write-Host "Cannot connect to RabbitMQ. Make sure RabbitMQ is installed and running." -ForegroundColor Yellow
    Write-Host "The Windows Service will not work without RabbitMQ." -ForegroundColor Yellow
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
Write-Host "Step 3: Prepare Database Migrations (Code First)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Set-Location -Path "TaskManagement.API"

# Check if EF Core tools are installed
Write-Host "Checking Entity Framework Core tools..." -NoNewline
$efToolsInstalled = $false
try {
    $efVersion = dotnet ef --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " OK" -ForegroundColor Green
        $efToolsInstalled = $true
    }
} catch {
    Write-Host " NOT FOUND" -ForegroundColor Yellow
}

if (-not $efToolsInstalled) {
    Write-Host "Installing Entity Framework Core tools..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to install EF Core tools. Please install manually: dotnet tool install --global dotnet-ef" -ForegroundColor Red
        Set-Location -Path $PSScriptRoot
        exit 1
    }
    Write-Host "EF Core tools installed successfully!" -ForegroundColor Green
}

# Check if migrations exist
$migrationsPath = "..\TaskManagement.Infrastructure\Migrations"
$migrationsExist = Test-Path $migrationsPath

if (-not $migrationsExist -or (Get-ChildItem $migrationsPath -Filter "*.cs" -ErrorAction SilentlyContinue).Count -eq 0) {
    Write-Host ""
    Write-Host "No migrations found. Creating initial migration..." -ForegroundColor Yellow
    
    # Remove Migrations folder if it exists but is empty
    if (Test-Path $migrationsPath) {
        Remove-Item $migrationsPath -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    # Create initial migration
    dotnet ef migrations add InitialCreate --project ..\TaskManagement.Infrastructure\TaskManagement.Infrastructure.csproj --startup-project TaskManagement.API.csproj
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to create migrations!" -ForegroundColor Red
        Set-Location -Path $PSScriptRoot
        exit 1
    }
    
    Write-Host "Initial migration created successfully!" -ForegroundColor Green
} else {
    Write-Host "Existing migrations found." -ForegroundColor Green
}

Write-Host ""
Write-Host "Migration files are ready." -ForegroundColor Green
Write-Host "Database will be created automatically when you start the API." -ForegroundColor Cyan

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
Write-Host "Migrations are ready. Database will be created automatically!" -ForegroundColor Green
Write-Host "Using Code First approach - no manual database setup needed." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Make sure RabbitMQ is installed and running (http://localhost:15672)" -ForegroundColor White
Write-Host "2. Run the API (database will be created automatically):" -ForegroundColor White
Write-Host "   .\start-api.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Run the Windows Service (in a new terminal):" -ForegroundColor White
Write-Host "   .\start-service.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Run the Frontend (in a new terminal):" -ForegroundColor White
Write-Host "   .\start-frontend.ps1" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Access the application:" -ForegroundColor Green
Write-Host "   - Frontend: http://localhost:5173" -ForegroundColor Gray
Write-Host "   - API: http://localhost:5119" -ForegroundColor Gray
Write-Host "   - Swagger: http://localhost:5119/swagger" -ForegroundColor Gray
Write-Host ""
Write-Host "For technical documentation, see README.md" -ForegroundColor Cyan
Write-Host ""
