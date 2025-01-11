# Stop IIS service if it exists
if (Get-Service -Name W3SVC -ErrorAction SilentlyContinue) {
    Stop-Service -Name W3SVC
} else {
    Write-Host "Service W3SVC does not exist."
}

choco install dotnet-windowshosting --yes --ignore-checksums
choco install dotnet-runtime --yes --ignore-checksums
choco install urlrewrite --yes --ignore-checksums