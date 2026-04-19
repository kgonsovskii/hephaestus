# Main script
param (
    [string]$inputFile = "C:\1\troyan.vbs",
    [string]$outputFile = "C:\1\output.vbs",
    [string]$fileType = "vbs" # Default to VBS, can also be "ps1" for PowerShell
)

# Function to generate a random string with a variable name starting with a letter
function Get-RandomVariableName {
    param (
        [int]$length = 10
    )

    $letters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()
    $chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray()

    # Ensure the first character is a letter
    $firstChar = $letters[(Get-Random -Maximum $letters.Length)]
    # Generate the rest of the string
    $restChars = -join ((1..($length - 1)) | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
    
    return "$firstChar$restChars"
}

# Function to generate a random VBS code line with If...Then...Else construction
function Generate-RandomVBScriptLine {
    $variableLength = 7 + (Get-Random -Minimum 0 -Maximum 4) # Ensure variable length is at least 7 characters
    $variable = Get-RandomVariableName -length $variableLength
    $value1 = (Get-Random -Minimum 1 -Maximum 100)
    $value2 = (Get-Random -Minimum 1 -Maximum 100)

    $operationLine = "$variable = $value1"
    $comparison = "$variable < $value2"

    $ifThenElseLine = "If $comparison Then`n    $variable = $variable + 1`nElse`n    $variable = $variable - 1`nEnd If"
    $line = "Dim $variable`n$operationLine`n$ifThenElseLine"
    return $line
}

# Function to generate a random PowerShell code line with If...Else construction
function Generate-RandomPSScriptLine {
    $variableLength = 7 + (Get-Random -Minimum 0 -Maximum 4) # Ensure variable length is at least 7 characters
    $variable = Get-RandomVariableName -length $variableLength
    $value1 = (Get-Random -Minimum 1 -Maximum 100)
    $value2 = (Get-Random -Minimum 1 -Maximum 100)

    $operationLine = "$variable = $value1"
    $comparison = "$variable -lt $value2"

    $ifElseLine = "if ($comparison) {`n    $variable = $variable + 1`n} else {`n    $variable = $variable - 1`n}"
    $line = "$operationLine`n$ifElseLine"
    return $line
}

if (-Not (Test-Path $inputFile)) {
    Write-Error "Input file not found: $inputFile"
    exit
}

# Read the input file
$lines = Get-Content -Path $inputFile

# Prepare the output content
$outputLines = @()
$lineCounter = 0

foreach ($line in $lines) {
    $outputLines += $line
    $lineCounter++
    if ($lineCounter % 3 -eq 0 -or $lineCounter -eq 1) {
        if ($fileType -eq "vbs") {
            $outputLines += Generate-RandomVBScriptLine
        }
    }
}

# Write the output to a new file
$outputLines | Out-File -FilePath $outputFile -Encoding ASCII

Write-Host "Randomized $fileType file has been generated: $outputFile"
