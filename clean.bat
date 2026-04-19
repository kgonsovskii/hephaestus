@echo off
setlocal
pushd "%~dp0" || exit /b 1
echo Root: %CD%
echo Removing all folders named "bin" or "obj" under this tree...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -Command "$r=(Get-Location).ProviderPath; $all=@(Get-ChildItem -LiteralPath $r -Recurse -Directory -Force -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq 'bin' -or $_.Name -eq 'obj' }); $sorted=$all | Sort-Object { $_.FullName.Length } -Descending; foreach ($d in $sorted) { Write-Host ('  ' + $d.FullName); Remove-Item -LiteralPath $d.FullName -Recurse -Force -ErrorAction SilentlyContinue }; Write-Host ''; Write-Host ('Removed ' + $sorted.Count + ' folder(s).')"

popd
echo.
echo Done.
exit /b 0
