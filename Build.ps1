# Greyrose Build Script
# PowerShell equivalent of build.sh plus local dev commands.
# Usage: .\Build.ps1 <command> [options]

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet("build", "run", "console", "publish", "docker", "patch", "validate-blob", "help")]
    [string]$Command = "help",

    [Alias("r")]
    [string]$Runtime = "",

    [Alias("c")]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$Database = "",

    [Alias("id")]
    [int]$CharId = -1,

    [switch]$Minimal,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ProjectDir = "$PSScriptRoot\wizard101\Greyrose"
$ProjectFile = "$ProjectDir\Greyrose.csproj"
$SolutionFile = "$PSScriptRoot\wizard101\WizPS.sln"
$ArtifactsDir = "$PSScriptRoot\artifacts"

$Runtimes = @("win-x64", "linux-x64", "linux-arm", "linux-arm64")

# ── Helpers ──────────────────────────────────────────────────────────

function Write-Step($msg) { Write-Host "`n>>> $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "  OK  $msg" -ForegroundColor Green }
function Write-Fail($msg) { Write-Host "  FAIL $msg" -ForegroundColor Red }

function Assert-DotNet {
    try { $null = dotnet --version }
    catch {
        Write-Fail ".NET SDK 8.0+ not found. Install from https://dotnet.microsoft.com/download"
        exit 1
    }
}

function Invoke-DotNet([string[]]$Arguments) {
    Write-Host "  dotnet $($Arguments -join ' ')" -ForegroundColor DarkGray
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "dotnet exited with code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

# ── Commands ─────────────────────────────────────────────────────────

function Cmd-Build {
    Write-Step "Building $SolutionFile"
    $args = @("build", $SolutionFile, "-c", $Configuration, "--verbosity", "quiet")
    if ($Runtime) { $args += "-r", $Runtime }
    Invoke-DotNet $args
    Write-Ok "Build complete"
}

function Cmd-Run {
    Write-Step "Running Greyrose (GUI)"
    $args = @("run", "--project", $ProjectFile, "-c", $Configuration)
    if ($Database) { $args += "--", "--db", $Database }
    Invoke-DotNet $args
}

function Cmd-Console {
    Write-Step "Running Greyrose (console)"
    $runArgs = @("run", "--project", $ProjectFile, "-c", $Configuration, "--", "--console")
    if ($Database) { $runArgs += "--db", $Database }
    Invoke-DotNet $runArgs
}

function Cmd-Publish {
    if ($Runtime) {
        Publish-One $Runtime
    } else {
        if (-not $Force) {
            Write-Host "Publishing all 4 runtimes. Use -Runtime <rid> for one, or -Force to confirm." -ForegroundColor Yellow
            Write-Host "  Runtimes: $($Runtimes -join ', ')"
            return
        }
        foreach ($rid in $Runtimes) { Publish-One $rid }
    }
}

function Publish-One([string]$rid) {
    $outDir = "$ArtifactsDir\$rid"
    Write-Step "Publishing $rid -> $outDir"

    $args = @("publish", $ProjectFile, "-c", $Configuration, "-r", $rid,
              "--self-contained", "true", "-o", $outDir)
    if ($rid -eq "win-x64") { $args += "-f", "net8.0-windows" }

    Invoke-DotNet $args

    $bin = Get-ChildItem "$outDir\Greyrose*" | Select-Object -First 1
    if ($bin) {
        $size = "{0:N1} MB" -f ($bin.Length / 1MB)
        Write-Ok "$rid  $($bin.Name)  ($size)"
    } else {
        Write-Fail "$rid  binary not found in $outDir"
    }
}

function Cmd-Docker {
    if (-not (Test-Path "build.sh")) {
        Write-Fail "build.sh not found"
        exit 1
    }
    Write-Step "Building via Docker (build.sh)"
    & "$PSScriptRoot\build.sh"
}

function Cmd-Patch {
    Write-Step "Rebuilding patch list"
    $args = @("run", "--project", $ProjectFile, "-c", $Configuration, "--", "--build-patch-only")
    if ($Minimal) { $args += "--build-patch-minimal" }
    Invoke-DotNet $args
    Write-Ok "Patch list rebuilt"
}

function Cmd-Validate-Blob {
    Write-Step "Validating login blob"
    $args = @("run", "--project", $ProjectFile, "-c", $Configuration, "--")
    if ($CharId -ge 0) {
        $args += "--validate-login-blob", "--char-id", $CharId.ToString()
    } else {
        $args += "--validate-patch-bin"
    }
    Invoke-DotNet $args
}

function Show-Help {
    Write-Host "Greyrose Build Script" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage:  .\Build.ps1 <command> [options]" -ForegroundColor White
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Yellow
    Write-Host "  build              Build the solution"
    Write-Host "  run                Run with Windows GUI"
    Write-Host "  console            Run in console mode (all platforms)"
    Write-Host "  publish            Publish self-contained binary"
    Write-Host "  docker             Multi-platform build via Docker"
    Write-Host "  patch              Rebuild patch list metadata"
    Write-Host "  validate-blob      Validate login blob or patch bin"
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -Runtime <rid>     Target runtime (win-x64, linux-x64, linux-arm, linux-arm64)"
    Write-Host "  -Configuration     Debug or Release (default: Release)"
    Write-Host "  -Database <path>   Custom database path"
    Write-Host "  -CharId <id>       Character ID for blob commands"
    Write-Host "  -Minimal           Minimal patch list (error 16 fix)"
    Write-Host "  -Force             Confirm multi-runtime publish"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\Build.ps1 build"
    Write-Host "  .\Build.ps1 publish -Runtime win-x64"
    Write-Host "  .\Build.ps1 publish -Runtime linux-x64 -Configuration Debug"
    Write-Host "  .\Build.ps1 publish -Force"
    Write-Host "  .\Build.ps1 console -Database C:\data\greyrose.db"
    Write-Host "  .\Build.ps1 patch -Minimal"
    Write-Host "  .\Build.ps1 validate-blob -CharId 1"
    Write-Host "  .\Build.ps1 docker"
}

# ── Entry ────────────────────────────────────────────────────────────

Assert-DotNet

switch ($Command) {
    "build"          { Cmd-Build }
    "run"            { Cmd-Run }
    "console"        { Cmd-Console }
    "publish"        { Cmd-Publish }
    "docker"         { Cmd-Docker }
    "patch"          { Cmd-Patch }
    "validate-blob"  { Cmd-Validate-Blob }
    default          { Show-Help }
}
