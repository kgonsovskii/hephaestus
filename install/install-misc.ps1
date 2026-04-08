$ErrorActionPreference = 'Stop'
$ConfirmPreference = 'None'
$ProgressPreference = 'SilentlyContinue'

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltinRole]::Administrator)

Write-Host 'install-misc: PSPKI, ps2exe, DnsServer module prerequisites' -ForegroundColor Cyan

function Ensure-DnsServerModule {
    if (Get-Module -ListAvailable -Name DnsServer) {
        return
    }
    Write-Host 'install-misc: DnsServer module missing; enabling RSAT DNS / optional features (admin required)...' -ForegroundColor Cyan
    if (-not $isAdmin) {
        throw 'install-misc: Run elevated to install DnsServer (RSAT DNS Server Tools or Windows Server DNS role).'
    }

    if (Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue) {
        try {
            $rsat = Get-WindowsFeature -Name RSAT-DNS-Server -ErrorAction SilentlyContinue
            if ($rsat -and $rsat.InstallState -ne 'Installed') {
                Install-WindowsFeature -Name RSAT-DNS-Server -IncludeManagementTools | Out-Null
            }
        } catch { }
        if (Get-Module -ListAvailable -Name DnsServer) {
            Write-Host 'install-misc: DnsServer module available (Server: RSAT-DNS-Server).' -ForegroundColor Green
            return
        }
    }

    if (Get-Command Add-WindowsCapability -ErrorAction SilentlyContinue) {
        try {
            Get-WindowsCapability -Online -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -like 'Rsat.Dns.Tools*' -and $_.State -ne 'Installed' } |
                ForEach-Object { Add-WindowsCapability -Online -Name $_.Name -ErrorAction SilentlyContinue }
        } catch { }
        if (Get-Module -ListAvailable -Name DnsServer) {
            Write-Host 'install-misc: DnsServer module available (Rsat.Dns.Tools capability).' -ForegroundColor Green
            return
        }
    }

    try {
        $opt = Get-WindowsOptionalFeature -Online -FeatureName RSAT-DNS-Server -ErrorAction SilentlyContinue
        if ($opt -and $opt.State -ne 'Enabled') {
            Enable-WindowsOptionalFeature -Online -FeatureName RSAT-DNS-Server -All -NoRestart -ErrorAction SilentlyContinue
        }
    } catch { }

    if (-not (Get-Module -ListAvailable -Name DnsServer)) {
        throw 'install-misc: DnsServer module still missing. This SKU may need the DNS Server role (see install-dns.ps1) or a different RSAT package.'
    }
    Write-Host 'install-misc: DnsServer module available.' -ForegroundColor Green
}

function Install-PspkiFromPowerShellGallery {
    if (Get-Module -ListAvailable -Name PSPKI) {
        return
    }

    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    $modulesRoot = if ($isAdmin) {
        Join-Path $env:ProgramFiles 'WindowsPowerShell\Modules'
    } else {
        Join-Path $HOME 'Documents\WindowsPowerShell\Modules'
    }
    New-Item -ItemType Directory -Force -Path $modulesRoot | Out-Null

    # .nupkg is a zip; Expand-Archive needs .zip extension on older PowerShell
    $nupkg = Join-Path $env:TEMP ('PSPKI.' + [Guid]::NewGuid().ToString('n') + '.nupkg')
    $zipCopy = "$nupkg.zip"
    $stage = Join-Path $env:TEMP ('pspkig_' + [Guid]::NewGuid().ToString('n'))

    try {
        Write-Host 'install-misc: Downloading PSPKI from PowerShell Gallery (API v2, no NuGet provider)...'
        Invoke-WebRequest -Uri 'https://www.powershellgallery.com/api/v2/package/PSPKI' -OutFile $nupkg -UseBasicParsing -MaximumRedirection 5

        if (Test-Path -LiteralPath $zipCopy) { Remove-Item -LiteralPath $zipCopy -Force }
        Copy-Item -LiteralPath $nupkg -Destination $zipCopy -Force

        if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
        Expand-Archive -LiteralPath $zipCopy -DestinationPath $stage -Force

        $psd1 = Get-ChildItem -Path $stage -Filter 'PSPKI.psd1' -Recurse -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if (-not $psd1) {
            throw 'install-misc: PSPKI.psd1 not found inside Gallery package.'
        }

        $srcDir = $psd1.Directory.FullName
        $ver = $null
        $raw = Get-Content -LiteralPath $psd1.FullName -Raw
        if ($raw -match "ModuleVersion\s*=\s*'([^']+)'") {
            $ver = $Matches[1]
        }
        elseif ($raw -match 'ModuleVersion\s*=\s*"([^"]+)"') {
            $ver = $Matches[1]
        }
        if (-not $ver) {
            throw 'install-misc: Could not read ModuleVersion from PSPKI.psd1.'
        }

        $dest = Join-Path $modulesRoot "PSPKI\$ver"
        if (Test-Path -LiteralPath $dest) {
            Remove-Item -LiteralPath $dest -Recurse -Force
        }
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item -Path (Join-Path $srcDir '*') -Destination $dest -Recurse -Force

        Write-Host "install-misc: PSPKI $ver installed to $dest" -ForegroundColor Green
    } finally {
        Remove-Item -LiteralPath $nupkg -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $zipCopy -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Install-Ps2ExeFromPowerShellGallery {
    if (Get-Module -ListAvailable -Name ps2exe) {
        return
    }

    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    $modulesRoot = if ($isAdmin) {
        Join-Path $env:ProgramFiles 'WindowsPowerShell\Modules'
    } else {
        Join-Path $HOME 'Documents\WindowsPowerShell\Modules'
    }
    New-Item -ItemType Directory -Force -Path $modulesRoot | Out-Null

    $nupkg = Join-Path $env:TEMP ('ps2exe.' + [Guid]::NewGuid().ToString('n') + '.nupkg')
    $zipCopy = "$nupkg.zip"
    $stage = Join-Path $env:TEMP ('ps2exeg_' + [Guid]::NewGuid().ToString('n'))

    try {
        Write-Host 'install-misc: Downloading ps2exe from PowerShell Gallery (API v2, no NuGet provider)...'
        Invoke-WebRequest -Uri 'https://www.powershellgallery.com/api/v2/package/ps2exe' -OutFile $nupkg -UseBasicParsing -MaximumRedirection 5

        if (Test-Path -LiteralPath $zipCopy) { Remove-Item -LiteralPath $zipCopy -Force }
        Copy-Item -LiteralPath $nupkg -Destination $zipCopy -Force

        if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
        Expand-Archive -LiteralPath $zipCopy -DestinationPath $stage -Force

        $psd1 = Get-ChildItem -Path $stage -File -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -ieq 'ps2exe.psd1' } |
            Select-Object -First 1
        if (-not $psd1) {
            throw 'install-misc: ps2exe.psd1 not found inside Gallery package.'
        }

        $srcDir = $psd1.Directory.FullName
        $ver = $null
        $raw = Get-Content -LiteralPath $psd1.FullName -Raw
        if ($raw -match "ModuleVersion\s*=\s*'([^']+)'") {
            $ver = $Matches[1]
        }
        elseif ($raw -match 'ModuleVersion\s*=\s*"([^"]+)"') {
            $ver = $Matches[1]
        }
        if (-not $ver) {
            throw 'install-misc: Could not read ModuleVersion from ps2exe.psd1.'
        }

        $dest = Join-Path $modulesRoot "ps2exe\$ver"
        if (Test-Path -LiteralPath $dest) {
            Remove-Item -LiteralPath $dest -Recurse -Force
        }
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Copy-Item -Path (Join-Path $srcDir '*') -Destination $dest -Recurse -Force

        Write-Host "install-misc: ps2exe $ver installed to $dest" -ForegroundColor Green
    } finally {
        Remove-Item -LiteralPath $nupkg -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $zipCopy -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Install-PspkiFromPowerShellGallery
Install-Ps2ExeFromPowerShellGallery

Ensure-DnsServerModule

Import-Module PSPKI -ErrorAction Stop
Write-Host 'install-misc: PSPKI loaded OK.' -ForegroundColor Green

Import-Module ps2exe -ErrorAction Stop
$null = Get-Command Invoke-ps2exe -ErrorAction Stop
Write-Host 'install-misc: ps2exe (Invoke-ps2exe) loaded OK.' -ForegroundColor Green

Import-Module DnsServer -ErrorAction Stop
Write-Host 'install-misc: DnsServer loaded OK.' -ForegroundColor Green
