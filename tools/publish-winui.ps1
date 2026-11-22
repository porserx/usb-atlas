param(
    [string]$Output = "dist",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\UsbAtlas.WinUI\UsbAtlas.WinUI.csproj"
$outDir = Join-Path $root $Output

if (-not (Test-Path $project)) {
    throw "Project not found: $project"
}

dotnet publish $project `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:Platform=x64 `
    -o $outDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exe = Join-Path $outDir "UsbAtlas.exe"
$pri = Join-Path $outDir "UsbAtlas.pri"
if (-not (Test-Path $exe) -or -not (Test-Path $pri)) {
    throw "Publish output is incomplete. Expected UsbAtlas.exe and UsbAtlas.pri in $outDir."
}

Write-Host ""
Write-Host "Published to: $outDir" -ForegroundColor Green
Write-Host "Launch with:  $exe"
Write-Host "Keep the whole dist folder together; the app needs its side-by-side Windows App SDK files."
