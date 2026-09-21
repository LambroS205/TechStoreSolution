# TechStore Project Rules & Architecture Constraints

## 1. Tech Stack & Environment
- Target Framework: .NET 10 LTS (C# 14).
- Architecture: Clean Architecture / Onion Architecture (TechStore.Core -> TechStore.Infrastructure -> TechStore.Web).
- Database: Microsoft SQL Server (LocalDB: (localdb)\MSSQLLocalDB), Entity Framework Core 10.
- Frontend: ASP.NET Core MVC (Razor Views), Tailwind CSS, Vanilla JS ES6 modules, LocalStorage for cart drawer.

## 2. Coding & Architectural Standards
- NEVER use in-memory collections where database querying is appropriate; always use `AsNoTracking()` for read-only queries.
- Keep Domain Entities independent in `TechStore.Core/Entities/` (Single Responsibility, 1 class per file).
- Explicit Foreign Key configurations must be specified in `TechStoreDbContext.cs` using Fluent API.
- All secure actions in Admin controllers must be decorated with `[HasPermission("Module.Action")]`.
- Cache invalidation: Always invalidate `IMemoryCache` key `UserPermissions_{userId}` upon permission change.
- Never write credentials directly into C# files; use `appsettings.json` and environment variables.

## 3. Verification & Testing Requirements
- Every backend change must compile cleanly: `dotnet build`.
- For UI changes, verify using the integrated Browser Agent at `https://localhost:5001`.
