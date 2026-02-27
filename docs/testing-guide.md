# Testing Guide

## Goal
This project uses `xUnit` for unit tests in `RenameMusic.Tests`.

The current objective is to validate core behavior without depending on WPF runtime.

## First tests included
- `TemplateRuleServiceTests`
- `FilenameFunctionsTests`

These are intentionally simple and can be used as templates for adding more tests.

## Run tests
From repository root:

```bash
dotnet test RenameMusic.Tests/RenameMusic.Tests.csproj -c Release
```

## Add a new test
1. Create a new file in `RenameMusic.Tests`.
2. Create a class ending with `Tests`.
3. Add methods with `[Fact]` for single scenario tests.
4. Use clear names like `Method_ShouldDoX_WhenY`.

## Recommended next tests
1. `SessionService` ingestion duplicate handling
2. `RenameExecutionService` conflict policy behavior
3. Missing-file behavior in session snapshots
