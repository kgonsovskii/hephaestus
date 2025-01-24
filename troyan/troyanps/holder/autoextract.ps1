. ./utils.ps1
. ./consts_body.ps1
. ./consts_autoextract.ps1

function do_autoextract()
{
    $appDataFolder = Get-HephaestusFolder
    if (-not (Test-Path -Path $appDataFolder))
    {
        New-Item -Path $appDataFolder -ItemType Directory | Out-Null
    }
    $holderBodyFile = Get-BodyPath
    if (-not (Test-Path -Path $holderBodyFile))
    {
        ExtractEmbedding -inContent $xbody -outFile $holderBodyFile
    }
}
