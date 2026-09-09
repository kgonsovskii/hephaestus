@echo off
setlocal
pushd "%~dp0" || exit /b 1
echo Root: %CD%
echo Building and running TroyanBuilder...
echo.

dotnet build "troyan\TroyanBuilder\TroyanBuilder.csproj" -o "output" || (
  echo Build failed.
  popd
  exit /b 1
)

pushd "output" || (
  popd
  exit /b 1
)
TroyanBuilder.exe %*
set ERR=%ERRORLEVEL%
popd
popd
echo.
if %ERR% neq 0 (
  echo TroyanBuilder exited with %ERR%.
  exit /b %ERR%
)
echo Done. Outputs under troyan\_output\ including troyan.cmd and troyan.cmd.nonobfuscated
exit /b 0
