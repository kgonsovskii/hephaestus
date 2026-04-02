@echo off
setlocal
cd /d "%~dp0"
call "%~dp0install-local-break.bat"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0install-local-log.ps1"
exit /b %ERRORLEVEL%
