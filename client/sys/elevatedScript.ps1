param (
    [string]$serverName,
    [string]$scriptPath,
    [string]$tempFile
)

# Run the provided script path with the given parameters
& $scriptPath -serverName $serverName | Out-File -FilePath $tempFile

# Signal completion
"Completed" | Out-File -FilePath "$tempFile.complete"