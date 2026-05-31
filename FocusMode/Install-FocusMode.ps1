$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$publishDir = Join-Path $scriptDir "publish"
$installDir = "$env:LocalAppData\FocusMode"

Write-Host "=========================================="
Write-Host "       Installing FocusMode App           "
Write-Host "=========================================="
Write-Host ""

if (-not (Test-Path $publishDir)) {
    Write-Host "Error: Could not find 'publish' folder. Make sure it's in the same directory as this script." -ForegroundColor Red
    exit 1
}

Write-Host "Installing to: $installDir"
if (Test-Path $installDir) {
    Write-Host "Removing old installation..."
    # Kill the app if it's running
    Get-Process -Name "FocusMode" -ErrorAction SilentlyContinue | Stop-Process -Force
    Remove-Item $installDir -Recurse -Force
}

Write-Host "Copying files..."
New-Item -ItemType Directory -Path $installDir | Out-Null
Copy-Item -Path "$publishDir\*" -Destination $installDir -Recurse -Force

Write-Host "Creating shortcuts..."
$WshShell = New-Object -comObject WScript.Shell

# Desktop shortcut
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcut = $WshShell.CreateShortcut("$desktopPath\FocusMode.lnk")
$shortcut.TargetPath = "$installDir\FocusMode.exe"
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = "$installDir\FocusMode.exe"
$shortcut.Save()

# Start Menu shortcut
$startMenuPath = [Environment]::GetFolderPath("Programs")
$shortcut = $WshShell.CreateShortcut("$startMenuPath\FocusMode.lnk")
$shortcut.TargetPath = "$installDir\FocusMode.exe"
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = "$installDir\FocusMode.exe"
$shortcut.Save()

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host " Installation Complete! " -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""
Write-Host "You can now launch FocusMode from your Desktop or Start Menu."
