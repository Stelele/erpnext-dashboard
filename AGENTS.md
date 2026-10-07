# Agents.md

## Shared conventions

Cross-project decisions, code style, stack choices, testing expectations and workflow live
in the hivemind board. **Read `core/INDEX.md` before coding**, then only the 2-3 core files
it points at for the work in hand.

The board is a private repo. If it is not checked out on this machine yet:

    mkdir -p ~/Documents/code-projects
    git clone git@github.com:Stelele/hivemind.git ~/Documents/code-projects/hivemind

Then either of these updates it — adjust the path if you cloned it elsewhere:

    ~/Documents/code-projects/hivemind/hm pull        # the board CLI
    git -C ~/Documents/code-projects/hivemind pull    # plain git

Board: https://github.com/Stelele/hivemind — every rule has an ADR recording why and what
it costs. Everything below, and anything else here, is specific to this project.

Three that get violated most: **never commit without being asked** (ADR 0001) · **make the
whole change, then run tests once** (ADR 0002) · **check the real docs before
implementing** (ADR 0011).

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