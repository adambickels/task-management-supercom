# Task Management Application

Hey there! This is a full-stack task management app I built for SUPERCOM's developer assignment. It's got everything you'd expect from a modern web app - a .NET Core backend, React frontend, SQL Server database, and even a background service that sends reminders through RabbitMQ when tasks are overdue.

**Developer:** Adam Bickels

## Table of Contents
- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Setup Instructions](#setup-instructions)
- [Running the Application](#running-the-application)
- [Architecture](#architecture)
- [Technologies Used](#technologies-used)
- [Project Structure](#project-structure)
- [Key Features](#key-features)
- [API Endpoints](#api-endpoints)
- [Database Schema](#database-schema)
- [SQL Query for Tasks with Multiple Tags](#sql-query-for-tasks-with-multiple-tags)
- [Windows Service](#windows-service)
- [Testing](#testing)

## Overview

This task management system lets you create, edit, update, and delete tasks with all the details you need - title, description, due dates, priority levels, user info, and tags. I built it with a .NET Core Web API backend, a React frontend using Redux Toolkit for state management, SQL Server with Entity Framework Core, and a Windows Service that keeps an eye on overdue tasks and sends reminders via RabbitMQ.

## What You'll Need

Before running this app, make sure you have these installed:

1. **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
2. **Node.js 18+** and npm - [Download](https://nodejs.org/)
3. **SQL Server** (Express or Developer Edition) - [Download](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
4. **RabbitMQ Server** - [Download](https://www.rabbitmq.com/download.html)
5. **Visual Studio 2022** or **VS Code** (recommended)

## Quick Start

**The easiest way to get this running - just use these PowerShell scripts:**

```powershell
# 1. First time setup - installs everything and creates migrations
#    This will:
#    - Restore all the NuGet packages
#    - Build the solution
#    - Create the database migrations (Code First style)
#    - Install all the frontend dependencies
#    NOTE: The database itself gets created automatically when you start the API
.\setup.ps1

# 2. Fire up the API (database magically appears on first run!)
.\start-api.ps1

# 3. Start the background service (open a new terminal)
.\start-service.ps1

# 4. Launch the frontend (one more terminal)
.\start-frontend.ps1
```

> **Quick tip:** Just run `.\setup.ps1` once to get everything ready, then start the three services. The database creates itself automatically - no manual setup needed!

**Where to find everything:**
- **Frontend**: http://localhost:5173
- **API**: http://localhost:5119
- **Swagger UI**: http://localhost:5119/swagger
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

## Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd task-management-system
```

**Got a ZIP file instead?**

1. Just extract it wherever you want
2. Open PowerShell in that folder

### 2. Database Setup

Here's the cool part - I used a true Code First approach with Entity Framework. The database and all the tables **create themselves automatically** when you first start the API or Windows Service.

#### Connection String

If your SQL Server setup is different from the default, just update the connection string in these files:

- `TaskManagement.API/appsettings.json`
- `TaskManagement.API/appsettings.Development.json`
- `TaskManagement.Service/appsettings.json`

Default connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

#### How It Works (The Magic Behind the Scenes)

The setup script (`.\setup.ps1`) just prepares the migration files. The actual database gets created when you run the app:

1. **Setup phase** (`.\setup.ps1`):
   - Checks if you have Entity Framework Core tools
   - Creates migration files from the entity classes (if needed)
   - Doesn't touch the database yet

2. **Runtime phase** (when you run `.\start-api.ps1` or `.\start-service.ps1`):
   - App checks if the database exists
   - If not, it creates it automatically
   - Applies all the migrations
   - Seeds some initial data (like the default tags)
   - Starts up and you're good to go

**No manual database setup needed!** Everything happens automatically through `context.Database.Migrate()`.

#### If You Need to Manage Migrations Manually

If you ever need to tweak the migrations yourself:

```bash
# Add a new migration
cd TaskManagement.API
dotnet ef migrations add MigrationName --project ..\TaskManagement.Infrastructure\TaskManagement.Infrastructure.csproj

# Undo the last migration (only if it hasn't been applied)
dotnet ef migrations remove --project ..\TaskManagement.Infrastructure\TaskManagement.Infrastructure.csproj
```

You don't need `dotnet ef database update` because the app does that automatically when it starts.

### 3. Configuration

#### Backend Configuration (`appsettings.json`)

```json
{
  "Pagination": {
    "DefaultPageSize": 10,
    "MaxPageSize": 100
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaskManagementDb;..."
  }
}
```

#### Frontend Configuration (`.env.development`, `.env.production`)

```env
VITE_API_BASE_URL=http://localhost:5119/api/v1.0
VITE_DEFAULT_PAGE_SIZE=10
```

#### Environment Variables

Set this environment variable on your API server to configure CORS:

```bash
# Windows PowerShell
$env:CORS_ORIGINS = "http://localhost:5173,http://localhost:3000"

# Production example
$env:CORS_ORIGINS = "https://yourdomain.com,https://www.yourdomain.com"
```

If you don't set it during development, it'll default to `http://localhost:5173` and `http://localhost:3000`.

### 4. Install RabbitMQ

#### Windows:
1. Install Erlang: [Download](https://www.erlang.org/downloads)
2. Install RabbitMQ: [Download](https://www.rabbitmq.com/install-windows.html)
3. Enable RabbitMQ Management Plugin:
   ```bash
   rabbitmq-plugins enable rabbitmq_management
   ```
4. Access management UI at `http://localhost:15672` (default credentials: guest/guest)

#### Linux/Mac:
```bash
# Using Docker
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management
```

### 4. Install RabbitMQ

```bash
# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build
```

### 5. Backend Setup

```bash
cd task-management-ui
npm install
```

Update the API base URL in `src/services/api.ts` if needed (default: `http://localhost:5119/api`).

## Running the Application

### Quick Start with PowerShell Scripts (Windows)

For convenience, PowerShell scripts are provided in the root directory:

```powershell
# Setup: Install dependencies and apply migrations (run once)
.\setup.ps1

# Start the API
.\start-api.ps1

# Start the Windows Service
.\start-service.ps1

# Start the Frontend
.\start-frontend.ps1
```

### Manual Start

#### 1. Start the Backend API

```bash
cd TaskManagement.API
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5119`
- Swagger UI: `http://localhost:5119/swagger`

#### 2. Start the Windows Service

```bash
cd TaskManagement.Service
dotnet run
```

The service will:
- Check for overdue tasks every 5 minutes
- Publish reminders to the RabbitMQ queue
- Consume and log reminder messages

#### 3. Start the Frontend

```bash
cd task-management-ui
npm run dev
```

The React app will be available at `http://localhost:5173`

## Architecture

I organized the app using clean architecture principles with these layers:

1. **TaskManagement.Core** - The domain stuff: entities, DTOs, and interfaces
2. **TaskManagement.Infrastructure** - Data access with EF Core and repositories
3. **TaskManagement.API** - The RESTful API controllers and configuration
4. **TaskManagement.Service** - Background Windows Service for task monitoring
5. **task-management-ui** - React frontend with TypeScript and Material-UI

### Design Patterns I Used

1. **Repository Pattern**: Keeps the data access logic clean and separate
2. **Dependency Injection**: Used everywhere for loose coupling
3. **DTO Pattern**: Keeps domain models separate from what the API sends
4. **Flux Pattern**: Redux's unidirectional data flow
5. **Producer-Consumer Pattern**: RabbitMQ message queue
6. **Clean Architecture**: Each layer has its own job

## Tech Stack

### Backend
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- SQL Server
- RabbitMQ.Client 7.2.0
- Swashbuckle (Swagger)
- Asp.Versioning.Mvc 8.1.0 (API Versioning)
- AutoMapper 12.0.1 (DTO Mapping)
- Serilog (Structured Logging)

### Frontend
- React 18 with TypeScript
- Vite
- Redux Toolkit for state management
- Material-UI (MUI) for UI components
- Axios for HTTP requests
- Vitest & React Testing Library

### Infrastructure
- SQL Server for data persistence
- RabbitMQ for message queuing
- Code-First migrations

## Testing

I've written comprehensive tests for both the frontend and backend.

### Backend Tests

```powershell
# Run all backend tests
cd TaskManagement.Tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**What's Tested:**
- **56 test cases** covering:
  - Repository Tests (TaskItemRepository: 17 tests, TagRepository: 8 tests)
    - CRUD operations with soft delete
    - Pagination edge cases (empty pages, out of bounds, normalization)
    - Query optimization checks (AsNoTracking)
  - Controller Tests (TasksController: 5 tests, TagsController: 8 tests)
    - CRUD endpoints and validation
    - Response caching
  - Service Tests
    - RabbitMQ handling 50+ concurrent messages
    - Windows Service Worker concurrent task processing
    - Dead letter queue with retry logic
  - Using xUnit, Moq, and FluentAssertions

### Frontend Tests

```powershell
# Run all frontend tests
cd task-management-ui
npm test

# Run with UI
npm run test:ui

# Run with coverage
npm run test:coverage
```

**What's Tested:**
- **21 test cases** covering:
  - React Components (TaskForm: 11 tests, TaskList: 4 tests)
    - Form validation (required fields, email format, phone format)
    - User interactions and submissions
  - Redux Store (taskSlice: 6 tests)
    - State management and async operations
    - Pagination state
  - Using Vitest and React Testing Library

Check out [TESTING.md](TESTING.md) for more detailed testing docs.

## Project Structure

```
SUPERCOM TEST/
├── TaskManagement.sln
├── setup.ps1                    # Setup script: install dependencies & migrations
├── start-api.ps1                # Start API server
├── start-service.ps1            # Start Windows Service
├── start-frontend.ps1           # Start React frontend
├── TaskManagement.API/          # Web API project
│   ├── Controllers/
│   │   ├── TasksController.cs
│   │   └── TagsController.cs
│   ├── Program.cs
│   └── appsettings.json
├── TaskManagement.Core/         # Domain layer
│   ├── Entities/
│   │   ├── TaskItem.cs
│   │   ├── Tag.cs
│   │   └── TaskItemTag.cs
│   ├── DTOs/
│   │   ├── TaskItemDto.cs
│   │   └── TagDto.cs
│   └── Interfaces/
│       ├── ITaskItemRepository.cs
│       └── ITagRepository.cs
├── TaskManagement.Infrastructure/  # Data access layer
│   ├── Data/
│   │   └── TaskManagementDbContext.cs
│   ├── Repositories/
│   │   ├── TaskItemRepository.cs
│   │   └── TagRepository.cs
│   └── Migrations/
├── TaskManagement.Service/      # Windows Service
│   ├── Services/
│   │   └── RabbitMQService.cs
│   ├── Worker.cs
│   └── Program.cs
└── task-management-ui/          # React frontend
    ├── src/
    │   ├── components/
    │   │   ├── TaskList.tsx
    │   │   └── TaskForm.tsx
    │   ├── store/
    │   │   ├── store.ts          # Redux store configuration
    │   │   ├── taskSlice.ts      # Redux slice with actions and reducers
    │   │   └── hooks.ts          # Typed Redux hooks
    │   ├── services/
    │   │   └── api.ts
    │   ├── types/
    │   │   └── index.ts
    │   ├── App.tsx
    │   └── main.tsx
    ├── REDUX_MIGRATION.md        # Redux migration documentation
    └── package.json
```

## What Makes This App Cool

### API Features
- **API Versioning**: URL-based versioning (`/api/v1.0/*`) so updates don't break old clients
- **Pagination**: Server-side pagination that you can configure (default: 10 items, max: 100)
  - The PagedResult tells you everything: items, current page, total pages, total count, whether there are more pages
  - Frontend has a nice UI with page selectors and page size dropdown
- **AutoMapper**: Automatically maps entities to DTOs (saves a ton of boilerplate)
- **Global Exception Handling**: RFC 7807 Problem Details for consistent, clean error responses
- **Response Caching**: In-memory caching (tags endpoint caches for 5 minutes)
- **Soft Delete**: Never actually delete data - just mark it with `IsDeleted` and `DeletedAt`
- **Query Optimization**: Using `AsNoTracking()` for read-only stuff, split queries for performance
- **CORS Configuration**: Environment-variable based so you can deploy anywhere
- **Structured Logging**: Serilog outputs to both console and files
- **XML Documentation**: Everything's documented in Swagger UI

### Task Management
- **Create Tasks**: Add new tasks with full validation
- **View Tasks**: See all tasks with color-coded priorities and status
- **Update Tasks**: Edit tasks with forms that pre-fill the data
- **Delete Tasks**: Remove tasks (with a confirmation dialog, of course)
- **Tag Assignment**: Assign multiple tags from a predefined list
- **Due Date Tracking**: Visual warnings for overdue tasks

### Validation
- **Frontend Validation**: 
  - **Title**: Required, maximum 200 characters
  - **Description**: Required, maximum 2000 characters
  - **Email**: Required, valid email format (regex: `^[^\s@]+@[^\s@]+\.[^\s@]+$`)
  - **Telephone**: Required, only digits/spaces/+/-/(), minimum 7 digits, real-time character filtering
  - **Full Name**: Required, maximum 100 characters
  - **Due Date**: Required
  - **Tags**: At least one tag required
  - Real-time validation feedback with error messages
  - Form submission prevention until all validations pass

- **Backend Validation**:
  - Data annotations on entities
  - Model state validation in controllers
  - Database constraints

### Windows Service Features
- **Automatic Overdue Detection**: Checks for overdue tasks every 5 minutes
- **RabbitMQ Integration**: Sends reminder messages to a durable queue with dead letter exchange
- **Dead Letter Queue**: 3-retry logic with exponential backoff for failed messages
- **Message Consumption**: Listens to the queue and logs reminders
- **Concurrent Updates**: Handles multiple task updates safely through the queue
- **Error Handling**: Robust error handling with structured logging

### State Management
- **Redux Toolkit**: Modern Redux that's way simpler to set up
- **Typed Hooks**: `useAppDispatch` and `useAppSelector` for type safety
- **Async Thunks**: Handles all the async stuff (loadTasks, createTask, updateTask, deleteTask)
- **Optimistic Updates**: UI updates immediately for a snappy feel
- **Error Handling**: All errors managed in one place in Redux state
- **Loading States**: Separate loading flags for different operations
- **Duplicate Request Prevention**: Built-in checks so rapid clicking doesn't create duplicate tasks

## API Endpoints

All endpoints are versioned with `/api/v1.0/` prefix.

### Tasks

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1.0/tasks?page=1&pageSize=10` | Get paginated tasks |
| GET | `/api/v1.0/tasks/{id}` | Get task by ID |
| POST | `/api/v1.0/tasks` | Create a new task |
| PUT | `/api/v1.0/tasks/{id}` | Update a task |
| DELETE | `/api/v1.0/tasks/{id}` | Soft delete a task |

### Tags

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1.0/tags` | Get all tags (cached for 5 minutes) |
| GET | `/api/v1.0/tags/{id}` | Get tag by ID |
| POST | `/api/v1.0/tags` | Create a new tag |
| PUT | `/api/v1.0/tags/{id}` | Update a tag |
| DELETE | `/api/v1.0/tags/{id}` | Soft delete a tag |

### Example Request - Create Task

```json
POST /api/tasks
{
  "title": "Complete project documentation",
  "description": "Write comprehensive README and API documentation",
  "dueDate": "2025-12-01T17:00:00",
  "priority": 4,
  "fullName": "John Doe",
  "telephone": "+1-555-123-4567",
  "email": "john.doe@example.com",
  "tagIds": [1, 3, 8]
}
```

## Database Schema

### TaskItems Table
- `Id` (int, PK): Primary key
- `Title` (nvarchar(200)): Task title
- `Description` (nvarchar(2000)): Task description
- `DueDate` (datetime2): Due date and time
- `Priority` (int): Priority level (1-5: VeryLow, Low, Medium, High, Critical)
- `FullName` (nvarchar(100)): User's full name
- `Telephone` (nvarchar(20)): User's phone number
- `Email` (nvarchar(100)): User's email address
- `CreatedAt` (datetime2): Creation timestamp
- `UpdatedAt` (datetime2, nullable): Last update timestamp
- `IsDeleted` (bit): Soft delete flag (default: false)
- `DeletedAt` (datetime2, nullable): Deletion timestamp

### Tags Table
- `Id` (int, PK): Primary key
- `Name` (nvarchar(50), unique): Tag name
- `IsDeleted` (bit): Soft delete flag (default: false)
- `DeletedAt` (datetime2, nullable): Deletion timestamp

### TaskItemTags Table (Junction Table)
- `TaskItemId` (int, PK, FK): Foreign key to TaskItems
- `TagId` (int, PK, FK): Foreign key to Tags
- Composite primary key: (TaskItemId, TagId)

### Seeded Tags
The database is pre-seeded with the following tags:
- Urgent
- Important
- Work
- Personal
- Home
- Shopping
- Meeting
- Project
- Research
- Development

## SQL Query for Tasks with Multiple Tags

This query returns tasks that have at least two tags, including the tag names, sorted by the number of tags in descending order:

```sql
SELECT 
    t.Id,
    t.Title,
    t.Description,
    t.DueDate,
    t.Priority,
    t.FullName,
    t.Telephone,
    t.Email,
    COUNT(tit.TagId) AS TagCount,
    STRING_AGG(tag.Name, ', ') AS TagNames
FROM 
    TaskItems t
INNER JOIN 
    TaskItemTags tit ON t.Id = tit.TaskItemId
INNER JOIN 
    Tags tag ON tit.TagId = tag.Id
GROUP BY 
    t.Id,
    t.Title,
    t.Description,
    t.DueDate,
    t.Priority,
    t.FullName,
    t.Telephone,
    t.Email
HAVING 
    COUNT(tit.TagId) >= 2
ORDER BY 
    TagCount DESC,
    t.Title ASC;
```

## Windows Service

The Windows Service (`TaskManagement.Service`) runs in the background, keeping an eye on tasks and sending reminders.

### What It Does
- **Continuous Monitoring**: Runs as a Windows Service, always checking for overdue tasks
- **Comprehensive Logging**: File-based logging with Serilog (daily rolling files)
- **Health Monitoring**: Built-in health checks for database and RabbitMQ
- **RabbitMQ Integration**: Publishes and consumes task reminder messages
- **Graceful Error Handling**: Keeps running even if RabbitMQ goes down
- **Configurable Intervals**: You can change how often it checks via appsettings

### Background Worker
- Runs continuously as a hosted service
- Checks for overdue tasks every 5 minutes (you can change this)
- Uses scoped DbContext for database stuff
- Does health checks on each cycle

### RabbitMQ Integration
- **Publisher**: Sends task reminder messages to the `task_reminders` queue
- **Consumer**: Listens to the `task_reminders` queue and logs messages
- **Message Format**: JSON with task details (Id, Title, DueDate, FullName, Email)
- **Queue Configuration**: Durable queue with manual acknowledgment for reliability
- **Auto-reconnection**: Tries to reconnect automatically if the connection drops
- **Concurrent Update Handling**: Built to handle tons of concurrent operations
  - Thread-safe message publishing and consumption
  - Proper message acknowledgment to prevent duplicates
  - Handles 100+ concurrent publish/consume operations like a champ
  - Maintains data integrity during concurrent task processing
  - FIFO message ordering guaranteed within the queue
  - Backpressure handling for variable loads
  - Thoroughly tested for concurrent scenarios (see Testing section)

### Logging

Both the API and Service use Serilog for logging everything.

#### Where to Find Logs
- **API Logs**: `TaskManagement.API/Logs/api-log-YYYYMMDD.txt`
- **Service Logs**: `TaskManagement.Service/Logs/service-log-YYYYMMDD.txt`

#### Log Configuration
You can tweak logging in `appsettings.json`:

```json
"LoggingConfig": {
  "LogDirectory": "Logs",
  "RetainedFileCountLimit": 10,
  "FileSizeLimitMB": 10
}
```

#### What You Get
- **Console Output**: Colored console logs for development and debugging
- **File Output**: Daily rolling log files with timestamps and structured format
- **Log Levels**: Information, Warning, Error with structured logging
- **Retention**: Configurable file count (default: 10 files) and size limit (default: 10MB per file)
- **Format**: `{Timestamp} [{Level}] {SourceContext}: {Message}`
- **Repository Logging**: All database operations logged with parameters
- **EF Core Logging**: Database queries and connection info (Warning level)
- **Auto-created Directory**: Logs directory creates itself if it doesn't exist

#### Viewing Logs

```powershell
# View recent service logs
Get-Content "TaskManagement.Service\Logs\service-log-*.txt" -Tail 20

# Monitor service logs in real-time
Get-Content "TaskManagement.Service\Logs\service-log-*.txt" -Wait -Tail 10

# View recent API logs
Get-Content "TaskManagement.API\Logs\api-log-*.txt" -Tail 20

# Monitor API logs in real-time
Get-Content "TaskManagement.API\Logs\api-log-*.txt" -Wait -Tail 10
```

### Installation as Windows Service

#### What You Need
- Windows 10/Server 2016 or later
- .NET 10 Runtime installed
- SQL Server running somewhere
- Administrator privileges to install services

#### Step 1: Build and Publish
```bash
# Navigate to service directory
cd TaskManagement.Service

# Publish it
dotnet publish -c Release -o publish
```

#### Step 2: Install Windows Service (Run PowerShell as Admin)
```powershell
# Delete existing service if it exists
C:\Windows\System32\sc.exe delete "TaskManagementService"

# Create new service
New-Service -Name "TaskManagementService" `
    -BinaryPathName "E:\SUPERCOM TEST\TaskManagement.Service\publish\TaskManagement.Service.exe" `
    -DisplayName "Task Management Service" `
    -Description "Task Management Background Service for overdue task monitoring" `
    -StartupType Manual

# Start the service
Start-Service -Name "TaskManagementService"

# Verify service status
Get-Service -Name "TaskManagementService"
```

#### Step 3: Configure Service (Optional)
```powershell
# Make it start automatically
Set-Service -Name "TaskManagementService" -StartupType Automatic

# Set up recovery options if it crashes
C:\Windows\System32\sc.exe failure "TaskManagementService" reset= 86400 actions= restart/5000/restart/5000/restart/5000
```

### Configuration

#### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "Logs/service-log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 10,
          "fileSizeLimitBytes": 10485760
        }
      }
    ]
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "Port": 5672,
    "TaskQueue": "task_reminders"
  },
  "CheckIntervalMinutes": 5
}
```

### Service Management Commands

#### Start/Stop/Restart
```powershell
# Start service
Start-Service -Name "TaskManagementService"

# Stop service
Stop-Service -Name "TaskManagementService"

# Restart service
Restart-Service -Name "TaskManagementService"

# Check status
Get-Service -Name "TaskManagementService"
```

#### View Logs
```powershell
# View recent logs
Get-Content "E:\SUPERCOM TEST\TaskManagement.Service\publish\Logs\service-log-*.txt" -Tail 20

# Monitor logs in real-time
Get-Content "E:\SUPERCOM TEST\TaskManagement.Service\publish\Logs\service-log-*.txt" -Wait -Tail 10
```

#### Uninstall Service
```powershell
# Stop and remove service
Stop-Service -Name "TaskManagementService" -Force
C:\Windows\System32\sc.exe delete "TaskManagementService"
```

### Troubleshooting

#### Service Won't Start
1. **Check logs**: Look at the service logs to see what's wrong
2. **Verify permissions**: Make sure the service account can access the database
3. **Test console mode**: Try running `TaskManagement.Service.exe` directly to see if it works
4. **Check dependencies**: Verify SQL Server and .NET Runtime are available

#### Database Connection Issues
```bash
# Test the connection by running it directly
dotnet run --project TaskManagement.Service
```

#### RabbitMQ Issues
- The service keeps running even without RabbitMQ
- Check if RabbitMQ is running: `Get-Service -Name "RabbitMQ"`
- Try accessing the management UI: `http://localhost:15672`

### Message Flow
1. Service queries database for tasks where `DueDate < DateTime.UtcNow`
2. For each overdue task, it publishes a JSON message to RabbitMQ
3. The same service grabs the message from the queue
4. Creates a log entry: `"REMINDER: Hi your Task is due - {message}"`

### Concurrent Update Handling
- Queue-based processing means tasks get processed one at a time
- Database transactions prevent race conditions
- Entity Framework's change tracking manages concurrent updates
- Messages only get acknowledged after successful processing

## Summary

This Task Management app shows off a solid full-stack implementation with:

- **Clean Architecture**: Proper separation of concerns across Core, Infrastructure, API, and Service layers
- **Modern Frontend**: React 18 with TypeScript, Redux Toolkit, and Material-UI
- **RESTful API**: ASP.NET Core Web API with Swagger docs
- **Database Management**: Entity Framework Core with Code-First migrations
- **Background Processing**: Windows Service with RabbitMQ message queuing
- **Comprehensive Logging**: Serilog for both file and console outputs
- **Production-Ready**: Validation, error handling, and health checks

Everything's documented here so you can understand how it works and build on it if you want.

---

## Git Repository

This project is properly set up as a Git repository with all the source code committed.

### What's In Here

✅ All source code files (.cs, .tsx, .ts, .json, .csproj, .sln)  
✅ Setup scripts (setup.ps1, start-api.ps1, start-service.ps1, start-frontend.ps1)  
✅ Config files (appsettings.json, docker-compose.yml)  
✅ Documentation (README.md, TESTING.md)  
✅ Test files (23 comprehensive unit tests)  
✅ Proper .gitignore (excludes build stuff, node_modules, logs)

### Repository Structure

```
commit 78c6af6
Author: Adam Bickels
Date: November 28, 2025

    Initial commit: Task Management Application for SUPERCOM
    
    - 73 files changed, 12,088 insertions
    - Complete source code for all layers
    - All tests included (23 passing tests)
    - Setup scripts for easy deployment
    - Comprehensive documentation
```

### How to Submit/Share This

**Option 1: Push to GitHub/GitLab/Bitbucket**

```powershell
# Add your remote
git remote add origin <your-repository-url>

# Push it
git push -u origin master
```

**Option 2: Create a ZIP**

```powershell
# Create a clean zip without build artifacts
git archive -o task-management-app.zip HEAD
```

**Option 3: Share the Folder**

Just share this folder as-is. Anyone who gets it can:
1. Copy the folder
2. Run `git log` to see the history
3. Run `.\setup.ps1` to get started

### Before You Send It

Double-check:

- [x] All source code is committed
- [x] Build artifacts are excluded (.gitignore)
- [x] Documentation is complete
- [x] Setup scripts work
- [x] Tests pass (run `dotnet test`)
- [x] App runs with the provided scripts
- [x] README has everything
- [x] SQL query for 2+ tags is documented
- [x] Concurrent update handling is tested

---

## About This Project

I built this app as a full-stack developer assignment for SUPERCOM to show off my skills in:
- Full-stack development with .NET and React
- Clean architecture and design patterns
- Database design with Entity Framework Core
- Background services and message queuing
- Modern frontend with TypeScript and Redux
- RESTful API design
- Handling concurrent updates with RabbitMQ
- Writing comprehensive tests

**Developer:** Adam Bickels  
**Date:** November 2025  
**Purpose:** Technical Assessment for SUPERCOM

---

## Contact

Got questions about this? Reach out:
- **Developer:** Adam Bickels
- **Assignment:** SUPERCOM Full-Stack Developer Home Assignment
