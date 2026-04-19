param (
    [string]$serverName, [string]$serverPassword
)
Write-Host "Setting trusted: $serverName"

function AddTrusted {
    param ($hostname)

    # Set-Item WSMan:\localhost\Client\TrustedHosts -value *
    # Set-Item -force WSMan:\localhost\Client\AllowUnencrypted $true
    # Set-Item -force WSMan:\localhost\Service\AllowUnencrypted $true
    # Set-Item -force WSMan:\localhost\Client\Auth\Digest $true
    # Set-Item -force WSMan:\localhost\Service\Auth\Basic $true

    # Read the current contents of TrustedHosts
    $currentTrustedHosts = (Get-Item WSMan:\localhost\Client\TrustedHosts).Value

    # Check if the currentTrustedHosts is empty or null
    if ([string]::IsNullOrEmpty($currentTrustedHosts)) {
        $newTrustedHosts = $hostname
    } else {
        # Check if the host is already in the TrustedHosts list
        if ($currentTrustedHosts -notmatch [regex]::Escape($hostname)) {
            $newTrustedHosts = "$currentTrustedHosts,$hostname"
        } else {
            # If the host is already in the list, no changes are needed
            $newTrustedHosts = $currentTrustedHosts
        }
    }

    # Update the TrustedHosts list with the new value if it has changed
    if ($currentTrustedHosts -ne $newTrustedHosts) {
        Set-Item WSMan:\localhost\Client\TrustedHosts -Value $newTrustedHosts -Force
    }

    # Display the updated TrustedHosts list
    Get-Item WSMan:\localhost\Client\TrustedHosts
}

AddTrusted($serverName)

if (-not [string]::IsNullOrEmpty($serverPassword) -and $serverPassword -ne "password")
{
   [System.Environment]::SetEnvironmentVariable("SuperPassword_$serverName", $serverPassword, [System.EnvironmentVariableTarget]::Machine)
}