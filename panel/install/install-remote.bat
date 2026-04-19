@echo off
setlocal
cd /d "%~dp0.."
dotnet run --project "install\InstallRemote\InstallRemote.csproj" -- %*
exit /b %ERRORLEVEL%
