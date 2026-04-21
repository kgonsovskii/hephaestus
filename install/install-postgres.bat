@echo off
setlocal EnableExtensions
cd /d "%~dp0"

net session >nul 2>&1
if %errorLevel% == 0 goto :Run

echo This script needs Administrator rights. Requesting elevation...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
exit /b %ERRORLEVEL%

:Run
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-postgres.ps1"
exit /b %ERRORLEVEL%
