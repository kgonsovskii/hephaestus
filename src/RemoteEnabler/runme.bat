@echo off
setlocal
cd /d "%~dp0"
dotnet run --project "%~dp0RemoteEnabler.csproj" -- 78.140.243.76 Administrator W0HmJkdBFyArO061
