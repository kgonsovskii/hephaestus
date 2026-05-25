@echo off
setlocal EnableExtensions
cd /d "%~dp0"
call "%~dp0install-admin.bat" "install-net.ps1"
exit /b %ERRORLEVEL%
