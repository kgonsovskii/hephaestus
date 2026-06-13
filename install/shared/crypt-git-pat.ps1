# Hephaestus Git PAT obfuscation (XOR + hex). Not strong crypto — keeps github_pat_* out of git plaintext.
param(
    [ValidateSet('encrypt', 'decrypt', 'show')][string]$Action,
    [string]$Value
)

Set-StrictMode -Version Latest

$script:HephaestusGitPatKey = 'HephaestusGitKey42'

function Get-GitPatEncryptedBlobPath {
    Join-Path $PSScriptRoot 'git-pat.enc'
}

function Encrypt-GitPat {
    param([Parameter(Mandatory)][string]$PlainText)
    $keyBytes = [Text.Encoding]::UTF8.GetBytes($script:HephaestusGitPatKey)
    $plainBytes = [Text.Encoding]::UTF8.GetBytes($PlainText)
    $chars = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $plainBytes.Length; $i++) {
        $chars.Add(('{0:x2}' -f ($plainBytes[$i] -bxor $keyBytes[$i % $keyBytes.Length])))
    }
    return -join $chars
}

function Decrypt-GitPat {
    param([Parameter(Mandatory)][string]$EncryptedHex)
    $keyBytes = [Text.Encoding]::UTF8.GetBytes($script:HephaestusGitPatKey)
    $t = $EncryptedHex.Trim()
    if ($t.Length -eq 0 -or ($t.Length % 2) -ne 0) {
        throw 'Encrypted Git PAT hex is empty or has odd length.'
    }
    $bytes = New-Object byte[] ($t.Length / 2)
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        $bytes[$i] = [Convert]::ToByte($t.Substring($i * 2, 2), 16)
    }
    for ($i = 0; $i -lt $bytes.Length; $i++) {
        $bytes[$i] = $bytes[$i] -bxor $keyBytes[$i % $keyBytes.Length]
    }
    return [Text.Encoding]::UTF8.GetString($bytes)
}

function Read-GitPatFromEncryptedFile {
    $path = Get-GitPatEncryptedBlobPath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Encrypted PAT file not found: $path"
    }
    return Decrypt-GitPat ((Get-Content -LiteralPath $path -Raw).Trim())
}

if ($PSBoundParameters.ContainsKey('Action') -and $MyInvocation.InvocationName -ne '.') {
    switch ($Action) {
        'encrypt' {
            if ([string]::IsNullOrWhiteSpace($Value)) { throw 'encrypt requires -Value <token>' }
            Encrypt-GitPat $Value
        }
        'decrypt' {
            if ([string]::IsNullOrWhiteSpace($Value)) { throw 'decrypt requires -Value <hex>' }
            Decrypt-GitPat $Value
        }
        'show' {
            Read-GitPatFromEncryptedFile
        }
    }
}
