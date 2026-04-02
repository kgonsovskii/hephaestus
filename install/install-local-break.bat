@echo off
REM Terminate PowerShell hosts (stops running .ps1 scripts). Run from cmd if you are in PowerShell.
taskkill /F /IM powershell.exe /T >nul 2>&1
taskkill /F /IM pwsh.exe /T >nul 2>&1
exit /b 0
