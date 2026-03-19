# Export Data-Only Script for All Databases
# Usage: .\export-databases-data-only.ps1

param(
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = ".\exports",
    
    [Parameter(Mandatory=$false)]
    [int]$Port = 0,  # 0 means auto-detect
    
    [Parameter(Mandatory=$false)]
    [string]$Password = ""
)

# Get PostgreSQL password from multiple sources if not provided
if ([string]::IsNullOrEmpty($Password)) {
    Write-Host "Attempting to retrieve PostgreSQL password..." -ForegroundColor Yellow
    
    # Method 1: Try user secrets
    Write-Host "  [1/3] Checking user secrets..." -ForegroundColor Gray
    $secretsOutput = dotnet user-secrets list --project "src/Services/STEMify-Backend/STEMify-Backend.AppHost" 2>&1
    $passwordLine = $secretsOutput | Select-String "stemify-postgres-password"
    if ($passwordLine) {
        $Password = ($passwordLine -split " = ")[1].Trim()
        Write-Host "  [OK] Password retrieved from user secrets" -ForegroundColor Green
    } else {
        # Method 2: Try Docker container environment
        Write-Host "  [2/3] Checking Docker container environment..." -ForegroundColor Gray
        # Find all running postgres containers (Docker Compose adds suffixes)
        $runningContainers = docker ps --filter "name=postgres" --format "{{.Names}}" 2>&1
        if ($runningContainers) {
            $containerNames = $runningContainers -split "`n" | Where-Object { $_ -match "postgres" }
            # Also try common names
            $containerNames += @("stemify-postgres", "stemify-postgres-local")
            $containerNames = $containerNames | Select-Object -Unique
            
            foreach ($containerName in $containerNames) {
                $containerName = $containerName.Trim()
                if ([string]::IsNullOrEmpty($containerName)) { continue }
                
                try {
                    Write-Host "    Trying container: $containerName" -ForegroundColor DarkGray
                    # Try to get password from container environment
                    $dockerEnv = docker inspect $containerName --format '{{range .Config.Env}}{{println .}}{{end}}' 2>&1
                    if ($LASTEXITCODE -eq 0 -and $dockerEnv) {
                        # Try multiple patterns for POSTGRES_PASSWORD
                        if ($dockerEnv -match 'POSTGRES_PASSWORD=([^\r\n]+)') {
                            $Password = $matches[1].Trim()
                            Write-Host "  [OK] Password retrieved from Docker container: $containerName" -ForegroundColor Green
                            break
                        }
                        # Also try lowercase
                        if ($dockerEnv -match 'postgres_password=([^\r\n]+)') {
                            $Password = $matches[1].Trim()
                            Write-Host "  [OK] Password retrieved from Docker container (lowercase): $containerName" -ForegroundColor Green
                            break
                        }
                    }
                } catch {
                    # Container might not exist, continue to next
                }
            }
        }
        
        # Method 3: Prompt user securely
        if ([string]::IsNullOrEmpty($Password)) {
            Write-Host "  [3/3] Prompting for password..." -ForegroundColor Gray
            $securePassword = Read-Host "Enter PostgreSQL password" -AsSecureString
            $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
            $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
            [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($BSTR)
            $Password = $Password.Trim()
            Write-Host "  [OK] Password provided" -ForegroundColor Green
        }
    }
    
    if ([string]::IsNullOrEmpty($Password)) {
        Write-Host "Error: Could not retrieve password from any source" -ForegroundColor Red
        Write-Host "Please provide password with -Password parameter or set it in user secrets" -ForegroundColor Yellow
        exit 1
    }
    
    # Debug: Show password info (masked)
    $passwordLength = $Password.Length
    $passwordPreview = if ($passwordLength -gt 0) { 
        $Password.Substring(0, [Math]::Min(3, $passwordLength)) + "***" 
    } else { 
        "***" 
    }
    Write-Host "  [DEBUG] Password length: $passwordLength, preview: $passwordPreview" -ForegroundColor DarkGray
} else {
    # Trim provided password
    $Password = $Password.Trim()
}

# Auto-detect port if not provided
if ($Port -eq 0) {
    Write-Host "Detecting PostgreSQL port..." -ForegroundColor Yellow
    
    # Find all running postgres containers
    $foundPort = $null
    $foundContainer = $null
    
    Write-Host "  Searching for PostgreSQL containers..." -ForegroundColor Gray
    $dockerOutput = docker ps --filter "name=postgres" --format "{{.Names}}|{{.Ports}}" 2>&1
    
    if ($dockerOutput) {
        $lines = $dockerOutput -split "`n" | Where-Object { $_ -match "postgres" }
        foreach ($line in $lines) {
            $line = $line.Trim()
            if ([string]::IsNullOrEmpty($line)) { continue }
            
            Write-Host "    Checking: $line" -ForegroundColor DarkGray
            # Try multiple port mapping formats:
            # 127.0.0.1:5433->5432/tcp
            # 0.0.0.0:5433->5432/tcp
            # ::1:5433->5432/tcp
            # 5433->5432/tcp
            if ($line -match '(?:127\.0\.0\.1|0\.0\.0\.0|::1|)(?::|)(\d+)->5432/tcp') {
                $foundPort = $matches[1]
                if ($line -match '^([^|]+)\|') {
                    $foundContainer = $matches[1]
                }
                Write-Host "  [OK] Found PostgreSQL container: $foundContainer on port: $foundPort" -ForegroundColor Green
                break
            }
        }
    }
    
    if ($foundPort) {
        $Port = [int]$foundPort
        Write-Host "Using port: $Port" -ForegroundColor Green
    } else {
        Write-Host "Error: Could not detect PostgreSQL port automatically" -ForegroundColor Red
        Write-Host "Available PostgreSQL containers:" -ForegroundColor Yellow
        docker ps --filter "name=postgres" --format "  {{.Names}} - {{.Ports}}" 2>&1
        Write-Host "`nPlease provide port with -Port parameter" -ForegroundColor Yellow
        Write-Host "Example: .\export-databases-data.ps1 -Port 5433 -Password 'your-password'" -ForegroundColor Yellow
        exit 1
    }
}

# Test connection before exporting
Write-Host "`nTesting PostgreSQL connection..." -ForegroundColor Yellow
Write-Host "  Connection details:" -ForegroundColor Gray
Write-Host "    Host: localhost" -ForegroundColor Gray
Write-Host "    Port: $Port" -ForegroundColor Gray
Write-Host "    User: postgres" -ForegroundColor Gray
Write-Host "    Password length: $($Password.Length) characters" -ForegroundColor Gray

$env:PGPASSWORD = $Password
$testResult = psql -h localhost -p $Port -U postgres -d postgres -c "SELECT version();" 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "  [OK] Connection successful!" -ForegroundColor Green
    $version = ($testResult | Select-String "PostgreSQL").ToString()
    if ($version) {
        Write-Host "  $version" -ForegroundColor Gray
    }
} else {
    Write-Host "  [FAIL] Connection failed!" -ForegroundColor Red
    Write-Host "  Error details:" -ForegroundColor Red
    $testResult | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    
    Write-Host "`nTroubleshooting steps:" -ForegroundColor Yellow
    Write-Host "  1. Verify container is running:" -ForegroundColor Yellow
    Write-Host "     docker ps --filter 'name=postgres'" -ForegroundColor Gray
    Write-Host "  2. Check container environment for password:" -ForegroundColor Yellow
    Write-Host "     docker inspect stemify-postgres --format '{{range .Config.Env}}{{println .}}{{end}}' | Select-String 'POSTGRES_PASSWORD'" -ForegroundColor Gray
    Write-Host "  3. Try connecting manually:" -ForegroundColor Yellow
    Write-Host "     psql -h localhost -p $Port -U postgres -d postgres" -ForegroundColor Gray
    Write-Host "  4. If password has special characters, use quotes:" -ForegroundColor Yellow
    Write-Host "     .\export-databases-data.ps1 -Port $Port -Password 'your-password'" -ForegroundColor Gray
    
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    exit 1
}
Write-Host ""

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host "Created output directory: $OutputPath" -ForegroundColor Green
}

# Auto-detect databases or use predefined list
Write-Host "Detecting databases..." -ForegroundColor Yellow
$env:PGPASSWORD = $Password
$dbListQuery = "SELECT datname FROM pg_database WHERE datname LIKE 'stemify%' AND datname NOT IN ('postgres', 'template0', 'template1') ORDER BY datname;"
$detectedDbs = psql -h localhost -p $Port -U postgres -d postgres -t -c $dbListQuery 2>&1

$databases = @()
if ($LASTEXITCODE -eq 0 -and $detectedDbs) {
    $databases = ($detectedDbs -split "`n" | Where-Object { $_.Trim() -match '^stemify' } | ForEach-Object { $_.Trim() }) | Where-Object { $_ -ne "" }
    if ($databases.Count -gt 0) {
        Write-Host "  [OK] Auto-detected $($databases.Count) databases:" -ForegroundColor Green
        $databases | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
    } else {
        Write-Host "  [WARN] No databases detected, using predefined list" -ForegroundColor Yellow
        $databases = @(
            "stemify_identity",
            "stemify_classroom",
            "stemify_resource",
            "stemify_notification",
            "stemify_product",
            "stemify_order",
            "stemify_payment",
            "stemify_cart",
            "stemify_hangfire",
            "stemify_aimemory"
        )
    }
} else {
    Write-Host "  [WARN] Could not auto-detect databases, using predefined list" -ForegroundColor Yellow
    # Fallback to predefined list if auto-detection fails
    # Note: Database names use underscores (stemify_identity) not camelCase (stemifyidentity)
    $databases = @(
        "stemify_identity",
        "stemify_classroom",
        "stemify_resource",
        "stemify_notification",
        "stemify_product",
        "stemify_order",
        "stemify_payment",
        "stemify_cart",
        "stemify_hangfire",
        "stemify_aimemory"
    )
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Exporting Data-Only for All Databases" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Host: localhost" -ForegroundColor White
Write-Host "Port: $Port" -ForegroundColor White
Write-Host "Output Path: $OutputPath" -ForegroundColor White
Write-Host "Databases: $($databases.Count)" -ForegroundColor White
Write-Host "========================================`n" -ForegroundColor Cyan

# Password already set in environment variable from connection test

$successCount = 0
$failedCount = 0
$failedDatabases = @()

foreach ($db in $databases) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFileName = "${db}_data_${timestamp}.sql"
    $backupPath = Join-Path $OutputPath $backupFileName
    
    Write-Host "[$($databases.IndexOf($db) + 1)/$($databases.Count)] Exporting data from: $db" -ForegroundColor Yellow
    
    # Export data-only (no schema)
    # Options:
    # --data-only: Only export data, not schema
    # --inserts: Use INSERT statements instead of COPY (more portable)
    # --column-inserts: Include column names in INSERT statements
    # --no-owner: Don't output commands to set ownership
    # --no-privileges: Don't output commands to set privileges
    $result = pg_dump `
        -h localhost `
        -p $Port `
        -U postgres `
        -d $db `
        --data-only `
        --inserts `
        --column-inserts `
        --no-owner `
        --no-privileges `
        -f $backupPath `
        2>&1
    
    if ($LASTEXITCODE -eq 0) {
        $fileSize = (Get-Item $backupPath).Length / 1KB
        $fileSizeFormatted = [math]::Round($fileSize, 2)
        Write-Host "  [OK] Success: $backupFileName ($fileSizeFormatted KB)" -ForegroundColor Green
        $successCount++
    } else {
        Write-Host "  [FAIL] Failed: $db" -ForegroundColor Red
        Write-Host "  Error details:" -ForegroundColor Red
        $result | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
        
        # Additional debug info for authentication errors
        if ($result -match "password authentication failed" -or $result -match "FATAL") {
            Write-Host "  [DEBUG] Connection info:" -ForegroundColor Yellow
            Write-Host "    Host: localhost" -ForegroundColor Yellow
            Write-Host "    Port: $Port" -ForegroundColor Yellow
            Write-Host "    User: postgres" -ForegroundColor Yellow
            Write-Host "    Database: $db" -ForegroundColor Yellow
        }
        
        $failedCount++
        $failedDatabases += $db
    }
}

# Clear password from environment
Remove-Item Env:\PGPASSWORD

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Export Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Total Databases: $($databases.Count)" -ForegroundColor White
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failedCount" -ForegroundColor $(if ($failedCount -gt 0) { "Red" } else { "Green" })

if ($failedDatabases.Count -gt 0) {
    Write-Host "`nFailed Databases:" -ForegroundColor Red
    foreach ($db in $failedDatabases) {
        Write-Host "  - $db" -ForegroundColor Red
    }
}

Write-Host "`nOutput Location: $OutputPath" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

if ($failedCount -eq 0) {
    Write-Host "All databases exported successfully! [OK]" -ForegroundColor Green
} else {
    Write-Host "Some databases failed to export. Please check errors above." -ForegroundColor Yellow
    exit 1
}

# DROP DB PRODUCTION
# DO $$
# DECLARE
#     r RECORD;
# BEGIN
#     FOR r IN (
#         SELECT tablename
#         FROM pg_tables
#         WHERE schemaname = 'public'
#     )
#     LOOP
#         EXECUTE 'TRUNCATE TABLE public.' || quote_ident(r.tablename) || ' CASCADE';
#     END LOOP;
# END $$;
