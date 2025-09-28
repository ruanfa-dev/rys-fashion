```instructions
# Copilot Instructions for Rys.Fashion (summary)

Backend: .NET 9 monorepo. Solution: `Rys.Shop.sln`.

Key folders (read first):
- `src/Core` — domain layers (entities, value objects, domain events). Example: `src/Core/Catalog/Products/Classification.cs` (uses ErrorOr, domain events, AuditableEntity).
- `src/UseCases` — vertical-slice application handlers, DTOs and `DependencyInjection.cs` that wires handlers.
- `src/Infrastructure` — persistence, storage services, background jobs, and DI registration.
- `src/SharedKernel` — primitives, messaging, errors, and shared types.
- `src/Web.Api` — Program.cs, middleware, controllers, appsettings, and global error handling.

Quick commands (repo root):
- Build solution: `dotnet build Rys.Shop.sln`
- Run API: `dotnet run --project src/Web.Api` (entry: `src/Web.Api/Program.cs`)
- Run tests: `dotnet test --no-build`
- EF CLI helpers: see `docs/scripts/EFCore-CLI-Commands.ps1` and `src/Infrastructure/Migrations`

Project-specific conventions to follow:
- Vertical-slice feature addition: add domain types under `Core/<Feature>`, handlers under `UseCases/<Feature>`, then register services in `UseCases/DependencyInjection.cs` and, if needed, `Infrastructure/DependencyInjection.cs`.
- Use ErrorOr for returning validation/errors. Search for `Error.Validation` and mimic existing error codes (e.g., `Classification.ProductRequired`).
- Domain events: domain entities call `AddDomainEvent(new Events.X(...))`. See `Classification.cs` for pattern and `DispatchDomainEventsInterceptor.cs` in Infrastructure for dispatching.
- Auditable entities: domain models inherit `AuditableEntity` and should call `MarkAsUpdated()` after state changes.
- Prefer file-scoped namespaces, records, and init-only properties in DTOs/value objects (see `SharedKernel` examples).
- Async APIs should accept CancellationToken and propagate it.

Integration & external services:
- Frontend: separate Angular app (not in this repo) — communicates over REST.
- Similarity model: external Python microservice referenced in docs; communicate via REST.
- Storage: pluggable storage implementations under `Infrastructure/Storage/Services`.

Files to inspect before changing behavior:
- `src/Web.Api/Program.cs` — middleware, logging, and app configuration.
- `src/UseCases/DependencyInjection.cs` and `src/Infrastructure/DependencyInjection.cs` — service wiring patterns.
- `src/Core/**` — domain logic and events.
- `tests/**` — unit/functional test examples and patterns.

Small examples to copy from:
- Create/update/delete domain pattern: `src/Core/Catalog/Products/Classification.cs` — shows ErrorOr usage, validation, domain events, and `AuditableEntity` semantics.

When in doubt follow the existing vertical-slice pattern and reuse error codes, event records, and DI registration style. If you need clarification, add a short TODO comment pointing to the canonical example file.

Feedback: tell me what to clarify or any code areas you want instructions for and I'll iterate.
```# Copilot Instructions for Rys.Fashion

## Project Architecture
- **Backend:** .NET 9, organized by domain (see `src/Core`, `src/Infrastructure`, `src/SharedKernel`, `src/UseCases`).
- **Frontend:** Angular 17+ (not included in this repo; see external docs).
- **Visual Similarity Model:** Python (see external `similarity-engine` docs).
- **Microservice-ready:** Each major domain (Catalog, Identity, Medias, Stores, Todos) is separated for scalability.
- **API Layer:** `src/Web.Api` exposes REST endpoints, configures middleware, and handles global error responses.

## Key Patterns & Conventions
- **Domain-Driven Design:** Core logic in `src/Core`; shared abstractions in `src/SharedKernel`.
- **Functional C#:** Use records, value objects, and functional extensions (see `Commons/Extensions`).
- **Vertical Slice:** UseCases are grouped by feature, not by layer (see `src/UseCases/Todos`, etc.).
- **Events & Errors:** Domain events and error types are split into dedicated files (e.g., `TodoItem.Events.cs`, `TodoItem.Errors.cs`).
- **Testing:** xUnit for unit/functional/integration tests (see `tests/`).
- **Configuration:** App settings in `Web.Api/appsettings*.json`; DI setup in `DependencyInjection.cs` files.

## Developer Workflows
- **Build:**
  - Backend: `dotnet build`
- **Run:**
  - Backend: `dotnet run` (entry: `Web.Api/Program.cs`)
- **Test:**
  - Backend: `dotnet test`
- **Frontend:** See external Angular repo.
- **Similarity Model:** See external Python repo.

## Integration Points
- **REST APIs:** All communication between frontends, backend, and model is via REST.
- **Events:** Domain events dispatched via interceptors (see `DispatchDomainEventsInterceptor.cs`).
- **Storage:** Pluggable storage services (see `Infrastructure/Storage/Services`).
- **Security:** Role-based access, JWT constraints (see `Identity/Tokens/Jwt.Contraints.cs`).

## Project-Specific Tips
- **Add new features in vertical slices:** Create new folders in `UseCases` and `Core` for each feature.
- **Extend shared abstractions:** Use `SharedKernel` for cross-cutting concerns (paging, sorting, messaging).
- **Error handling:** Use global exception middleware (`Web.Api/Infrastructure/Middleware`).
- **Testing:** Place tests in matching `tests/*UnitTests` or `*FunctionalTests` projects.

## Example: Adding a Todo Feature
- Define domain models/events/errors in `Core/Todos`.
- Implement endpoints and handlers in `UseCases/Todos`.
- Expose via API in `Web.Api`.
- Add tests in `tests/UseCases.UnitTests`.

## References
- See `README.md` for setup and external component instructions.
- See `docs/` for API response, error handling, and architecture guides.

---

**Feedback:** If any section is unclear or missing, please specify so it can be improved for future AI agents.