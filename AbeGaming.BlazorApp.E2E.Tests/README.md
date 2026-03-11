# AbeGaming Playwright E2E Tests

This project contains browser-level UI regression tests using Playwright.

## Prerequisites

1. Install Playwright browsers (first run only):

```powershell
dotnet build AbeGaming.BlazorApp.E2E.Tests/AbeGaming.BlazorApp.E2E.Tests.csproj
pwsh AbeGaming.BlazorApp.E2E.Tests/bin/Debug/net10.0/playwright.ps1 install
```

2. Start the Blazor app locally (default expected URL is `http://localhost:5211`):

```powershell
dotnet run --project AbeGamingBlazorApp/AbeGamingBlazorApp.csproj --urls http://localhost:5211
```

## Run tests

```powershell
dotnet test AbeGaming.BlazorApp.E2E.Tests/AbeGaming.BlazorApp.E2E.Tests.csproj
```

Run only PoG browser tests:

```powershell
dotnet test AbeGaming.BlazorApp.E2E.Tests/AbeGaming.BlazorApp.E2E.Tests.csproj --filter "FullyQualifiedName~PoGBattleE2ETests"
```

Optional: point tests to a different environment:

```powershell
$env:E2E_BASE_URL = "https://abegaming.org"
dotnet test AbeGaming.BlazorApp.E2E.Tests/AbeGaming.BlazorApp.E2E.Tests.csproj
```
