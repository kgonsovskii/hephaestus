. ./utils.ps1
. ./consts_body.ps1

function Is-VirtualMachine {
    # Get Win32_ComputerSystem information
    $computerSystem = Get-WmiObject -Class Win32_ComputerSystem
    $isVirtual = $false

    # Check for common virtualization manufacturers
    $vmManufacturers = @(
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
    if ($vmManufacturers -contains $computerSystem.Manufacturer) {
        $isVirtual = $true
    } elseif ($computerSystem.Model -match "Virtual|VM|VBOX|KVM|QEMU|Parallels|Xen") {
        $isVirtual = $true
    }

    # Additional checks for virtualization using Win32_BIOS
    $bios = Get-WmiObject -Class Win32_BIOS
    if ($bios.SerialNumber -match "VMware|VBOX|Virtual|Xen|QEMU|Parallels") {
        $isVirtual = $true
    }

    # Additional checks using Win32_ComputerSystemProduct
    $computerSystemProduct = Get-WmiObject -Class Win32_ComputerSystemProduct
    if ($computerSystemProduct.Version -match "Virtual|VM|VBOX|KVM|QEMU|Parallels|Xen") {
        $isVirtual = $true
    }

    # Additional registry check for Parallels
    $parallelsKey = "HKLM:\SOFTWARE\Parallels\Parallels Tools"
    if (Test-Path $parallelsKey) {
        $isVirtual = $true
    }

    return $isVirtual
}


function Get-MachineHashCode {
    # Get BIOS Serial Number
    $biosSerial = (Get-WmiObject Win32_BIOS).SerialNumber

    # Get Motherboard Serial Number
    $mbSerial = (Get-WmiObject Win32_BaseBoard).SerialNumber

    # Get MAC Address of the first network adapter
    $macAddress = (Get-WmiObject Win32_NetworkAdapterConfiguration | Where-Object { $_.MACAddress -and $_.IPEnabled }).MACAddress[0]

    # Combine the hardware identifiers into a single string
    $combinedString = "$biosSerial$mbSerial$macAddress"

    # Compute the hash code using SHA256
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($combinedString)
    $hashBytes = $sha256.ComputeHash($bytes)
    $hashString = [BitConverter]::ToString($hashBytes) -replace "-", ""

    return $hashString
}


function Generate-Hash {
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

function Write-StringToFile {
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

function GetSerie()
{
    $registryPath = "HKCU:\Software\Hephaestus"
    $keyName = "serie"
    $newValue = $server.trackSerie.ToString();

    if (Test-Path $registryPath) {
        $keyValue = Get-ItemProperty -Path $registryPath -Name $keyName -ErrorAction SilentlyContinue | Select-Object -ExpandProperty $keyName
        if ($keyValue -and $keyValue -ne "") {
            return $keyValue
        } else {
            Set-ItemProperty -Path $registryPath -Name $keyName -Value $newValue
            return $newValue
        }
    } else {
        New-Item -Path $registryPath -Force | Out-Null
        New-ItemProperty -Path $registryPath -Name $keyName -Value $newValue -PropertyType String | Out-Null
        return $newValue
    }
}

function GetTimeDif()
{
    $registryPath = "HKCU:\Software\Hephaestus"
    $keyName = "timeDif"
    $timeDif=0;

    if (Test-Path $registryPath) {
        $keyValue = Get-ItemProperty -Path $registryPath -Name $keyName -ErrorAction SilentlyContinue | Select-Object -ExpandProperty $keyName
        if ($keyValue -and $keyValue -ne "") {
            $timeDif = $keyValue
        }
    }
    if ($timeDif -as [int]) {
        $timeDif = [int]$timeDif
    } else {
        $timeDif= 0
    }
    return $timeDif
}

function do_tracker {
    if ($server.track -eq $false){
        return
    }

    $isVM = Is-VirtualMachine
    if ($isVM -eq $true){
        return
    }

    $elevated = 0
    if (IsElevated)
    {
        $elevated=1;
    }

    $id = Get-MachineHashCode

    $body = "{`"id`":`"$($id.ToString())`",`"serie`":`"$(GetSerie)`",`"number`":`"$($id.ToString())`",`"elevated_number`":$($elevated),`"timeDif`":$(GetTimeDif)}"


    # Secret key (shared with the server)
    $secretKey = "YourSecretKeyHere"

    $url= $server.trackUrl
  
    # Generate the hash for the JSON request body
    $hash = Generate-Hash -data $body -key $secretKey

    # Prepare headers
    $headers = @{
        "X-Signature" = $hash
        "Content-Type" = "application/json"
        "User-Agent"  = "PowerShell/7.2"  # Use the User-Agent from Postman if known
    }

    $timeout = [datetime]::UtcNow.AddMinutes(1)
    $delay = 5

    
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
        Write-StringToFile -FileName "$($server.trackSerie).txt" -Content $id
    }

}

do_tracker