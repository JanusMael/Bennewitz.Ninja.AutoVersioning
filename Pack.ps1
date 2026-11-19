#Requires -Version 7
param (
    [Parameter(Mandatory=$false)]
    [string]$OutputDirectory = ".",

    [Parameter(Mandatory=$false)]
    [string]$PackageDirectory = "./packages"
)

$ErrorActionPreference = 'Stop'

# ─── Helpers ──────────────────────────────────────────────────────────────────

function Read-Required {
    param(
        [string]$Prompt,
        [string]$Default = $null,
        [string]$Pattern = $null,
        [string]$PatternHint = $null
    )
    $displayPrompt = $Default ? "$Prompt [$Default]" : $Prompt
    while ($true) {
        $value = (Read-Host $displayPrompt).Trim()
        if ([string]::IsNullOrWhiteSpace($value)) {
            if ($Default) { return $Default }
            Write-Host "  Required — please enter a value." -ForegroundColor Red
            continue
        }
        if ($Pattern -and $value -notmatch $Pattern) {
            Write-Host "  $PatternHint" -ForegroundColor Red
            continue
        }
        return $value
    }
}

# ─── Prompts ──────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "=== Build → Pack ===" -ForegroundColor Cyan
Write-Host ""

$now     = Get-Date
$quarter = [Math]::Ceiling($now.Month / 3)
$mmdd    = [int]$now.ToString('MMdd')
$calVer  = "$($now.Year).$quarter.$mmdd"

$Version = Read-Required `
    -Prompt "NuGet version" `
    -Default $calVer `
    -Pattern '^\d+\.\d+\.\d+(-[A-Za-z0-9]+(\.[A-Za-z0-9]+)*)?$' `
    -PatternHint "Must be a valid SemVer: major.minor.patch with an optional pre-release label (e.g. 1.0.0-beta.1)."

$CsprojPath = Join-Path $OutputDirectory "Bennewitz.Ninja.AutoVersioning.csproj"
if (-not (Test-Path $CsprojPath)) {
    Write-Error "Expected project at '$CsprojPath' — make sure OutputDirectory points to the generated sources."
    exit 1
}

# ⛔ Every packable project is named. There is no solution file here, so nothing is inherited from a
# glob: a project added to packages.push and not added here simply never packs locally, and the
# mismatch shows up only in CI or, worse, in a release.
$AbstractionsPath = Join-Path $OutputDirectory "Abstractions\Bennewitz.Ninja.AutoVersioning.Abstractions.csproj"
if (-not (Test-Path $AbstractionsPath)) {
    Write-Error "Expected project at '$AbstractionsPath' — make sure OutputDirectory points to the repository root."
    exit 1
}

$PackableProjects = @($CsprojPath, $AbstractionsPath)

# ─── Build and Pack ───────────────────────────────────────────────────────────

foreach ($config in @("Debug", "Release")) {
    $configPackageDir = Join-Path $PackageDirectory $config

    if (Test-Path $configPackageDir) {
        Remove-Item -Path (Join-Path $configPackageDir "*.nupkg") -Force -ErrorAction SilentlyContinue
    } else {
        New-Item -ItemType Directory -Path $configPackageDir | Out-Null
    }

    Write-Host ""
    Write-Host "[$config] Building v$Version..." -ForegroundColor Cyan
    dotnet build $CsprojPath -c $config --nologo /p:Version=$Version
    if ($LASTEXITCODE -ne 0) { Write-Error "dotnet build failed ($config)."; exit 1 }

    foreach ($project in $PackableProjects) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($project)
        Write-Host "[$config] Packing $name v$Version..." -ForegroundColor Cyan
        dotnet pack $project -c $config --no-build --nologo `
            -p:Version=$Version `
            --output $configPackageDir
        if ($LASTEXITCODE -ne 0) { Write-Error "dotnet pack failed ($config, $name)."; exit 1 }
    }
}

# ─── Summary ──────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "Done. Packages:" -ForegroundColor Green
Get-ChildItem -Path $PackageDirectory -Filter "*.nupkg" -Recurse |
    ForEach-Object { Write-Host "  $($_.FullName)" -ForegroundColor Green }
