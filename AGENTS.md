# Project's details

CarRental is a .NET 8 backend built with Clean Architecture

### **Main projects:**

- src/CarRental.Domain — entities, enums, value objects, domain exceptions
- src/CarRental.Application — use-case interfaces, repository abstractions, DTOs and application services
- src/CarRental.Infrastructure — EF Core, PostgreSQL, repository implementations and infrastructure services
- src/CarRental.Api — ASP.NET Core controllers, middleware and HTTP-specific code.
- tests/CarRental.UnitTests — unit tests
- tests/CarRental.IntegrationTests — API/integration tests

Use the existing project structure and nearby code as the source of truth, except for error handling: some existing controllers still use try/catch — that is legacy, follow rule 6 below instead of copying it

# Architecture rules

## 1. Keep dependency direction

### **Allowed project dependencies:**

_Domain <- Application <- Infrastructure <- Api_

More precisely:
- CarRental.Domain must not reference Application, Infrastructure or Api

- CarRental.Application may reference Domain, but must not reference Infrastructure or Api

- CarRental.Infrastructure may reference Application and Domain

- CarRental.Api may reference Application and Infrastructure

Do not introduce reverse dependencies to make a feature easier

## 2. Keep controllers thin

Controllers in _src/CarRental.Api/Controllers_ must work through Application abstractions such as _ICarCatalogService_, _IRentalRequestService_ and _IUserManagementService_

### **Do not:**
- inject ApplicationDbContext into a controller

- inject concrete repository implementations into a controller

- query EF Core directly from a controller

- put business rules in a controller

Controllers should parse HTTP input, call an Application service and return the HTTP result (errors are handled by ExceptionHandlingMiddleware, see rule 6).

## 3. Do not expose Domain entities from API endpoints
API request/response types should be DTOs from CarRental.Application.DTOs or simple HTTP response objects

Do not return CarRental.Domain.Entities.* directly from a controller

When a new endpoint needs data that is not represented yet, add or extend a DTO instead of returning an entity

## 4. Follow the async convention in Application and repositories

New asynchronous methods in Application service interfaces, Application service implementations and repository abstractions must:

- return Task or Task< T>

- end with the Async suffix

- accept CancellationToken cancellationToken = default

- pass the token to downstream async calls

#### Example:

```
Task<CarDto?> GetCarByIdAsync(Guid id, CancellationToken cancellationToken = default);
```

Controller action names do not need the Async suffix because existing controllers use names such as GetCars and CreateUser

## 5. Persist changes through repositories and IUnitOfWork

Application services must use repository abstractions from 
CarRental.Application.Abstractions.Repositories

After a write operation, persist changes through:

await _uow.SaveChangesAsync(cancellationToken);

**Do not access EF Core or ApplicationDbContext directly from an Application service**

## 6. Error handling in new code

- Do not add try/catch blocks to controllers. Controller actions call the Application service and return the success result only; all exceptions are converted to HTTP responses by ExceptionHandlingMiddleware. (Existing controllers still contain try/catch — this is legacy, do not copy it.)

- For a business-rule violation, create a specific exception in src/CarRental.Domain/Exceptions that inherits DomainException (e.g. `public class XxxException : DomainException`).

- Do not throw InvalidOperationException, ArgumentException or Exception for business rules in new code.

- Map every new exception type to an HTTP status code in src/CarRental.Api/Middleware/ExceptionHandlingMiddleware.cs.

- "Not found" is the only exception: throw KeyNotFoundException (already mapped to 404).

## 7. Add tests in the matching test project

When behavior in an Application service or Domain object is added or changed, add unit tests under tests/CarRental.UnitTests

When an API endpoint is added or changed, add an integration test under tests/CarRental.IntegrationTests

**Do not put API integration tests into the unit-test project**

## 8. Document every new endpoint

When you add an HTTP endpoint, add a row for it (method, route, roles) to the endpoints table in README.md.

# Commands for testing

**Build and run all tests:**

```
dotnet test
```

**Run only unit tests:**

```
dotnet test tests/CarRental.UnitTests
```

**Run integration tests:**

```
dotnet test tests/CarRental.IntegrationTests
```

Integration tests require Docker

# Before completion

- Check that layer dependencies still follow the rules above

- Check that controllers contain no EF Core or repository implementation access

- Check new Application/repository async methods for Async and CancellationToken

- Check write operations call SaveChangesAsync

- Check controllers contain no try/catch and new business errors use DomainException subclasses mapped in ExceptionHandlingMiddleware

- Check every new endpoint is in the README endpoints table

- Add/update the appropriate tests

- Run dotnet test
