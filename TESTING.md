# Testing Guide

This document describes how to test the Task Management Application.

## Automated Testing

### Backend Tests (C# with xUnit)

The backend has comprehensive unit tests for controllers using xUnit, Moq, and FluentAssertions.

#### Running Backend Tests

```powershell
# Run all tests
cd TaskManagement.Tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity detailed

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

#### Test Coverage

- **TasksController Tests**: 10 test cases covering:
  - GetAllTasks - success scenario
  - GetTask - valid and invalid IDs
  - CreateTask - valid data and invalid tags
  - UpdateTask - valid data and mismatched IDs
  - DeleteTask - valid and invalid IDs

- **RabbitMQService Tests**: 9 comprehensive concurrency tests covering:
  - Single message publishing
  - Concurrent message publishing (50+ messages simultaneously)
  - Concurrent message consumption with order tracking
  - High-load publish and consume scenarios (100+ messages)
  - Real-world concurrent update simulations
  - Message acknowledgment with processing failures
  - Multiple service instances working independently
  - Message durability and persistence
  - Backpressure handling under variable load

- **WorkerConcurrencyTests**: 4 advanced concurrency tests covering:
  - Multiple concurrent overdue task checks
  - Concurrent message processing in parallel
  - Concurrent task updates with consistency validation
  - Message ordering and FIFO guarantees

**Total: 23 backend test cases** ensuring robust concurrent operation handling.

#### Adding New Backend Tests

1. Create test class in `TaskManagement.Tests` folder
2. Use Moq to mock dependencies
3. Follow AAA pattern (Arrange, Act, Assert)
4. Use FluentAssertions for readable assertions

Example:
```csharp
[Fact]
public async Task GetTask_WithValidId_ShouldReturnOkResult_WithTask()
{
    // Arrange
    var taskId = 1;
    _mockTaskRepository.Setup(repo => repo.GetByIdAsync(taskId))
        .ReturnsAsync(mockTask);

    // Act
    var result = await _controller.GetTask(taskId);

    // Assert
    var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
    var returnedTask = okResult.Value.Should().BeOfType<TaskItemDto>().Subject;
    returnedTask.Id.Should().Be(taskId);
}
```

### Frontend Tests (Vitest + React Testing Library)

The frontend has comprehensive unit tests for components and Redux store.

#### Running Frontend Tests

```powershell
# Run all tests
cd task-management-ui
npm test

# Run tests in watch mode
npm test -- --watch

# Run tests with UI
npm run test:ui

# Run tests with coverage
npm run test:coverage
```

#### Test Coverage

- **TaskForm Component Tests**: 7 test cases covering:
  - Form field rendering
  - Validation errors for empty fields
  - Form population when editing
  - Cancel button functionality
  - Email and telephone validation
  - Tag requirement validation

- **TaskList Component Tests**: 4 test cases covering:
  - Task rendering
  - Task details display
  - Tags display
  - Empty state

- **Redux Store Tests**: 6 test cases covering:
  - Initial state
  - setSelectedTask action
  - clearError action
  - loadTasks async thunk (pending, fulfilled, rejected)

#### Adding New Frontend Tests

1. Create test file in `__tests__` folder next to component
2. Use `@testing-library/react` for component testing
3. Mock external dependencies with `vi.mock()`
4. Use `@testing-library/jest-dom` matchers

Example:
```typescript
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

describe('MyComponent', () => {
  it('should render correctly', () => {
    render(<MyComponent />);
    expect(screen.getByText('Hello')).toBeInTheDocument();
  });
});
```

## Manual Testing

### Prerequisites for Testing
- API running at `http://localhost:5000`
- Frontend running at `http://localhost:5173`
- SQL Server database created and migrated
- RabbitMQ running at `localhost:5672`
- Windows Service running

## Backend API Testing

### Using Swagger UI

1. Navigate to `http://localhost:5119/swagger`
2. Explore all available endpoints
3. Use "Try it out" feature to test API calls

### Testing Task CRUD Operations

#### 1. Get All Tags (Setup)
```http
GET /api/tags
```
Expected: List of 10 pre-seeded tags

#### 2. Create a Task
```http
POST /api/tasks
Content-Type: application/json

{
  "title": "Test Task",
  "description": "This is a test task for validation",
  "dueDate": "2025-12-31T23:59:00",
  "priority": 3,
  "fullName": "John Doe",
  "telephone": "+1-555-123-4567",
  "email": "john.doe@example.com",
  "tagIds": [1, 3, 8]
}
```
Expected: 201 Created with task object

#### 3. Get All Tasks
```http
GET /api/tasks
```
Expected: 200 OK with array of tasks

#### 4. Get Task by ID
```http
GET /api/tasks/1
```
Expected: 200 OK with task object or 404 Not Found

#### 5. Update a Task
```http
PUT /api/tasks/1
Content-Type: application/json

{
  "id": 1,
  "title": "Updated Test Task",
  "description": "Updated description",
  "dueDate": "2025-12-31T23:59:00",
  "priority": 4,
  "fullName": "John Doe",
  "telephone": "+1-555-123-4567",
  "email": "john.doe@example.com",
  "tagIds": [1, 2, 3]
}
```
Expected: 200 OK with updated task object

#### 6. Delete a Task
```http
DELETE /api/tasks/1
```
Expected: 204 No Content

### Testing Validation

#### Invalid Email
```http
POST /api/tasks
Content-Type: application/json

{
  "title": "Test",
  "description": "Test",
  "dueDate": "2025-12-31T23:59:00",
  "priority": 3,
  "fullName": "John Doe",
  "telephone": "+1-555-123-4567",
  "email": "invalid-email",
  "tagIds": [1]
}
```
Expected: 400 Bad Request with validation errors

#### Missing Required Fields
```http
POST /api/tasks
Content-Type: application/json

{
  "title": "Test"
}
```
Expected: 400 Bad Request with validation errors for all required fields

#### Invalid Priority
```http
POST /api/tasks
Content-Type: application/json

{
  "title": "Test",
  "description": "Test",
  "dueDate": "2025-12-31T23:59:00",
  "priority": 10,
  "fullName": "John Doe",
  "telephone": "+1-555-123-4567",
  "email": "john@example.com",
  "tagIds": [1]
}
```
Expected: 400 Bad Request (priority must be 1-5)

#### No Tags Selected
```http
POST /api/tasks
Content-Type: application/json

{
  "title": "Test",
  "description": "Test",
  "dueDate": "2025-12-31T23:59:00",
  "priority": 3,
  "fullName": "John Doe",
  "telephone": "+1-555-123-4567",
  "email": "john@example.com",
  "tagIds": []
}
```
Expected: 400 Bad Request (at least one tag required)

## Frontend Testing

### Task List View
1. Open `http://localhost:5173`
2. Verify tasks are displayed
3. Check priority colors:
   - Green: Very Low (1)
   - Blue: Low (2)
   - Orange: Medium (3)
   - Red: High (4)
   - Red: Critical (5)
4. Verify overdue tasks have red due date badge
5. Check tags display as chips
6. Verify user details (name, phone, email) are shown

### Create Task
1. Click "Add Task" button
2. Fill in all fields:
   - Title (required, max 200 chars)
   - Description (required, max 2000 chars)
   - Due Date (required, datetime)
   - Priority (dropdown, 1-5)
   - Tags (multi-select, minimum 1 required)
   - Full Name (required, max 100 chars)
   - Telephone (required, phone format)
   - Email (required, valid email)
3. Try submitting with empty fields → Should show validation errors
4. Try invalid email → Should show validation error
5. Try without tags → Should show validation error
6. Fill valid data and submit → Task should be created and appear in list

### Edit Task
1. Click edit icon on a task
2. Dialog should open with pre-filled data
3. Modify any fields
4. Submit → Task should be updated in list

### Delete Task
1. Click delete icon on a task
2. Confirmation dialog should appear
3. Confirm → Task should be removed from list
4. Cancel → Task should remain

### Error Handling
1. Stop the API server
2. Try creating a task
3. Error alert should appear
4. Start API server
5. Try again → Should work

## Windows Service Testing

### Setup Test Data

First, create a task that is already overdue:

```sql
INSERT INTO TaskItems (Title, Description, DueDate, Priority, FullName, Telephone, Email, CreatedAt)
VALUES ('Overdue Test Task', 'This task is overdue', DATEADD(hour, -1, GETUTCDATE()), 5, 'Test User', '+1-555-999-8888', 'test@example.com', GETUTCDATE());

DECLARE @TaskId INT = SCOPE_IDENTITY();

INSERT INTO TaskItemTags (TaskItemId, TagId)
VALUES (@TaskId, 1), (@TaskId, 2);
```

### Testing Service Functionality

1. Start the Windows Service:
   ```powershell
   cd TaskManagement.Service
   dotnet run
   ```

2. Check console output:
   - Should see "Task Reminder Service is starting"
   - Should see "Connected to RabbitMQ successfully"
   - Should see "Started consuming messages from queue TaskReminders"

3. Wait for task check (5 minutes or modify code to 30 seconds for testing):
   - Should see "Checking for overdue tasks at: [timestamp]"
   - Should see "Published reminder for overdue task: [id] - [title]"
   - Should see "Found and published X overdue task reminders"

4. Check for consumed messages:
   - Should see log entries like:
   ```
   REMINDER: Hi your Task is due - {"TaskId":1,"Title":"Overdue Test Task",...}
   ```

### Testing RabbitMQ Integration

1. Open RabbitMQ Management UI: `http://localhost:15672`
2. Login with guest/guest
3. Go to "Queues" tab
4. Should see "TaskReminders" queue
5. Check message rates:
   - Messages should be published and consumed
6. Click on queue name → View message details

### Testing Concurrent Updates

1. Create multiple overdue tasks
2. Start the service
3. While service is running, update a task via API
4. Check logs - should handle concurrent updates gracefully
5. Verify no duplicate messages in queue

### Error Scenarios

#### RabbitMQ Not Running
1. Stop RabbitMQ service
2. Start Windows Service
3. Should see error: "Failed to connect to RabbitMQ"
4. Service should attempt to reconnect

#### Database Connection Lost
1. Stop SQL Server
2. Wait for next check cycle
3. Should see error: "Error checking overdue tasks"
4. Service should continue running and retry

## Database Testing

### Verify Schema
```sql
-- Check tables exist
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME;

-- Should show: TaskItems, TaskItemTags, Tags, __EFMigrationsHistory
```

### Verify Relationships
```sql
-- Check foreign key constraints
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTableName,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumnName
FROM 
    sys.foreign_keys AS fk
INNER JOIN 
    sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
WHERE 
    OBJECT_NAME(fk.parent_object_id) IN ('TaskItems', 'TaskItemTags', 'Tags')
ORDER BY 
    TableName, ForeignKeyName;
```

### Test SQL Query (Tasks with Multiple Tags)
```sql
-- Query from README
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

Expected: Tasks with 2+ tags, sorted by tag count descending

## Integration Testing

### Full Flow Test

1. **Create Task via Frontend**
   - Open frontend
   - Click "Add Task"
   - Fill all fields with future due date
   - Submit
   - Verify task appears in list

2. **Verify in Database**
   ```sql
   SELECT TOP 1 * FROM TaskItems ORDER BY Id DESC;
   SELECT * FROM TaskItemTags WHERE TaskItemId = (SELECT TOP 1 Id FROM TaskItems ORDER BY Id DESC);
   ```

3. **Update Task to Overdue**
   ```sql
   UPDATE TaskItems 
   SET DueDate = DATEADD(hour, -1, GETUTCDATE())
   WHERE Id = (SELECT TOP 1 Id FROM TaskItems ORDER BY Id DESC);
   ```

4. **Wait for Service**
   - Wait up to 5 minutes
   - Check service logs for reminder
   - Check RabbitMQ management UI for message

5. **Update Task via API**
   - Use Swagger to update the task
   - Change title and priority
   - Verify update succeeds

6. **Verify in Frontend**
   - Refresh frontend
   - Task should show as overdue (red badge)
   - Updated fields should reflect

7. **Delete Task**
   - Click delete icon
   - Confirm deletion
   - Task should disappear

8. **Verify in Database**
   ```sql
   SELECT * FROM TaskItems WHERE Id = [your_task_id];
   -- Should return no rows
   ```

## Performance Testing

### Load Test - Create Multiple Tasks
```powershell
# PowerShell script to create 100 tasks
$baseUrl = "http://localhost:5000/api"
1..100 | ForEach-Object {
    $body = @{
        title = "Load Test Task $_"
        description = "Performance testing task number $_"
        dueDate = "2025-12-31T23:59:00"
        priority = Get-Random -Minimum 1 -Maximum 6
        fullName = "Test User $_"
        telephone = "+1-555-000-$($_.ToString('0000'))"
        email = "user$_@test.com"
        tagIds = @(1, 2)
    } | ConvertTo-Json
    
    Invoke-RestMethod -Uri "$baseUrl/tasks" -Method Post -Body $body -ContentType "application/json"
    Write-Host "Created task $_"
}
```

### Verify Performance
```sql
-- Check query performance
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

SELECT * FROM TaskItems;
SELECT * FROM TaskItemTags;

-- Check indexes
EXEC sp_helpindex 'TaskItems';
EXEC sp_helpindex 'Tags';
EXEC sp_helpindex 'TaskItemTags';
```

## Test Checklist

### Backend ✓
- [ ] All CRUD operations work for Tasks
- [ ] All CRUD operations work for Tags
- [ ] Validation prevents invalid data
- [ ] Error handling returns appropriate status codes
- [ ] CORS allows frontend requests
- [ ] Swagger documentation is accurate

### Frontend ✓
- [ ] Task list displays correctly
- [ ] Create dialog works with validation
- [ ] Edit dialog pre-fills data correctly
- [ ] Delete confirmation works
- [ ] Error messages display properly
- [ ] MobX state updates correctly
- [ ] UI is responsive

### Windows Service ✓
- [ ] Service starts without errors
- [ ] Connects to RabbitMQ successfully
- [ ] Detects overdue tasks
- [ ] Publishes messages to queue
- [ ] Consumes messages from queue
- [ ] Logs reminder messages
- [ ] Handles errors gracefully

### Database ✓
- [ ] Migrations create correct schema
- [ ] Relationships are properly configured
- [ ] Seed data is inserted
- [ ] Cascading deletes work
- [ ] Unique constraints are enforced
- [ ] SQL query returns correct results

### Integration ✓
- [ ] Frontend → API → Database flow works
- [ ] Overdue task detection works
- [ ] RabbitMQ message flow works
- [ ] Concurrent updates handled safely
- [ ] System recovers from component failures

## Reporting Issues

When reporting bugs, please include:
1. Steps to reproduce
2. Expected behavior
3. Actual behavior
4. Error messages (console, logs)
5. Environment (OS, .NET version, Node version)
6. Screenshots if applicable

## Test Data Cleanup

To reset the database for fresh testing:

```sql
-- Delete all tasks and related data
DELETE FROM TaskItemTags;
DELETE FROM TaskItems;

-- Reset identity seeds
DBCC CHECKIDENT ('TaskItems', RESEED, 0);

-- Keep the seeded tags for testing
```

Or drop and recreate the database:

```powershell
cd TaskManagement.API
dotnet ef database drop --force
dotnet ef database update
```
