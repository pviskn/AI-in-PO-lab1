---
name: implement-api-endpoint
description: Use when adding or changing an HTTP endpoint in CarRental
---

# Skill: implement-api-endpoint

Use this when adding or tweaking an HTTP endpoint in CarRental

# Goal
Build the endpoint without breaking the Clean Architecture rules

# Steps

## 1. Check out a similar endpoint
Before coding, open the closest controller and its Application service. Copy their naming, auth attributes, DTOs, and HTTP status codes to keep things consistent.

## 2. Define the API contract
If you need new input or output data:
- add or update a DTO in _src/CarRental.Application/DTOs_

- **never** use Domain entities directly as your controller request/response models

## 3. Update the Application layer
Add the use-case method to the right interface in _src/CarRental.Application/Abstractions/UseCases_
For async methods:
- add the `Async` suffix

- return `Task` or `Task<T>`

- always include `CancellationToken cancellationToken = default`

Example:
```
Task<SomethingDto> GetSomethingAsync(
    Guid id,
    CancellationToken cancellationToken = default);
```

## 4. Write the business logic

Put the logic in the Application service or Domain. Use repository interfaces, not EF Core directly.

If it's a domain rule or state change, put it in the Domain entity instead of cluttering the controller

For writes, always finish with:

```
await _uow.SaveChangesAsync(cancellationToken);
```

## 5. Update repositories (only if needed)
Only touch the repos if you actually need a new data query. If so:

- add it to the interface in _CarRental.Application/Abstractions/Repositories_

- implement it in _CarRental.Infrastructure_

- keep all EF Core stuff strictly inside Infrastructure

Async repo methods need the `Async` suffix and a `CancellationToken`

## 6. Add the controller action

Add the action in _src/CarRental.Api/Controllers_ 
The controller should **only**:

- grab route/query/body data

- check auth if needed

- call the Application service

- return the HTTP result

**Don't inject _ApplicationDbContext_ or concrete repos into the controller**

**No try/catch in the action at all, the middleware handles errors**

## 7. Handle errors

For a business-rule violation, create a specific DomainException subclass in src/CarRental.Domain/Exceptions and map it in ExceptionHandlingMiddleware

Never just `throw new Exception(...)`. Let unexpected errors bubble up to the `ExceptionHandlingMiddleware`

## 8. Add the endpoint to the README endpoints table

Document the new endpoint in the README endpoints table.

## 9. Add tests
- Add unit tests in _tests/CarRental.UnitTests_ for Domain/Application changes

- Add integration tests in _tests/CarRental.IntegrationTests_ for the endpoint

- At least cover the happy path and one major edge case or failure

## 10. Run checks

Run:

```
dotnet test
```

**Double-check:**

- no circular layer dependencies

- no Domain entities returned from the controller

- no direct EF Core access in the controller

- new async methods have _Async_ and _CancellationToken_

- writes use _SaveChangesAsync_

- tests are in the right projects.
