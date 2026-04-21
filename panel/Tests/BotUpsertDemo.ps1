<#
.SYNOPSIS
  Demo client for POST /bot/upsert — builds the JSON envelope + X-Signature the panel expects.

.DESCRIPTION
  Matches panel/cp/BotUpsertSigning.cs:
  - Inner body: BotLogRequest serialized with camelCase policy + explicit elevated_number from JsonPropertyName.
  - Envelope hash: SHA256 UTF-8 bytes -> Base64 (same as ComputeEnvelopeContentHash).
  - X-Signature header: HMACSHA256(secret, UTF-8 canonical JSON body) -> Base64 (same as ComputeXSignature).

  Default secret must match BaseController.SecretKey ("YourSecretKeyHere") unless you override.

.EXAMPLE
  .\BotUpsertDemo.ps1 -BaseUrl 'http://127.0.0.1' -ServerSegment 'default'

.NOTES
  Route: /bot/upsert on BotController. Some deployments also forward /upsert (see CpPipeline).
#>

param(
    [string] $BaseUrl = 'http://127.0.0.1',
    [string] $ServerSegment = 'default',
    [string] $SecretKey = 'YourSecretKeyHere',
    [string] $BotId = 'demo-machine-id',
    [string] $Serie = 'demo-serie',
    [int] $ElevatedNumber = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function ConvertTo-Base64([byte[]]$bytes) {
    return [Convert]::ToBase64String($bytes)
}

function Get-Sha256Base64([string]$text) {
    $enc = [System.Text.Encoding]::UTF8
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ConvertTo-Base64($sha.ComputeHash($enc.GetBytes($text)))
    }
    finally {
        $sha.Dispose()
    }
}

function Get-HmacSha256Base64([string]$text, [string]$key) {
    $enc = [System.Text.Encoding]::UTF8
    # New-Object would splat byte[] into 17 ctor args; use .NET ctor with a single byte[].
    $h = [System.Security.Cryptography.HMACSHA256]::new($enc.GetBytes($key))
    try {
        return ConvertTo-Base64($h.ComputeHash($enc.GetBytes($text)))
    }
    finally {
        $h.Dispose()
    }
}

function Escape-JsonString([string]$s) {
    if ($null -eq $s) { return '' }
    return $s.Replace('\', '\\').Replace('"', '\"')
}

# Canonical inner JSON — must byte-match System.Text.Json.Serialize(BotLogRequest, UpsertJsonOptions).
# Do not use ConvertTo-Json here; spacing/order can differ from the server.
# -f requires {{ and }} for literal { } in the format string (not the {0} placeholders).
$innerJson = ('{{"id":"{0}","serie":"{1}","elevated_number":{2}}}' -f `
        (Escape-JsonString $BotId), `
        (Escape-JsonString $Serie), `
        $ElevatedNumber)

$innerHash = Get-Sha256Base64 $innerJson
$envelopeObj = [ordered]@{
    json  = $innerJson
    hash  = $innerHash
}
$bodyJson = ($envelopeObj | ConvertTo-Json -Compress -Depth 5)

$xSig = Get-HmacSha256Base64 $innerJson $SecretKey

$uri = ($BaseUrl.TrimEnd('/') + '/' + $ServerSegment.Trim('/') + '/bot/upsert')
Write-Host "POST $uri"
Write-Host "X-Signature (HMAC-SHA256 Base64 of inner JSON): $xSig"
Write-Host "Body: $bodyJson"

try {
    $response = Invoke-WebRequest -Uri $uri -Method POST -ContentType 'application/json; charset=utf-8' `
        -Headers @{ 'X-Signature' = $xSig } `
        -Body ([System.Text.Encoding]::UTF8.GetBytes($bodyJson)) `
        -UseBasicParsing
    Write-Host "Status:" $response.StatusCode
    Write-Host $response.Content
}
catch {
    Write-Host "Request failed: $_"
    if ($_.Exception.Response) {
        $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host ($sr.ReadToEnd())
    }
    throw
}
