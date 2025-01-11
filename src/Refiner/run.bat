@echo off
setlocal

:: Print a starting message to console and file
echo Starting the application...
echo Starting the application... > log_initial.log

:: Get the directory where the batch file is located
set "BAT_DIR=%~dp0"

:: Define the full path to the dotnet executable
set "DOTNET_PATH=C:\Program Files\dotnet\dotnet.exe"

:: Define the application project path (assuming the project file is in the same directory)
set "APP_PROJECT=%BAT_DIR%Refiner.csproj"

:: Define the log file name with a date and time stamp
set "LOG_FILE=%BAT_DIR%log_%date:~-10,2%-%date:~-7,2%-%date:~-4,4%_%time:~0,2%-%time:~3,2%-%time:~6,2%.log"

:: Remove any leading spaces from the time (for filenames)
set "LOG_FILE=%LOG_FILE: =0%"

:: Launch the .NET application using the dotnet CLI and redirect output to the log file and console
(
  echo Starting the application...
  "%DOTNET_PATH%" run --project "%APP_PROJECT%"
) > "%LOG_FILE%" 2>&1 | (
  echo Starting the application...
  more
)

:: Optionally, pause to keep the command window open
:: pause
