# Crypto helpers from troyan/troyanps/tracker.ps1 (Generate-Hash) and utils.ps1 (Get-SHA256HashBase64, EnvelopeIt).
param(
    [Parameter(Mandatory)][string]$InnerJson,
    [Parameter(Mandatory)][string]$SecretKey,
    [ValidateSet('Hmac', 'Sha256', 'Envelope')][string]$Operation = 'Hmac'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Generate-Hash {
    param([string]$data, [string]$key)
    $keyBytes = [System.Text.Encoding]::UTF8.GetBytes($key)
    $dataBytes = [System.Text.Encoding]::UTF8.GetBytes($data)
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $keyBytes
    $hashBytes = $hmac.ComputeHash($dataBytes)
    return [Convert]::ToBase64String($hashBytes)
}

function Get-SHA256HashBase64 {
    param([string]$inputString)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $byteArray = [System.Text.Encoding]::UTF8.GetBytes($inputString)
        $hashBytes = $sha256.ComputeHash($byteArray)
        return [Convert]::ToBase64String($hashBytes)
    }
    finally {
        $sha256.Dispose()
    }
}

function EnvelopeIt {
    param([string]$inputString)
    $hash = Get-SHA256HashBase64 -inputString $inputString
    $envelope = @{
        json = $inputString
        hash = $hash
    }
    return ($envelope | ConvertTo-Json -Compress)
}

switch ($Operation) {
    'Hmac' { Write-Output (Generate-Hash -data $InnerJson -key $SecretKey) }
    'Sha256' { Write-Output (Get-SHA256HashBase64 -inputString $InnerJson) }
    'Envelope' { Write-Output (EnvelopeIt -inputString $InnerJson) }
}
