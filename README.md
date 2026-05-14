# Victory CIS Management System

Initial backend scaffold for the regulated Collective Investment Scheme Management System for Victory Financial Services Ltd.

## Stack

- ASP.NET Core Web API on .NET 8
- PostgreSQL with Entity Framework Core
- Clean Architecture modular monolith
- xUnit, FluentAssertions, and Testcontainers for tests
- Swagger/OpenAPI, Serilog, ProblemDetails, and health checks

## Repository Layout

```text
src/
  Cis.Api/
  Cis.Application/
  Cis.Contracts/
  Cis.Domain/
  Cis.Infrastructure/
tests/
  Cis.Tests.Unit/
  Cis.Tests.Integration/
docs/
  srs-traceability.md
```

## Run with Docker Compose

```powershell
docker compose up --build
```

The API listens on:

- Health: http://localhost:8080/health
- Swagger: http://localhost:8080/swagger
- PostgreSQL from host tools: `localhost:55432`

## Build

With a local .NET SDK:

```powershell
dotnet build .\Cis.ManagementSystem.sln
```

Without a local .NET SDK:

```powershell
docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet build Cis.ManagementSystem.sln
```

## Tests

```powershell
dotnet test .\Cis.ManagementSystem.sln
```

Integration tests use Testcontainers and require Docker.

## Migrations

Install the EF Core CLI if needed:

```powershell
dotnet tool install --global dotnet-ef
```

Create the first migration:

```powershell
dotnet ef migrations add InitialCreate --project .\src\Cis.Infrastructure --startup-project .\src\Cis.Api --output-dir Persistence\Migrations
```

Apply migrations:

```powershell
$env:ConnectionStrings__CisDb="Host=localhost;Port=55432;Database=cis;Username=cis;Password=cis_password"
dotnet ef database update --project .\src\Cis.Infrastructure --startup-project .\src\Cis.Api
```

The design-time DbContext factory uses `ConnectionStrings__CisDb` when present, then falls back to `localhost:5432`.

## Domain Rules Already Enforced

- Financial primitives use `decimal`, never `double` or `float`.
- `BusinessDate` is separate from UTC timestamps.
- `RowVersion` is configured as an optimistic concurrency token.
- Audit logs are immutable append-only records.
- API errors use RFC 7807 ProblemDetails.
