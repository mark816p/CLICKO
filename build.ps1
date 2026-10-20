# CLICKO PowerShell Build Script
Write-Host "[CLICKO] Building standalone CLICKO.exe..." -ForegroundColor Cyan

$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscPath)) {
    $cscPath = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path $cscPath)) {
    Write-Error "[ERROR] csc.exe compiler not found in standard .NET Framework locations."
    exit 1
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$iconPath = Join-Path $scriptDir "icon.ico"
$outExe = Join-Path $scriptDir "CLICKO.exe"
$srcDir = Join-Path $scriptDir "src"

$sources = @(
    (Join-Path $srcDir "Win32.cs"),
    (Join-Path $srcDir "DeviceDetector.cs"),
    (Join-Path $srcDir "ClickStats.cs"),
    (Join-Path $srcDir "StartupManager.cs"),
    (Join-Path $srcDir "DashboardForm.cs"),
    (Join-Path $srcDir "TrayContext.cs"),
    (Join-Path $srcDir "Program.cs"),
    (Join-Path $srcDir "IconGenerator.cs")
)

$argsList = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/win32icon:$iconPath",
    "/r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Web.Extensions.dll",
    "/out:$outExe"
) + $sources

& $cscPath $argsList

if ($LASTEXITCODE -eq 0) {
    $fileInfo = Get-Item $outExe
    Write-Host "[SUCCESS] CLICKO.exe built successfully! ($([math]::Round($fileInfo.Length / 1KB, 1)) KB)" -ForegroundColor Green
    Write-Host "Output: $outExe" -ForegroundColor Gray
} else {
    Write-Host "[ERROR] Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
