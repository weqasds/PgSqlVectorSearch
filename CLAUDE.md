# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a RAG (Retrieval-Augmented Generation) system using PostgreSQL vector search with DDD (Domain-Driven Design) architecture. The system includes knowledge base management, document processing, vector retrieval, and intelligent conversation capabilities.

**Technology Stack**:
- **Backend**: .NET 10.0, ASP.NET Core, Semantic Kernel
- **Database**: PostgreSQL with pgvector extension
- **Frontend**: Vue.js 3, TypeScript, Vite, Tailwind CSS
- **ORM**: Entity Framework Core

## Quick Start

1. **Database**: Install PostgreSQL and enable pgvector extension.
2. **Connection**: Update the hardcoded connection string in `DataMigration/RagDbContext.cs` or move to `appsettings.json`.
3. **Migrations**: Run `dotnet ef database update --project DataMigration --startup-project PgSqlVectorSearch`.
4. **Backend**: `dotnet run --project PgSqlVectorSearch`.
5. **Frontend**: `cd fronted && pnpm install && pnpm dev`.

## Development Commands

### Frontend (fronted/)
```bash
cd fronted
pnpm install          # Install dependencies
pnpm dev             # Start development server
pnpm build           # Build for production
pnpm type-check      # TypeScript type checking
pnpm lint            # Run linters (oxlint + ESLint)
pnpm format          # Format code with Prettier
```

### Backend (.NET)
```bash
# Restore packages and build
dotnet restore
dotnet build

# Run the main API (from root or PgSqlVectorSearch directory)
dotnet run --project PgSqlVectorSearch

# Database migrations (from DataMigration directory)
dotnet ef migrations add <name> --project DataMigration --startup-project PgSqlVectorSearch
dotnet ef database update --project DataMigration --startup-project PgSqlVectorSearch
```

### Running with Docker
The main API project includes Docker support (Dockerfile). Use standard Docker commands:
```bash
docker build -t pgsqlvectorsearch -f PgSqlVectorSearch/Dockerfile .
```

## Architecture

### DDD Layer Structure
The solution follows strict dependency direction:

1. **DomainService** (Domain Layer)
   - Contains core domain entities (`Entities/`) and business logic
   - No external dependencies, defines repository interfaces
   - Referenced by Application and DataMigration

2. **Application** (Application Layer)
   - Contains application services (`ApplicationService/`) and DTOs
   - Coordinates use cases, orchestrates domain services
   - References DomainService only

3. **DataMigration** (Infrastructure Layer)
   - Contains EF Core DbContext (`RagDbContext`), migrations, and repository implementations
   - References DomainService for entity definitions
   - Configured with PostgreSQL and pgvector

4. **PgSqlVectorSearch** (Presentation Layer)
   - ASP.NET Core Web API with controllers
   - References Application layer
   - Serves frontend assets and API endpoints

### Core Domain Entities
- `KnowledgeBase`, `KnowledgeDocument`, `KnowledgeChunk` - Knowledge management
- `Embedding` - Vector representations (1536-dimensional)
- `User`, `ChatSession`, `ChatMessage` - Conversation management
- `RagGeneration`, `RagRetrievalLog` - RAG processing tracking

### Database Configuration
- PostgreSQL connection string is currently hardcoded in `RagDbContext.OnConfiguring`
- Uses pgvector extension for vector similarity search
- Vector columns defined as `vector(1536)`
- Enum `ChatRole` mapped to PostgreSQL enum type

### Service Implementation Status
**Note**: The architecture design (`RAG项目DDD架构设计文档.md`) defines comprehensive service interfaces, but most are not yet implemented. Current implementation includes:
- ✅ Domain entities defined
- ✅ Database schema and migrations
- ✅ Basic DbContext configuration
- ⏳ Domain services (to be implemented in `DomainService/Services/`)
- ⏳ Application services (to be implemented in `Application/ApplicationService/`)
- ⏳ API controllers (partially implemented in `PgSqlVectorSearch/Controllers/`)

## Key Implementation Patterns

### Dependency Injection
Services should be registered in `Program.cs` following the pattern in the architecture document (section 6.2). Current `Program.cs` is minimal and needs expansion.

### Database Access
- Use `RagDbContext` from DataMigration project
- Repository pattern interfaces are defined in DomainService (to be implemented)
- Always create new migrations when modifying domain entities

### Vector Operations
- `Embedding` entity has a `Vector` property of type `vector(1536)`
- Use pgvector's similarity functions via EF Core extensions
- Semantic Kernel integration planned for embedding generation

## Frontend Structure
- Vue 3 Composition API with TypeScript
- Pinia for state management
- Vue Router for navigation
- Tailwind CSS + DaisyUI for styling
- Axios for API calls
- API clients in `src/api/` (auth, chat) - needs backend integration

## Development Notes

1. **Database Setup**: Ensure PostgreSQL with pgvector extension is running. Update connection string in `RagDbContext` or move to configuration.

2. **Service Implementation Priority**: Refer to the architecture document for interface definitions. Implement services in this order:
   - Domain services (`DomainService/Services/`)
   - Application services (`Application/ApplicationService/`)
   - API controllers (`PgSqlVectorSearch/Controllers/`)

3. **Testing**: Test projects are not yet set up. Consider adding xUnit or NUnit tests.

4. **Configuration**: Move hardcoded connection strings and secrets to `appsettings.json` or user secrets.

5. **API Design**: Follow RESTful patterns outlined in the architecture document (section 5.1).

6. **CORS**: Configure Cross-Origin Resource Sharing in `Program.cs` for frontend development (typically port 5173).

## Useful References
- `RAG项目DDD架构设计文档.md` - Comprehensive architecture design (in Chinese)
- `DataMigration/Migrations/` - Database schema evolution
- `DomainService/Entities/` - Core domain models
- `PgSqlVectorSearch/Controllers/` - Example API endpoints