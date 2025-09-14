# ?? EF Core CLI Commands for Rys-Fashion Project

Assuming your project structure is as follows:

- **Infrastructure Project**: `src/Infrastructure/Infrastructure.csproj`
- **Startup Project**: `src/Web.Api/Web.Api.csproj`
- **DbContext**: `Infrastructure.Persistence.Contexts.ApplicationDbContext`

Ensure that the EF Core CLI tools are installed globally:

```sh
dotnet tool install --global dotnet-ef
```

---

## ?? Migrations Commands

### 1. Add a New Migration

```sh
dotnet ef migrations add <MigrationName> \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web.Api/Web.Api.csproj \
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext \
  --configuration Debug \
  --verbose
```

**Options Explained**:

- `<MigrationName>`: Replace with your desired migration name.
- `--project`: Specifies the project containing the DbContext.
- `--startup-project`: Specifies the startup project to use.
- `--context`: Specifies the DbContext to use.
- `--configuration`: Defines the build configuration (e.g., Debug or Release).
- `--verbose`: Enables detailed output for debugging purposes.

### 2. Remove the Last Migration

```sh
dotnet ef migrations remove \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web.Api/Web.Api.csproj \
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext \
  --configuration Debug \
  --verbose
```

Removes the last migration that was added but not yet applied to the database.

### 3. List All Migrations

```sh
dotnet ef migrations list \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web.Api/Web.Api.csproj \
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext \
  --configuration Debug \
  --verbose
```

Displays a list of all migrations applied to the database.

### 4. Generate SQL Script from Migrations

```sh
dotnet ef migrations script \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web.Api/Web.Api.csproj \
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext \
  --configuration Debug \
  --verbose
```

Generates a SQL script from the migrations.

---

## ??? Database Commands

### 1. Update the Database

```sh
dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web.Api/Web.Api.csproj \
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext \
  --configuration Debug \
  --verbose
```

Applies any pending migrations to the database.

### 2. Drop the Database

```sh
dotnet ef database drop \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web.Api/Web.Api.csproj \
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext \
  --configuration Debug \
  --verbose \
  --force
```

Drops the database associated with the specified DbContext. The `--force` option skips the confirmation prompt.

---

## ?? DbContext Commands

### 1. Scaffold DbContext and Entities from an Existing Database

```sh
dotnet ef dbcontext scaffold "YourConnectionString" Npgsql.EntityFrameworkCore.PostgreSQL \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web.Api/Web.Api.csproj \
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext \
  --output-dir Models \
  --configuration Debug \
  --verbose
```

**Options Explained**:

- `"YourConnectionString"`: Replace with your actual database connection string.
- `Npgsql.EntityFrameworkCore.PostgreSQL`: Specifies the database provider.
- `--output-dir`: Specifies the directory to place the generated models.

### 2. List Available DbContext Types

```sh
dotnet ef dbcontext list \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web.Api/Web.Api.csproj \
  --configuration Debug \
  --verbose
```

Lists all DbContext types in the specified project.

### 3. Get Information About a DbContext

```sh
dotnet ef dbcontext info \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/Web.Api/Web.Api.csproj \
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext \
  --configuration Debug \
  --verbose
```

Displays information about the specified DbContext.

---

## ?? Notes

- **Project Structure**: Ensure that your `DbContext` and entity classes reside in the `src/Infrastructure` project, and that `src/Web.Api` is configured as the startup project.
- **Configuration**: The `--configuration Debug` flag specifies that the Debug build configuration should be used. Adjust this if you're using a different build configuration.
- **Verbose Output**: Use the `--verbose` flag for detailed output during command execution.
