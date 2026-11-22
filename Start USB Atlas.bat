@echo off
setlocal
cd /d "%~dp0"
title USB Atlas

if exist "dist\UsbAtlas.exe" (
  start "" "%~dp0dist\UsbAtlas.exe"
  exit /b 0
)

echo USB Atlas has not been published yet.
echo.
echo Run: .\tools\publish-winui.ps1
echo.
pause
