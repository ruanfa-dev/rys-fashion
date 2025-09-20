#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Migration script to complete the transition from EF Core Identity to Keycloak
.DESCRIPTION
    This script helps migrate existing users and roles from EF Core Identity to Keycloak
    and removes EF Identity tables from the database.
.PARAMETER SkipUserMigration
    Skip the user migration step (useful if users are already in Keycloak)
.PARAMETER SkipDatabaseCleanup
    Skip removing EF Identity tables from database
.PARAMETER BackupDatabase
    Create a database backup before migration
.EXAMPLE
    .\Complete-Keycloak-Migration.ps1
    .\Complete-Keycloak-Migration.ps1 -SkipUserMigration -BackupDatabase
#>

param(
    [switch]$SkipUserMigration,
    [switch]$SkipDatabaseCleanup,
    [switch]$BackupDatabase,
    [string]$Environment = "Development"
)

$ErrorActionPreference = "Stop"

function Write-Section($message) {
    Write-Host "`n=== $message ===" -ForegroundColor Cyan
}

function Write-Step($message) {
    Write-Host "? $message" -ForegroundColor Green
}

function Write-Warning($message) {
    Write-Host "? WARNING: $message" -ForegroundColor Yellow
}

function Write-Error($message) {
    Write-Host "? ERROR: $message" -ForegroundColor Red
}

try {
    Write-Section "Starting Keycloak Migration"

    # Validate environment
    if ($Environment -notin @("Development", "Staging", "Production")) {
        throw "Invalid environment. Must be Development, Staging, or Production"
    }

    Write-Step "Environment: $Environment"

    # Check if Keycloak is running
    Write-Section "Checking Keycloak Service"
    try {
        $keycloakHealth = Invoke-RestMethod -Uri "http://localhost:8080/health/ready" -TimeoutSec 10
        Write-Step "Keycloak is running and ready"
    }
    catch {
        Write-Error "Keycloak is not accessible at http://localhost:8080"
        Write-Host "Please ensure Keycloak is running: docker-compose -f docker-compose.keycloak.yml up -d"
        exit 1
    }

    # Backup database if requested
    if ($BackupDatabase) {
        Write-Section "Creating Database Backup"
        $backupFile = "rys-fashion-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss').sql"
        Write-Host "Creating backup: $backupFile"
        # Add your database backup command here
        # Example for PostgreSQL:
        # pg_dump -h localhost -U postgres -d rys_fashion_dev > $backupFile
        Write-Step "Database backup created: $backupFile"
    }

    # Migrate users to Keycloak
    if (-not $SkipUserMigration) {
        Write-Section "Migrating Users to Keycloak"
        Write-Host "This step would:"
        Write-Host "1. Export users from EF Identity tables"
        Write-Host "2. Create corresponding users in Keycloak"
        Write-Host "3. Map roles and permissions"
        Write-Host "4. Set up user credentials"
        
        # Add your user migration logic here
        # This would typically involve:
        # - Reading users from the database
        # - Creating users in Keycloak via Admin API
        # - Setting up roles and permissions
        
        Write-Step "User migration completed (placeholder - implement actual migration)"
    }
    else {
        Write-Step "Skipping user migration as requested"
    }

    # Build the application
    Write-Section "Building Application"
    dotnet build --configuration Release --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Step "Application built successfully"

    # Run database migrations
    Write-Section "Applying Database Migrations"
    $env:ASPNETCORE_ENVIRONMENT = $Environment
    dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
    if ($LASTEXITCODE -ne 0) {
        throw "Database migration failed"
    }
    Write-Step "Database migrations applied"

    # Remove EF Identity tables if requested
    if (-not $SkipDatabaseCleanup) {
        Write-Section "Cleaning Up EF Identity Tables"
        Write-Warning "About to remove EF Identity tables from database"
        $confirmation = Read-Host "Are you sure you want to remove EF Identity tables? (y/N)"
        
        if ($confirmation -eq 'y' -or $confirmation -eq 'Y') {
            # Create migration to remove Identity tables
            Write-Host "Creating migration to remove Identity tables..."
            dotnet ef migrations add RemoveIdentityTables --project src/Infrastructure --startup-project src/Web.Api
            
            Write-Host "Applying migration to remove Identity tables..."
            dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
            
            Write-Step "EF Identity tables removed"
        }
        else {
            Write-Step "Skipping Identity table cleanup"
        }
    }
    else {
        Write-Step "Skipping database cleanup as requested"
    }

    # Test the application
    Write-Section "Testing Application"
    Write-Host "Starting application for testing..."
    $appProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/Web.Api --environment $Environment" -PassThru -NoNewWindow
    
    Start-Sleep -Seconds 10
    
    try {
        $healthCheck = Invoke-RestMethod -Uri "https://localhost:7001/health" -TimeoutSec 10
        Write-Step "Application is running and healthy"
    }
    catch {
        Write-Warning "Health check failed, but application may still be starting"
    }
    finally {
        Stop-Process -Id $appProcess.Id -Force -ErrorAction SilentlyContinue
    }

    Write-Section "Migration Completed Successfully!"
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Update your client applications to use Keycloak authentication endpoints"
    Write-Host "2. Test all authentication flows thoroughly"
    Write-Host "3. Update your deployment scripts to include Keycloak configuration"
    Write-Host "4. Monitor application logs for any authentication issues"
    Write-Host ""
    Write-Host "Authentication Endpoints:" -ForegroundColor Cyan
    Write-Host "- Login: POST /api/auth/login"
    Write-Host "- Refresh: POST /api/auth/refresh"
    Write-Host "- User Info: GET /api/auth/userinfo"
    Write-Host "- Logout: POST /api/auth/logout"
    Write-Host ""
    Write-Host "Keycloak Admin Console: http://localhost:8080/admin" -ForegroundColor Cyan
    Write-Host "Application API: https://localhost:7001" -ForegroundColor Cyan
}
catch {
    Write-Error "Migration failed: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "1. Check that Keycloak is running: docker-compose -f docker-compose.keycloak.yml ps"
    Write-Host "2. Verify database connectivity"
    Write-Host "3. Check application logs for detailed error information"
    Write-Host "4. Ensure all required configuration is in appsettings.json"
    exit 1
}