@echo off

setlocal EnableExtensions

cd /d "%~dp0"

call "%~dp0install-admin.bat" "uninstall.ps1"

exit /b %ERRORLEVEL%

