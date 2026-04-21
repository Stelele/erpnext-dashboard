# SQLite Migration Design - erpnext-dashboard

## Overview

Migrate erpnext-dashboard backend from PostgreSQL to SQLite, with Google Drive backup/restore functionality matching the cms-system pattern.

## Current State

- **Database**: PostgreSQL with EF Core 9.0.11
- **Naming Convention**: Snake case (via EFCore.NamingConventions)
- **Entities**: Site, User, Company
- **Migrations**: 4 existing migrations in `Infrastructure/Migrations/`
- **Package References**: Npgsql.EntityFrameworkCore.PostgreSQL, EFCore.NamingConventions

## Target State

- **Database**: SQLite with EF Core
- **Naming Convention**: Default (PascalCase or as configured in entity configs)
- **Backup**: Google Drive (via DatabaseSyncService - periodic sync)
- **Restore**: Google Drive (via DatabaseRestoreService - on startup)
- **Package References**: Microsoft.EntityFrameworkCore.Sqlite

## Architecture

### Data Flow

```
Startup
  ├─> DatabaseRestoreService.EnsureDatabaseExists()
  │       └─> Check if database file exists locally
  │           ├─> Exists: continue (no restore needed)
  │           └─> Not exists: restore from Google Drive
  │
  └─> DatabaseSyncService (Background Service)
          └─> Periodic sync to Google Drive (configurable interval)
```

### New/Modified Files

#### Infrastructure.csproj
- Remove: `Npgsql.EntityFrameworkCore.PostgreSQL` (9.0.4)
- Remove: `EFCore.NamingConventions` (9.0.0)
- Add: `Microsoft.EntityFrameworkCore.Sqlite` (matching version)

#### Infrastructure/DependencyInjection.cs
- Change `UseNpgsql` to `UseSqlite`
- Add Google Drive service registration
- Add DatabaseRestoreService and DatabaseSyncService

#### New Services (from cms-system)
- `Infrastructure/Services/GoogleDriveService.cs`
- `Infrastructure/Services/GoogleDriveAuthService.cs`
- `Infrastructure/Services/GoogleTokenProvider.cs`
- `Infrastructure/Services/IGoogleTokenProvider.cs`
- `Infrastructure/Services/DatabaseRestoreService.cs`
- `Infrastructure/Services/IDatabaseSyncService.cs`
- `Infrastructure/Services/DatabaseSyncService.cs`

#### Configuration
- Change `ConnectionStrings:Postgres` to `ConnectionStrings:Sqlite`
- Add `GoogleDrive:FolderId`
- Add `GoogleDrive:ClientID`
- Add `GoogleDrive:ClientSecret`
- Add `GoogleDrive:SyncIntervalMinutes` (default: 15)
- Add `GoogleDrive:RestoreMaxRetries` (default: 3)

#### Migrations
- Delete all existing migrations in `Infrastructure/Migrations/`
- Regenerate migrations with SQLite provider

## Configuration Reference

### appsettings.json (example)

```json
{
  "ConnectionStrings": {
    "Sqlite": "Data Source=erpnext.db"
  },
  "GoogleDrive": {
    "FolderId": "your-folder-id",
    "ClientID": "your-client-id",
    "ClientSecret": "your-client-secret",
    "SyncIntervalMinutes": 15,
    "RestoreMaxRetries": 3
  }
}
```

## Database Files Location

- Database file stored in content root (`IHostEnvironment.ContentRootPath`)
- Backup file name: `erpnext.db` (configurable via connection string)
- Stored in Google Drive folder specified by `FolderId`

## Migration Strategy

1. Start fresh - no data migration needed (user will do manual migration)
2. Delete existing PostgreSQL migrations
3. Create new SQLite migrations
4. Apply migrations on startup

## Error Handling

- **Restore failure**: Log error, continue with new empty database
- **Sync failure**: Log error, continue (will retry on next interval)
- **Missing Google Drive config**: Service should fail fast with clear error message

## Testing Checklist

- [ ] Database creates successfully on fresh start
- [ ] Migrations apply correctly
- [ ] CRUD operations work with SQLite
- [ ] Backup syncs to Google Drive
- [ ] Restore works when database doesn't exist locally
- [ ] Configuration loaded correctly from appsettings.json