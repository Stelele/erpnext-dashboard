# Agents.md

Guidelines for agents working with this codebase.

## Project Overview

- **Name**: Njeremoto Dashboard
- **Type**: Full-stack business intelligence webapp
- **Stack**: Vue 3 + .NET 10 + PostgreSQL

## Conventions

### Code Style

- Use TypeScript strict mode on frontend
- C# with nullable reference types enabled
- ESLint and C# analyzers enforce style

### Build & Test

**Frontend**:
```bash
npm run build     # Build for production
npm run dev       # Development server
```

**Backend**:
```bash
dotnet build      # Build solution
dotnet test      # Run tests
```

### Database

- PostgreSQL with EF Core migrations
- User secrets for local connection strings
- Scripts in `erpnext/optimize/` for ERPNext integration

## Common Tasks

1. **Add new API endpoint**: Create in `backend/Api/Endpoints/`
2. **Add new frontend page**: Add route in `frontend/src/router/`, create component in `frontend/src/components/`
3. **Run migrations**: `dotnet ef database update` in `backend/Api/Host/`

## Important Files

- `frontend/package.json` - Frontend dependencies
- `backend/Api/Host/Host.csproj` - Backend project file
- `.env` - Environment template (do not commit secrets)