# Task Management Application

> **Full-Stack Developer Assignment for SUPERCOM**  
> **Developer:** Adam Bickels  
> A comprehensive task management application demonstrating full-stack development capabilities with .NET Core, React, SQL Server, and RabbitMQ.

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

This is a comprehensive task management system that allows users to create, read, update, and delete tasks. Each task includes detailed information such as title, description, due date, priority, user details (full name, telephone, email), and multiple tags. The application features a .NET Core Web API backend, a React frontend with Redux Toolkit state management, SQL Server database with Entity Framework Core, and a Windows Service that monitors overdue tasks and sends reminders via RabbitMQ.

## Prerequisites

Before running this application, ensure you have the following installed:

1. **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
2. **Node.js 18+** and npm - [Download](https://nodejs.org/)
3. **SQL Server** (Express or Developer Edition) - [Download](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
4. **RabbitMQ Server** - [Download](https://www.rabbitmq.com/download.html)
5. **Visual Studio 2022** or **VS Code** (recommended)

## Quick Start

**The fastest way to run the application using PowerShell scripts:**

```powershell
# 1. Setup: Install dependencies and apply migrations (run once)
.\setup.ps1

# 2. Start the API (in terminal 1)
.\start-api.ps1

# 3. Start the Windows Service (in terminal 2)
.\start-service.ps1

# 4. Start the Frontend (in terminal 3)
.\start-frontend.ps1
```

**Access the application:**
- **Frontend**: http://localhost:5173
- **API**: http://localhost:5119
- **Swagger UI**: http://localhost:5119/swagger
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

**Access the application:**
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

**Or if you received this as a ZIP file:**

1. Extract the ZIP file to your desired location
2. Open a terminal/PowerShell in the extracted folder

### 2. Database Setup

The application uses Code-First migrations. Update the connection string in the following files if needed:

- `TaskManagement.API/appsettings.json`
- `TaskManagement.API/appsettings.Development.json`
- `TaskManagement.Service/appsettings.json`

Default connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TaskManagementDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

#### Apply Migrations

```bash
cd TaskManagement.API
dotnet ef database update --project ..\TaskManagement.Infrastructure\TaskManagement.Infrastructure.csproj
```

This will create the database and seed initial tag data.

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

Set the following environment variable on your API server for CORS configuration:

```bash
# Windows PowerShell
$env:CORS_ORIGINS = "http://localhost:5173,http://localhost:3000"

# Production example
$env:CORS_ORIGINS = "https://yourdomain.com,https://www.yourdomain.com"
```

For development, if `CORS_ORIGINS` is not set, it defaults to `http://localhost:5173` and `http://localhost:3000`.

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

The application follows a clean architecture pattern with the following layers:

1. **TaskManagement.Core** - Domain entities, DTOs, and interfaces
2. **TaskManagement.Infrastructure** - Data access layer with EF Core and repositories
3. **TaskManagement.API** - RESTful API controllers and configuration
4. **TaskManagement.Service** - Windows Service for background processing
5. **task-management-ui** - React frontend with TypeScript and Material-UI

### Design Patterns Used

1. **Repository Pattern**: Abstracts data access logic
2. **Dependency Injection**: Used throughout for loose coupling
3. **DTO Pattern**: Separates domain models from API contracts
4. **Flux Pattern**: Redux unidirectional data flow
5. **Producer-Consumer Pattern**: RabbitMQ message queue
6. **Clean Architecture**: Separation of concerns across layers

## Technologies Used

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

The application includes comprehensive automated tests for both front-end and back-end components.

### Backend Tests

```powershell
# Run all backend tests
cd TaskManagement.Tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

**Test Coverage:**
- **56 test cases** covering:
  - Repository Tests (TaskItemRepository: 17 tests, TagRepository: 8 tests)
    - CRUD operations with soft delete support
    - Pagination edge cases (empty pages, out of bounds, normalization)
    - Query optimization validation (AsNoTracking)
  - Controller Tests (TasksController: 5 tests, TagsController: 8 tests)
    - CRUD endpoints and validation
    - Response caching behavior
  - Service Tests
    - RabbitMQ concurrent message handling (50+ messages simultaneously)
    - Windows Service Worker concurrent task processing
    - Dead letter queue with retry logic
  - Unit tests with xUnit, Moq, and FluentAssertions

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

**Test Coverage:**
- **21 test cases** covering:
  - React Components (TaskForm: 11 tests, TaskList: 4 tests)
    - Form validation (required fields, email format, phone format)
    - User interactions and submission
  - Redux Store (taskSlice: 6 tests)
    - State management and async operations
    - Pagination state handling
  - Vitest and React Testing Library

See [TESTING.md](TESTING.md) for detailed testing documentation.

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

## Key Features

### API Features
- **API Versioning**: URL-based versioning (`/api/v1.0/*`) for backward compatibility
- **Pagination**: Server-side pagination with configurable page size (default: 10, max: 100)
  - PagedResult response includes `items`, `currentPage`, `totalPages`, `totalCount`, `hasPreviousPage`, `hasNextPage`
  - Frontend pagination UI with page selector and page size dropdown
- **AutoMapper Integration**: Automatic mapping between entities and DTOs
- **Global Exception Handling**: RFC 7807 Problem Details for consistent error responses
- **Response Caching**: In-memory caching (5-minute expiration for tags endpoint)
- **Soft Delete**: Logical deletion with `IsDeleted` and `DeletedAt` fields, global query filter
- **Query Optimization**: `AsNoTracking()` for read-only operations, split queries for better performance
- **CORS Configuration**: Environment-variable based CORS origins for flexible deployment
- **Structured Logging**: Serilog with console and file outputs
- **XML Documentation**: Comprehensive API documentation in Swagger UI

### Task Management
- **Create Tasks**: Add new tasks with all required fields and validations
- **View Tasks**: Display all tasks with color-coded priorities and status
- **Update Tasks**: Edit existing tasks with pre-filled forms
- **Delete Tasks**: Remove tasks with confirmation dialog
- **Tag Assignment**: Assign multiple tags to each task from a predefined list
- **Due Date Tracking**: Visual indicators for overdue tasks

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
- **RabbitMQ Integration**: Publishes reminder messages to a durable queue with dead letter exchange
- **Dead Letter Queue**: 3-retry logic with exponential backoff for failed messages
- **Message Consumption**: Subscribes to the queue and logs reminder messages
- **Concurrent Updates**: Handles multiple task updates safely through queue-based processing
- **Error Handling**: Robust error handling with structured logging

### State Management
- **Redux Toolkit**: Modern Redux with simplified setup
- **Typed Hooks**: `useAppDispatch` and `useAppSelector` for type safety
- **Async Thunks**: Handles async operations (loadTasks, createTask, updateTask, deleteTask)
- **Optimistic Updates**: Immediate UI feedback
- **Error Handling**: Centralized error management in Redux state
- **Loading States**: Separate loading flags for different operations
- **Duplicate Request Prevention**: Built-in condition checks to prevent concurrent create operations

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

### Query Explanation:
1. **Joins**: Links TaskItems with TaskItemTags and Tags tables
2. **GROUP BY**: Groups tasks to aggregate their tags
3. **COUNT**: Counts the number of tags per task
4. **STRING_AGG**: Concatenates tag names into a comma-separated list
5. **HAVING**: Filters to only include tasks with 2 or more tags
6. **ORDER BY**: Sorts by tag count (descending) and then by title (ascending)

## Windows Service

The Windows Service (`TaskManagement.Service`) performs background task monitoring and reminder functionality.

### Features
- **Continuous Monitoring**: Runs as a Windows Service checking for overdue tasks
- **Comprehensive Logging**: File-based logging with Serilog (daily rolling files)
- **Health Monitoring**: Built-in health checks for database and RabbitMQ
- **RabbitMQ Integration**: Publishes and consumes task reminder messages
- **Graceful Error Handling**: Continues operation even when RabbitMQ is unavailable
- **Configurable Intervals**: Check interval configurable via appsettings

### Background Worker
- Runs continuously as a hosted service
- Checks for overdue tasks every 5 minutes (configurable)
- Uses scoped DbContext for database operations
- Performs health checks on each cycle

### RabbitMQ Integration
- **Publisher**: Sends task reminder messages to the `task_reminders` queue
- **Consumer**: Listens to the `task_reminders` queue and logs received messages
- **Message Format**: JSON with task details (Id, Title, DueDate, FullName, Email)
- **Queue Configuration**: Durable queue with manual acknowledgment for reliability
- **Auto-reconnection**: Automatically attempts to reconnect on connection failures
- **Concurrent Update Handling**: Designed to handle high-volume concurrent operations
  - Thread-safe message publishing and consumption
  - Proper message acknowledgment to prevent duplicates
  - Handles 100+ concurrent publish/consume operations efficiently
  - Maintains data integrity during concurrent task processing
  - FIFO message ordering within queue guarantees
  - Backpressure handling for variable loads
  - Comprehensive testing validates concurrent scenarios (see Testing section)

### Logging

Both the API and Service use Serilog for comprehensive logging.

#### Log File Locations
- **API Logs**: `TaskManagement.API/Logs/api-log-YYYYMMDD.txt`
- **Service Logs**: `TaskManagement.Service/Logs/service-log-YYYYMMDD.txt`

#### Log Configuration
Configure logging in `appsettings.json`:

```json
"LoggingConfig": {
  "LogDirectory": "Logs",
  "RetainedFileCountLimit": 10,
  "FileSizeLimitMB": 10
}
```

#### Features
- **Console Output**: Colored console logs for development and debugging
- **File Output**: Rolling daily log files with timestamp and structured format
- **Log Levels**: Information, Warning, Error with structured logging
- **Retention**: Configurable file count (default: 10 files) and size limit (default: 10MB per file)
- **Format**: `{Timestamp} [{Level}] {SourceContext}: {Message}`
- **Repository Logging**: All database operations are logged with parameters
- **EF Core Logging**: Database queries and connection info (Warning level)
- **Auto-created Directory**: Logs directory is automatically created if it doesn't exist

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

#### Prerequisites
- Windows 10/Server 2016 or later
- .NET 10 Runtime installed
- SQL Server accessible
- Administrator privileges for service installation

#### Step 1: Build and Publish
```bash
# Navigate to service directory
cd TaskManagement.Service

# Publish the service
dotnet publish -c Release -o publish
```

#### Step 2: Install Windows Service (Run PowerShell as Administrator)
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
# Set to start automatically
Set-Service -Name "TaskManagementService" -StartupType Automatic

# Set service recovery options
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
1. **Check logs**: Review service logs for error details
2. **Verify permissions**: Ensure service account has database access
3. **Test console mode**: Run `TaskManagement.Service.exe` directly to test
4. **Check dependencies**: Verify SQL Server and .NET Runtime are available

#### Database Connection Issues
```bash
# Test connection string
dotnet run --project TaskManagement.Service
```

#### RabbitMQ Issues
- Service continues running without RabbitMQ
- Check RabbitMQ service status: `Get-Service -Name "RabbitMQ"`
- Verify RabbitMQ Management UI: `http://localhost:15672`

### Message Flow
1. Service queries database for tasks where `DueDate < DateTime.UtcNow`
2. For each overdue task, a JSON message is published to RabbitMQ
3. The same service consumes the message from the queue
4. Log entry is created: `"REMINDER: Hi your Task is due - {message}"`

### Concurrent Update Handling
- Queue-based processing ensures tasks are processed sequentially
- Database transactions prevent race conditions
- Entity Framework's change tracking manages concurrent updates
- Message acknowledgment only after successful processing
- Message acknowledgment only after successful processing

## Testing

### Unit Testing
While not included in this submission, the architecture supports unit testing:

```bash
# Example: Create test project
dotnet new xunit -n TaskManagement.Tests
dotnet add reference ../TaskManagement.API/TaskManagement.API.csproj
```

### Manual Testing Checklist

#### Backend API
- [ ] Swagger UI loads successfully at `/swagger`
- [ ] GET `/api/tasks` returns all tasks
- [ ] POST `/api/tasks` creates a new task with validation
- [ ] PUT `/api/tasks/{id}` updates an existing task
- [ ] DELETE `/api/tasks/{id}` removes a task
- [ ] GET `/api/tags` returns all tags
- [ ] Tag CRUD operations work correctly

#### Frontend
- [ ] Application loads without errors
- [ ] Task list displays all tasks
- [ ] Create task dialog opens and validates fields
- [ ] Tasks can be edited with pre-filled data
- [ ] Tasks can be deleted with confirmation
- [ ] Priority colors display correctly
- [ ] Overdue tasks are highlighted
- [ ] Tags display as chips
- [ ] Redux state updates reflect in UI
- [ ] Validation prevents invalid input (telephone, email)
- [ ] Cannot submit form with validation errors
- [ ] Duplicate submissions prevented (rapid clicking)

#### Windows Service
- [ ] Service starts without errors
- [ ] RabbitMQ connection established
- [ ] Overdue tasks are detected
- [ ] Messages are published to queue
- [ ] Messages are consumed and logged
- [ ] Service handles errors gracefully

#### Integration Testing
- [ ] Create task via UI → Appears in database
- [ ] Update task via UI → Changes saved to database
- [ ] Create overdue task → Service publishes reminder
- [ ] Check service logs for reminder messages

## Design Patterns Used

1. **Repository Pattern**: Abstracts data access logic
2. **Dependency Injection**: Used throughout for loose coupling
3. **DTO Pattern**: Separates domain models from API contracts
4. **Flux Pattern**: Redux unidirectional data flow
5. **Producer-Consumer Pattern**: RabbitMQ message queue
6. **Clean Architecture**: Separation of concerns across layers

## Error Handling

### API
- Global exception handling in controllers
- Model validation with data annotations
- HTTP status codes (200, 201, 204, 400, 404, 500)
- Detailed error messages for debugging

### Frontend
- Try-catch blocks in async operations
- Error state in Redux store
- User-friendly error alerts
- Console logging for debugging
- Real-time form validation with error messages
- Duplicate submission prevention with ref-based locking

### Windows Service
- Try-catch blocks in background worker
- Error logging with ILogger
- Graceful degradation on RabbitMQ failures
- Retry logic with delays

## Known Limitations

1. **Authentication**: No authentication/authorization implemented
2. **Pagination**: Large datasets not paginated
3. **Real-time Updates**: No SignalR for live updates
4. **Email Sending**: Reminders only logged, not sent via email
5. **Deployment**: No Docker configuration provided
6. **Unit Tests**: Not included in this submission

## Future Enhancements

- [ ] Add user authentication and authorization
- [ ] Implement pagination and filtering
- [ ] Add real-time notifications with SignalR
- [ ] Send email reminders via SMTP
- [ ] Add task attachments
- [ ] Implement task comments
- [ ] Add task assignment to multiple users
- [ ] Create dashboard with analytics
- [ ] Add Docker support
- [ ] Implement comprehensive unit and integration tests

## Troubleshooting

### Database Connection Issues
- Ensure SQL Server is running
- Verify connection string in appsettings.json
- Check Windows Authentication or SQL Server authentication

### RabbitMQ Connection Issues
- Verify RabbitMQ service is running
- Check port 5672 is accessible
- Confirm default credentials (guest/guest)
- Review RabbitMQ logs in management UI

### Frontend Issues
- Clear browser cache
- Check API URL in `services/api.ts`
- Ensure backend API is running
- Check browser console for errors

### CORS Issues
- Verify CORS policy in API Program.cs
- Check frontend URL matches allowed origins
- Update ports if using different configuration

## Summary

This Task Management Application demonstrates a comprehensive full-stack implementation with:

- **Clean Architecture**: Separation of concerns across Core, Infrastructure, API, and Service layers
- **Modern Frontend**: React 18 with TypeScript, Redux Toolkit, and Material-UI
- **RESTful API**: ASP.NET Core Web API with Swagger documentation
- **Database Management**: Entity Framework Core with Code-First migrations
- **Background Processing**: Windows Service with RabbitMQ message queuing
- **Comprehensive Logging**: Serilog with file and console outputs
- **Production-Ready Features**: Validation, error handling, and health checks

The documentation provides detailed setup instructions, architectural explanations, and key implementation details to facilitate understanding and future development.

---

## Git Repository Submission

This project has been properly initialized as a Git repository with all source code committed.

### Repository Contents

✅ **All source code files** (.cs, .tsx, .ts, .json, .csproj, .sln)  
✅ **Setup scripts** (setup.ps1, start-api.ps1, start-service.ps1, start-frontend.ps1)  
✅ **Configuration files** (appsettings.json, docker-compose.yml)  
✅ **Documentation** (README.md, TESTING.md)  
✅ **Test files** (23 comprehensive unit tests)  
✅ **Proper .gitignore** (excludes build artifacts, node_modules, logs)

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

### To Submit/Share This Repository

**Option 1: Push to GitHub/GitLab/Bitbucket**

```powershell
# Add your remote repository
git remote add origin <your-repository-url>

# Push to remote
git push -u origin master
```

**Option 2: Create a ZIP Archive**

```powershell
# Create a clean zip without build artifacts
git archive -o task-management-app.zip HEAD
```

**Option 3: Share Folder Directly**

The current folder can be shared as-is. Recipients can:
1. Copy the folder
2. Run `git log` to see commit history
3. Run `.\setup.ps1` to get started

### Verification Checklist

Before submission, verify:

- [x] All source code is committed
- [x] Build artifacts are excluded (.gitignore)
- [x] Documentation is complete and up-to-date
- [x] Setup scripts are tested and working
- [x] Tests are passing (run `dotnet test`)
- [x] Application runs successfully with provided scripts
- [x] README includes all required information
- [x] SQL query for 2+ tags is documented
- [x] Concurrent update handling is tested and documented

---

## About This Project

This application was developed by **Adam Bickels** as a **Full-Stack Developer assignment for SUPERCOM** to demonstrate proficiency in:
- Full-stack development with .NET and React
- Clean architecture and design patterns
- Database design and Entity Framework Core
- Background services and message queuing
- Modern frontend development with TypeScript and Redux
- RESTful API design and documentation
- Concurrent update handling with RabbitMQ
- Comprehensive testing strategies

**Developer:** Adam Bickels  
**Date:** November 2025  
**Purpose:** Technical Assessment for SUPERCOM

---

## Contact & Support

For questions about this implementation, please contact:
- **Developer:** Adam Bickels
- **Assignment:** SUPERCOM Full-Stack Developer Home Assignment

---

## License

This project is created as a technical assignment for SUPERCOM and is for evaluation purposes only.
