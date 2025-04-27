param (
    [string]$serverIp,
    [string]$user,
    [string]$password,
    [string]$tempScriptPath,
    [string]$remoteScriptPath,
    [string]$arguments
)

$securePassword = ConvertTo-SecureString -String $password -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential ($user, $securePassword)
$session = New-PSSession -ComputerName $serverIp -Credential $credential

# Copy temp.ps1 directly to C:\temp.ps1
Copy-Item -Path $tempScriptPath -Destination $remoteScriptPath -ToSession $session -Force

Start-Sleep -Seconds 1

# Execute C:\temp.ps1 remotely
Invoke-Command -Session $session -ScriptBlock {
    param($scriptPath, $argsLine)
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $scriptPath $argsLine
} -ArgumentList $remoteScriptPath, $arguments -Verbose

Remove-PSSession $session