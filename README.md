# Student and Branch Management API

A REST API built with ASP.NET Core 8, Entity Framework Core,
and SQLite.

## Features

- Student and branch CRUD operations
- Student branch assignment and reassignment
- Duplicate branch-name prevention
- Prevention of deleting branches with assigned students
- Student search, filtering, sorting, and pagination
- Request validation and cancellation-token forwarding
- Centralized exception handling and structured logging
- Unit and integration tests

## Requirements

- .NET 8 SDK
- An editor such as Visual Studio 2022

SQLite does not require a separate database server.

## Setup

From the solution folder:

```powershell
dotnet restore
dotnet tool restore
```

From the folder containing WebApplication1.csproj:

```powershell
dotnet tool run dotnet-ef -- database update
dotnet run
```

Open `/swagger` on the address printed by the application
when running in the Development environment.

## Endpoints

| Method | Route | Purpose | Main responses |
|---|---|---|---|
| GET | /api/students | Search and list students | 200, 400 |
| GET | /api/students/{id} | Get a student | 200, 404 |
| POST | /api/students | Create a student | 201, 400 |
| PUT | /api/students/{id} | Update a student | 204, 400, 404 |
| DELETE | /api/students/{id} | Delete a student | 204, 404 |
| GET | /api/branches | List branches | 200 |
| GET | /api/branches/{id} | Get a branch | 200, 404 |
| POST | /api/branches | Create a branch | 201, 400, 409 |
| PUT | /api/branches/{id} | Rename a branch | 204, 400, 404, 409 |
| DELETE | /api/branches/{id} | Delete an unused branch | 204, 404, 409 |

### Create a student

POST `/api/students`

```json
{
  "name": "Ab",
  "marks": 98,
  "branchId": 1
}
```

The branch must already exist. BranchId is optional.
The database generates the student ID.

### Create or rename a branch

POST `/api/branches` or PUT `/api/branches/{id}`

```json
{
  "name": "Computer Science"
}
```

### Student query parameters

| Parameter | Purpose | Default |
|---|---|---|
| search | Match part of the student's name | No filter |
| minMarks | Minimum marks, inclusive, from 0 100 | No filter |
| branchId | Filter by a positive branch ID | No filter |
| page | Page number, starting at 1 | 1 |
| pageSize | Results per page, from 1 100 | 5 |
| sortBy | id, name, or marks | id |
| ascending | Sort direction | true |

Example:

```text
/api/students?search=ab&minMarks=40&page=1&pageSize=5&sortBy=marks&ascending=false
```

Paginated responses include the current page's items,
the total matching count, and pagination metadata.

### Branch rules

Branch names are trimmed before saving. ASCII case differences
do not allow duplicate names.

A branch with assigned students cannot be deleted.
Reassign those students or explicitly remove their branch
assignments first.

For student PUT requests, an omitted or null BranchId removes
the existing branch assignment.

## Tests

From the solution folder:

```powershell
dotnet test
```

Unit tests use fake services to verify controller behavior.

Integration tests send HTTP requests through the application
pipeline and use a temporary SQLite database in memory.
They do not use the application's students.db file.

The suite currently contains 39 passing test cases.

## Error handling

Validation errors return 400. Missing resources return 404.
Duplicate branch names and protected branch deletions return 409.
## Health checks

| Endpoint | Purpose |
|---|---|
| GET /health/live | Reports whether the API can respond |
| GET /health/ready | Checks database connectivity |

Healthy responses return HTTP 200.
An unhealthy database check makes readiness return HTTP 503,
while liveness can remain healthy.

These endpoints allow anonymous access and return a simple
status without exposing internal failure details.

## Continuous integration

GitHub Actions restores packages, builds the solution in Release
mode, and runs the automated tests on pushes and pull requests.

## Local deployment rehearsal

Publish the API from the repository root:

```powershell
dotnet publish WebApplication1/WebApplication1.csproj --configuration Release --output artifacts/publish
```

Create a separate deployment database:

```powershell
cd WebApplication1
dotnet tool restore
dotnet tool run dotnet-ef -- database update --connection "Data Source=../artifacts/deployment.db"
cd ..
```

Run the published API using PowerShell:

```powershell
cd artifacts/publish
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://localhost:5080"
$env:ConnectionStrings__StudentDb = "Data Source=../deployment.db"
dotnet WebApplication1.dll
```

Keep this terminal open. In another terminal, verify:

```powershell
curl.exe -i http://localhost:5080/health/live
curl.exe -i http://localhost:5080/health/ready
curl.exe -i http://localhost:5080/api/students
```

All three endpoints should return HTTP 200.
Both health endpoints should return `Healthy`.

Swagger and the development Admin bootstrapper are disabled
in Production. This rehearsal uses local HTTP; hosted deployment
requires HTTPS configuration.

Stop the API with Ctrl+C and close its terminal to discard
the session's environment-variable overrides.

Unexpected exceptions return a generic 500 ProblemDetails
response with a trace ID. Detailed exceptions are recorded
in application logs.
## Hosted demonstration

Base URL: https://abhinav-chaudhari.runasp.net

This practice API is hosted on MonsterASP.NET using Windows/IIS
with HTTPS. It uses SQLite for database storage.

### Public endpoints

- GET /health/live — application liveness
- GET /health/ready — database connectivity
- GET /api/students — paginated student list

Student and branch write operations require an authenticated Admin.

### Authentication

POST /api/auth/login?useCookies=false accepts an email and password
as JSON. Use the returned accessToken as a Bearer token.

The application uses ASP.NET Core Identity's built-in bearer tokens,
not JWTs. Public registration does not assign the Admin role.

### Deployment storage

- /wwwroot — published application
- /private/data — SQLite database
- /private/keys — Data Protection keys

Preserve the private folders during application deployments.
Never commit database files, authentication keys, passwords,
access tokens, or deployment credentials.

### Verification

The hosted deployment was checked for:

- Successful HTTPS health responses.
- Admin login and authorized student creation.
- Student data surviving an application-pool restart.
- An existing unexpired access token working after restart.

CI runs 53 automated tests, verifies the published application
through HTTP smoke checks, and uploads the tested publish output.

### Limitations

This is a practice deployment using a free hosting plan.
Docker remains postponed. Load testing and a tested backup-and-restore
procedure remain future improvements.