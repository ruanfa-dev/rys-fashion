# EF Core CLI Commands for Rys-Fashion Project (PowerShell)

# Ensure EF Core CLI tools are installed globally
# Run this once if not already installed
# dotnet tool install --global dotnet-ef

# ---
# Migrations Commands

# 1. Add a New Migration
$MigrationName = "<MigrationName>" # Replace with your migration name

# Add migration
& dotnet ef migrations add $MigrationName `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Web.Api/Web.Api.csproj `
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext `
  --configuration Debug `
  --verbose

# 2. Remove the Last Migration
& dotnet ef migrations remove `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Web.Api/Web.Api.csproj `
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext `
  --configuration Debug `
  --verbose

# 3. List All Migrations
& dotnet ef migrations list `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Web.Api/Web.Api.csproj `
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext `
  --configuration Debug `
  --verbose

# 4. Generate SQL Script from Migrations
& dotnet ef migrations script `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Web.Api/Web.Api.csproj `
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext `
  --configuration Debug `
  --verbose

# ---
# Database Commands

# 1. Update the Database
& dotnet ef database update `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Web.Api/Web.Api.csproj `
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext `
  --configuration Debug `
  --verbose

# 2. Drop the Database
& dotnet ef database drop `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Web.Api/Web.Api.csproj `
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext `
  --configuration Debug `
  --verbose `
  --force

# ---
# DbContext Commands

# 1. Scaffold DbContext and Entities from an Existing Database
$ConnectionString = "YourConnectionString" # Replace with your actual connection string

& dotnet ef dbcontext scaffold $ConnectionString Npgsql.EntityFrameworkCore.PostgreSQL `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Web.Api/Web.Api.csproj `
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext `
  --output-dir Models `
  --configuration Debug `
  --verbose

# 2. List Available DbContext Types
& dotnet ef dbcontext list `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Web.Api/Web.Api.csproj `
  --configuration Debug `
  --verbose

# 3. Get Information About a DbContext
& dotnet ef dbcontext info `
  --project src/Infrastructure/Infrastructure.csproj `
  --startup-project src/Web.Api/Web.Api.csproj `
  --context Infrastructure.Persistence.Contexts.ApplicationDbContext `
  --configuration Debug `
  --verbose

# ---
# Notes
# - Ensure your DbContext and entities are in src/Infrastructure
# - src/Web.Api should be the startup project
# - Adjust --configuration if needed
# - Use --verbose for detailed output
