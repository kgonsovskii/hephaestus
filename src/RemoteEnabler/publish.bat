@echo off
setlocal
cd /d "%~dp0"
dotnet publish "%~dp0RemoteEnabler.csproj" -c Release -r win-x64 --self-contained true -o "..\..\rdp"
