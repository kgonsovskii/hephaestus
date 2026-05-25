@echo off
REM Internal: elevate then run install\*.ps1. Usage: install-admin.bat script.ps1
setlocal EnableExtensions
cd /d "%~dp0"

if "%~1"=="" (
  echo Usage: %~nx0 script.ps1
  exit /b 1
)

net session >nul 2>&1
if %errorLevel% == 0 goto :Run

echo This script needs Administrator rights. Requesting elevation...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -ArgumentList '%~1' -Verb RunAs"
exit /b %ERRORLEVEL%

:Run
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0%~1"
exit /b %ERRORLEVEL%
