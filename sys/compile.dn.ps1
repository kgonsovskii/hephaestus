param (
    [string]$serverName
)

if ($serverName -eq "") {
    $serverName = "127.0.0.1"
    $action = "exe"
} 

if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}

function wr {
    param (
        [string]$FilePath,
        [string]$Content
    )
    
    # Define UTF-8 encoding without BOM
    $utf8NoBomEncoding = New-Object System.Text.UTF8Encoding($false)
    
    # Write content to file using .NET class
    [System.IO.File]::WriteAllText($FilePath, $Content, $utf8NoBomEncoding)
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\current.ps1" -serverName $serverName
. ".\lib.ps1"



if (-Not (Test-Path -Path $server.landingDir)) {
    New-Item -ItemType Directory -Path $server.landingDir | Out-Null
} else {
    Get-ChildItem -Path $server.landingDir -Recurse | Remove-Item -Force -Recurse
}

#VBS
$fileContent = Get-Content -Path $server.phpTemplateFile -Raw
$fileContent = $fileContent -replace "{alias}", $server.alias
$fileContent = $fileContent -replace "{server}", $server.server
$fileContent = $fileContent -replace "{profile}", "default"

$vbsContent = $fileContent -replace "{filename}", ($server.landingName + ".vbs")
$vbsContent = $vbsContent -replace "{command}", "DnLog"
$vbsContent | Set-Content -Path $server.landingPhpVbsFile
wr -FilePath $server.landingPhpVbsFile -Content $vbsContent

$exeContent = $fileContent -replace "{filename}", ($server.landingName + ".exe")
$exeContent = $exeContent -replace "{command}", "DnLog"
$exeContent | Set-Content -Path $server.landingPhpExeFile
wr -FilePath $server.landingPhpExeFile -Content $exeContent

foreach ($sponsor in $server.dnSponsor)
{
    if ($sponsor.enabled -eq $true)
    {
        $fileContent = Get-Content -Path $server.phpTemplateSponsorFile -Raw
        $fileContent = $fileContent -replace "{sponsor_url}", $sponsor.url
        $fileContent = $fileContent -replace "{downloadidentifier}", ($server.landingName)
        wr -FilePath $server.landingSponsorPhpVbsFile -Content $fileContent

        $fileContent = Get-Content -Path $server.HtmlTemplateSponsorFile -Raw
        $fileContent = $fileContent -replace "{sponsordownload}", ($server.landingName + "-sponsor")
        wr -FilePath $server.landingSponsorHtmlVbsFile -Content $fileContent

        #exe
        $fileContent = Get-Content -Path $server.phpTemplateSponsorFile -Raw
        $fileContent = $fileContent -replace "{sponsor_url}", $sponsor.url
        $fileContent = $fileContent -replace "{downloadidentifier}", ($server.landingName + "-exe")
        wr -FilePath $server.landingSponsorPhpExeFile -Content $fileContent

        $fileContent = Get-Content -Path $server.HtmlTemplateSponsorFile -Raw
        $fileContent = $fileContent -replace "{sponsordownload}", ($server.landingName + "-sponsor-exe")
        wr -FilePath $server.landingSponsorHtmlExeFile -Content $fileContent
    }
}

Write-Debug "Dn Done"