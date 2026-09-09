@echo off
call "%~dp0install\install-remote.bat" %*
exit /b %ERRORLEVEL%
