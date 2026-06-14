@echo off
setlocal EnableExtensions
cd /d "%~dp0"
call "%~dp0win\install.bat" %*
exit /b %ERRORLEVEL%
