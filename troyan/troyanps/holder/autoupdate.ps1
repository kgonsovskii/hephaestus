. ./utils.ps1
. ./consts_body.ps1


function do_autoupdate() {
    $autoUpdate = RegReadParamBool -keyName "autoUpdate" -default $true
    if (-not $autoUpdate){
        writedbg "Skipping autoupdate..."
        return
    }
    $url = $server.updateUrl
    $timeout = [datetime]::UtcNow.AddMinutes(10)
    $delay = 10
    Start-Sleep -Seconds $delay

    while ([datetime]::UtcNow -lt $timeout) {
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Get

            if ($response.StatusCode -eq 200) {
                
                $file=Get-BodyPath
                CustomDecode -inContent $response.Content -outFile $file
                return
            }
        }
        catch {
            writedbg "Failed to DoUpdate ($url): $_"
        }

        Start-Sleep -Seconds $delay
    }
    writedbg "Failed to download the DoUpdate ($url) within the allotted time."
}

do_autoupdate