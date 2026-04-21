# SQLite Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate erpnext-dashboard backend from PostgreSQL to SQLite with Google Drive backup/restore

**Architecture:** Replace PostgreSQL with SQLite in EF Core configuration, copy Google Drive services from cms-system, regenerate migrations

**Tech Stack:** .NET 10, EF Core 9.0.11, SQLite, Google Drive API

---

### Task 1: Update Infrastructure.csproj - Change database packages

**Files:**
- Modify: `erpnext-dashboard/backend/Api/Infrastructure/Infrastructure.csproj`

- [ ] **Step 1: Remove PostgreSQL packages**

Find and remove these lines:
```xml
<PackageReference Include="EFCore.NamingConventions" Version="9.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
```

- [ ] **Step 2: Add SQLite package**

Add after the other EF Core packages:
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.11" />
<PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.11" />
```

- [ ] **Step 3: Add Google Drive packages**

Add after the SQLite packages:
```xml
<PackageReference Include="Google.Apis.Drive.v3" Version="1.69.0.3520" />
<PackageReference Include="Google.Apis.Auth" Version="1.69.0" />
```

- [ ] **Step 4: Commit**

```bash
git add Infrastructure/Infrastructure.csproj
git commit -m "refactor: replace PostgreSQL with SQLite packages"
```

---

### Task 2: Copy Google Drive services from cms-system

**Files:**
- Create: `erpnext-dashboard/backend/Api/Infrastructure/Services/IGoogleTokenProvider.cs`
- Create: `erpnext-dashboard/backend/Api/Infrastructure/Services/GoogleTokenProvider.cs`
- Create: `erpnext-dashboard/backend/Api/Infrastructure/Services/GoogleDriveService.cs`
- Create: `erpnext-dashboard/backend/Api/Infrastructure/Services/GoogleDriveAuthService.cs`
- Create: `erpnext-dashboard/backend/Api/Infrastructure/Services/IDatabaseSyncService.cs`
- Create: `erpnext-dashboard/backend/Api/Infrastructure/Services/DatabaseSyncService.cs`
- Create: `erpnext-dashboard/backend/Api/Infrastructure/Services/DatabaseRestoreService.cs`

- [ ] **Step 1: Create Services directory**

```bash
mkdir -p erpnext-dashboard/backend/Api/Infrastructure/Services
```

- [ ] **Step 2: Copy IGoogleTokenProvider.cs**

Copy from `cms-system/backend/Infrastructure/Services/IGoogleTokenProvider.cs`

- [ ] **Step 3: Copy GoogleTokenProvider.cs**

Copy from `cms-system/backend/Infrastructure/Services/GoogleTokenProvider.cs`

- [ ] **Step 4: Copy GoogleDriveService.cs**

Copy from `cms-system/backend/Infrastructure/Services/GoogleDriveService.cs`

In the `FindFileAsync` method, change the application name:
```csharp
// Change from: "CMS" to "ERPNext"
listRequest.Q = $"name = '{fileName}' and '{_folderId}' in parents and trashed = false";
```

- [ ] **Step 5: Copy GoogleDriveAuthService.cs**

Copy from `cms-system/backend/Infrastructure/Services/GoogleDriveAuthService.cs`

- [ ] **Step 6: Copy IDatabaseSyncService.cs**

Copy from `cms-system/backend/Infrastructure/Services/IDatabaseSyncService.cs`

- [ ] **Step 7: Copy DatabaseSyncService.cs**

Copy from `cms-system/backend/Infrastructure/Services/DatabaseSyncService.cs`

In the `SyncDatabaseAsync` method, change the application name:
```csharp
// Change from: "CMS" to "ERPNext"
var driveService = CreateDriveService("ERPNext");
```

- [ ] **Step 8: Copy DatabaseRestoreService.cs**

Copy from `cms-system/backend/Infrastructure/Services/DatabaseRestoreService.cs`

- [ ] **Step 9: Commit**

```bash
git add Infrastructure/Services/
git commit -m "feat: add Google Drive backup/restore services"
```

---

### Task 3: Update DependencyInjection.cs

**Files:**
- Modify: `erpnext-dashboard/backend/Api/Infrastructure/DependancyInjection.cs`

- [ ] **Step 1: Read current file**

Current content uses `UseNpgsql`. Need to change to SQLite and add Google Drive services.

- [ ] **Step 2: Update using statements**

Add after existing usings:
```csharp
using Infrastructure.Services;
```

- [ ] **Step 3: Update AddInfrastructure method**

Replace the entire method body with:

```csharp
public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
{
    var loggerFactory = builder.Services.BuildServiceProvider()
        .GetRequiredService<ILoggerFactory>();
    var tokenProviderLogger = loggerFactory.CreateLogger<GoogleTokenProvider>();
    var contentRoot = builder.Environment.ContentRootPath;
    tokenProvider.LoadFromConfigurationAsync(builder.Configuration, contentRoot).GetAwaiter().GetResult();

    GoogleDriveService? googleDrive = null;

    if (!tokenProvider.HasToken)
    {
        var mainLogger = loggerFactory.CreateLogger("GoogleDriveAuth");
        mainLogger.LogInformation("No Google Drive token found. Starting authentication...");
        var authLogger = loggerFactory.CreateLogger<GoogleDriveAuthService>();
        var authService = new GoogleDriveAuthService(builder.Configuration, tokenProvider, authLogger);
        authService.StartAuthFlowAsync(CancellationToken.None, builder.Configuration, contentRoot).GetAwaiter().GetResult();
    }
    else
    {
        googleDrive = new GoogleDriveService(builder.Configuration, tokenProvider);
        var isValid = googleDrive.ValidateTokenAsync(
            CancellationToken.None,
            TimeSpan.FromSeconds(10)
        ).GetAwaiter().GetResult();

        if (!isValid)
        {
            var mainLogger = loggerFactory.CreateLogger("GoogleDriveAuth");
            mainLogger.LogWarning("Google Drive token invalid or expired. Re-authenticating...");
            var authLogger = loggerFactory.CreateLogger<GoogleDriveAuthService>();
            var authService = new GoogleDriveAuthService(builder.Configuration, tokenProvider, authLogger);
            authService.StartAuthFlowAsync(CancellationToken.None, builder.Configuration, contentRoot).GetAwaiter().GetResult();
        }
        else
        {
            var mainLogger = loggerFactory.CreateLogger("GoogleDriveAuth");
            mainLogger.LogInformation("Google Drive token is valid.");
        }
    }

    googleDrive ??= new GoogleDriveService(builder.Configuration, tokenProvider);

    builder.Services.AddSingleton<IGoogleTokenProvider>(tokenProvider);
    builder.Services.AddSingleton(googleDrive);

    var dbRestoreLogger = builder.Services.BuildServiceProvider()
        .GetRequiredService<ILogger<DatabaseRestoreService>>();
    DatabaseRestoreService.EnsureDatabaseExists(googleDrive, builder.Configuration, dbRestoreLogger);

    builder.Services.AddDbContext<DashboardDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("Sqlite") ??
            "Data Source=erpnext.db";
        options.UseSqlite(connectionString);
    });

    builder.Services.AddSingleton<IDatabaseSyncService, DatabaseSyncService>();
    builder.Services.AddHostedService(sp => (DatabaseSyncService)sp.GetRequiredService<IDatabaseSyncService>());

    builder.Configuration["ContentRootPath"] = builder.Environment.ContentRootPath;

    return builder;
}
```

- [ ] **Step 4: Update MapInfrastructure method**

Keep existing migration logic (works the same for SQLite):
```csharp
public static WebApplication MapInfrastructure(this WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
    var canConnect = db.Database.CanConnect();
    app.Logger.LogInformation("Can connect to database: {CanConnect}", canConnect);

    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while applying database migrations.");
        throw;
    }

    return app;
}
```

- [ ] **Step 5: Commit**

```bash
git add Infrastructure/DependancyInjection.cs
git commit -m "refactor: migrate from PostgreSQL to SQLite with Google Drive"
```

---

### Task 4: Delete existing migrations and create new SQLite migrations

**Files:**
- Delete: `erpnext-dashboard/backend/Api/Infrastructure/Migrations/*.cs`
- Create: New migration files

- [ ] **Step 1: Delete existing migrations**

```bash
rm -rf erpnext-dashboard/backend/Api/Infrastructure/Migrations/
```

- [ ] **Step 2: Create new migration**

Run from Host directory:
```bash
cd erpnext-dashboard/backend/Api/Host
dotnet ef migrations add InitialCreate --output-dir ../Infrastructure/Migrations
```

- [ ] **Step 3: Verify migration was created**

List files in `Infrastructure/Migrations/` - should have InitialCreate.cs and Designer.cs

- [ ] **Step 4: Commit**

```bash
git add Infrastructure/Migrations/
git commit -m "feat: add SQLite migrations"
```

---

### Task 5: Update configuration files

**Files:**
- Modify: `erpnext-dashboard/backend/Api/Host/appsettings.json` (or create appsettings.Development.json)

- [ ] **Step 1: Update connection string**

Change from:
```json
"ConnectionStrings": {
  "Postgres": "Host=localhost;Database=erpnext;Username=postgres;Password=password"
}
```

To:
```json
"ConnectionStrings": {
  "Sqlite": "Data Source=erpnext.db"
}
```

- [ ] **Step 2: Add Google Drive configuration**

Add:
```json
"GoogleDrive": {
  "FolderId": "your-folder-id",
  "ClientID": "your-client-id",
  "ClientSecret": "your-client-secret",
  "SyncIntervalMinutes": 15,
  "RestoreMaxRetries": 3
}
```

- [ ] **Step 3: Commit**

```bash
git add Host/appsettings.json
git commit -m "config: add SQLite and Google Drive configuration"
```

---

### Task 6: Build and verify

**Files:**
- Verify: Build solution

- [ ] **Step 1: Restore and build**

```bash
cd erpnext-dashboard/backend/Api
dotnet restore
dotnet build
```

- [ ] **Step 2: Fix any compilation errors**

If there are issues with namespace or missing references, fix them.

- [ ] **Step 3: Commit**

```bash
git add .
git commit -m "fix: resolve build issues"
```

---

### Task 7: Test the migration

- [ ] **Step 1: Run the application**

```bash
cd erpnext-dashboard/backend/Api/Host
dotnet run
```

- [ ] **Step 2: Verify startup behavior**

- On first run without database: should attempt restore from Google Drive
- If no backup exists: creates new database
- Migrations should apply automatically

- [ ] **Step 3: Verify CRUD operations work**

Test creating/getting/updating sites, users, companies

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "test: verify SQLite migration works"
```

---

## Plan Complete

**Execution choice:**

1. **Subagent-Driven (recommended)** - I dispatch a fresh subagent per task, review between tasks, fast iteration

2. **Inline Execution** - Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?