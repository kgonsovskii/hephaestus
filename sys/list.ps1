# Get IPv4 addresses excluding loopback, private, and link-local addresses
$networkInterfaces = Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object { 
        $_.IPAddress -ne '127.0.0.1' -and 
        $_.IPAddress -notlike '192.*' -and 
        $_.IPAddress -notlike '10.*' -and 
        $_.IPAddress -notlike '169.*' -and 
        $_.IPAddress -notlike '26.*' 
    } |
    Select-Object -ExpandProperty IPAddress

# If $networkInterfaces is null or not an array, initialize it as an empty array
if (-not $networkInterfaces) {
    $networkInterfaces = @()
} elseif (-not ($networkInterfaces -is [Array])) {
    $networkInterfaces = @($networkInterfaces)
}

# Return the $networkInterfaces array
return $networkInterfaces