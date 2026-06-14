@echo off
REM Internal: elevate then run install\*.ps1. Usage: install-admin.bat script.ps1 [args...]
setlocal EnableExtensions
cd /d "%~dp0"

if "%~1"=="" (
  echo Usage: %~nx0 script.ps1 [args...]
  exit /b 1
)

set "PS1SCRIPT=%~1"
shift

net session >nul 2>&1
if %errorLevel% == 0 goto :Run

echo This script needs Administrator rights. Requesting elevation...
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -ArgumentList '%PS1SCRIPT%' %* -Verb RunAs"
exit /b %ERRORLEVEL%

:Run
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0%PS1SCRIPT%" %*
exit /b %ERRORLEVEL%
