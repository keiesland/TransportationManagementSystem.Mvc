# TransportationManagementSystem.Mvc

A clean architecture refactor of a production payroll summarization application, demonstrating modern .NET development 
patterns and comprehensive testing practices.

## Overview

TransportationManagementSystem is a payroll data processing and summarization tool built with ASP.NET Core. The project showcases a complete 
refactor of a production application with the goal of implementing clean architecture principles, dependency injection, 
async/await patterns throughout, and comprehensive unit testing.

**Key Achievement:** Identified and fixed 4 real pre-existing bugs from the production codebase during the refactor process, 
all verified against production output.

## Tech Stack

- **Framework:** ASP.NET Core 10
- **ORM:** Entity Framework Core with InMemory provider for testing
- **Testing:** xUnit, Moq
- **Architecture:** Clean Architecture with Service/Repository pattern
- **Patterns:** Generic Repository, Unit of Work, Dependency Injection
- **Code Coverage:** 90%+

## Architecture Highlights

### Clean Separation of Concerns
- **Service Layer:** Business logic and domain operations
- **Repository Layer:** Data access abstraction with generic repository pattern
- **Unit of Work Pattern:** Coordinates multiple repositories and manages transactions
- **Dependency Injection:** All dependencies resolved at composition root

### Key Design Decisions

**1. Generic Repository Pattern**
```csharp
public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}
```
- Reduces code duplication across entity-specific repositories
- Maintains consistency in data access patterns
- Testable through mocking

**2. IBulkOperationsProvider Interface**
Wrapped `EFCore.BulkExtensions` behind an interface to enable:
- Dependency injection and loose coupling
- Easy mocking in unit tests
- Testable bulk operations without a real database

**3. Async/Await Throughout**
- All database operations are truly asynchronous
- Service methods are async by default
- Prevents blocking threads and improves scalability

**4. EF Core InMemory for Testing**
- Unit tests use InMemory provider instead of mocking DbContext
- Tests actual EF Core query behavior
- More realistic testing without mocking infrastructure

### Bug Fixes Discovered

During the refactor, 4 pre-existing bugs in the production code were identified and fixed:
1. **N+1 Query Problem:** Resolved lazy-loading inefficiency with explicit includes
2. **Negative TimeSpan Calculation:** Fixed payroll calculation edge case
3. [Additional bugs documented in commit history]

All fixes were verified against production output to ensure correctness.

## Project Structure

```
TransportationManagementSystem/
├── src/
│   └── TransportationManagementSystem.Api/
│       ├── Controllers/
│       ├── Models/
│       ├── Services/           # Business logic
│       ├── Repositories/        # Data access
│       ├── Data/               # EF Core DbContext
│       └── Program.cs          # DI configuration
└── tests/
    └── TransportationManagementSystem.Tests/
        ├── Services/           # Service layer tests
        ├── Repositories/       # Repository tests
        ├── Fixtures/          # Test data & setup
        └── Integration/       # End-to-end tests
```

## Running the Project

### Prerequisites
- .NET 10 SDK
- Visual Studio 2026 or VS Code

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

View detailed coverage report:
```bash
dotnet test /p:CollectCoverage=true
```

### Run the Application
```bash
cd src/TransportationManagementSystem.Api
dotnet run
```

Application runs on `http://localhost:5000`

## Test Data

Test files use generic/anonymized data to demonstrate functionality while protecting sensitive business 
information.

## Testing Approach

### Unit Testing Strategy
- **Service Layer Tests:** Mock repositories to isolate business logic
- **Repository Tests:** Use EF Core InMemory provider for realistic database behavior
- **Integration Tests:** WebApplicationFactory with InMemory database

### Coverage Goals
- Aim for 80%+ coverage on critical paths
- Prioritize testing business logic over trivial getters/setters
- Test edge cases and error handling

### Example Test Pattern
```csharp
[Fact]
public async Task GetSummary_WithValidData_ReturnsSummarizedResult()
{
    // Arrange
    var mockRepository = new Mock<IRepository<PayrollData>>();
    var service = new PayrollService(mockRepository.Object);
    
    // Act
    var result = await service.GetSummaryAsync(testData);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(expected, result.Total);
}
```

## What I Learned

### Architecture & Design Patterns
- How to structure a scalable, maintainable application
- The value of dependency injection for testability and flexibility
- Trade-offs between generic patterns and specific implementations
- When and how to use interfaces for abstraction

### Entity Framework Core
- Proper async/await usage with EF Core
- Query optimization (N+1 detection and prevention)
- Testing strategies with InMemory provider
- Bulk operations and their testability challenges

### Testing Best Practices
- Writing meaningful unit tests (not just coverage)
- Arranging test data efficiently with fixtures
- Testing edge cases discovered from production bugs
- Balancing mock complexity vs. test accuracy

### Production-to-Code Insights
- Identifying performance issues in live applications
- Fixing bugs discovered through careful code review
- Verifying fixes against production behavior

## Future Enhancements

- [ ] Add automated performance benchmarking
- [ ] Implement caching strategy for frequently accessed data
- [ ] Add API versioning
- [ ] Create Swagger/OpenAPI documentation
- [ ] Set up CI/CD pipeline with GitHub Actions

## How to Use This for Learning

This project demonstrates:
1. **Clean Architecture** - How to structure a real application
2. **Testing Excellence** - Comprehensive unit and integration testing
3. **Bug Resolution** - How to find and fix real production issues
4. **Modern .NET Patterns** - Async/await, DI, EF Core best practices

The code is intentionally well-commented and structured to be educational. Use it as a reference for your own projects.

## License

This is a portfolio project. Feel free to use it as reference, but please create your own implementations for production use.

---

**Questions or feedback?** Feel free to open an issue or reach out.

**Last Updated:** 07/03/2026  
**Status:** Complete refactor with 90%+ test coverage
