@echo off
setlocal EnableExtensions
cd /d "%~dp0"
call "%~dp0install-admin.bat" "install-git.ps1"
exit /b %ERRORLEVEL%
