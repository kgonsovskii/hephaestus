@echo off
setlocal
set /p PROFILE="Hephaestus profile: "
if "%PROFILE%"=="" (
  echo Profile is required.
  exit /b 1
)
cd /d "%~dp0"
call "%~dp0install-remote.bat" "%PROFILE%" %*
exit /b %ERRORLEVEL%
