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
    $autoStart = RegReadParamBool -keyName "autoStart" -default $true
    if (-not $autoStart)
    {
        writedbg "Skipping autostart..."
        return
    }
    try 
    {
        Add-HolderToStartup
    } catch {
        writedbg "Error  DoRegistryAutoStart $_"
    }
}