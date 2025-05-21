param (
    [string]$serverName,  [string]$user="",  [string]$password="", [string]$direct="",  [string]$tag="", [int]$timeout=0
)

#currents
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -Path $scriptDir
. ".\lib.ps1"

if ($serverName -eq "") {
    $serverName = detectServer
} 

if ($direct -ne "true")
{
    . ".\current.ps1" -serverName $serverName
}
. ".\install-lib.ps1" -serverName $serverName -user $user -password $password -direct $direct


if ($direct -eq "true")
{
    $serverIp = $serverName
}
else
{
    $password = $server.clone.clonePassword
    $user=$server.clone.cloneUser
    $serverIp = $server.clone.cloneServerIp
}

Write-Host "Install-Tag $serverName, serverIp $serverIp, tag $tag, $timeout timeout"


function WaitForTag {
    param (
        [string]$tag,
        [int]$timeout
    )
    $result = 0
    $startTime = Get-Date

    while ($true) {
        Start-Sleep -Seconds 1

        $elapsed = (Get-Date) - $startTime
        if ($elapsed.TotalSeconds -ge $timeout) {
            Write-Host "Timeout reached before connection to server."
            return -1
        }

        try {
            $tested = Test
            if ($tested -eq $false)
            {
                Start-Sleep -Seconds 1
                continue
            } 

           $result = Invoke-RemoteCommand -ScriptBlock {
                param (
                    [string]$tag,
                    [int]$timeout
                )
                Write-Host "Waiting for tag $tag ..."
                Set-Content -Path 'C:\install\tagR.txt' -Value $tag
                $filePath = "C:\install\tag.txt"
                $startTime = Get-Date

                function IsTag() {
                    if (Test-Path $filePath) {
                        $content = Get-Content -Path $filePath -Raw
                        $co = $content -like "*$tag*"
                        if ($co -eq $false) {
                            return 1
                        }
                        $co = $content -like "*timeout*"
                        if ($co) {
                            return -1
                        } else {
                            return 0
                        }
                    } else {
                        return 1
                    }
                }

                while ($true) {
                    $elapsed = (Get-Date) - $startTime
                    if ($elapsed.TotalSeconds -ge $timeout) {
                        Write-Host "Timeout reached waiting for tag '$tag'."
                        return -1
                    }

                    $result = IsTag
                    if ($result -eq 0 -or $result -eq -1) {
                        Write-Host "Tag '$tag' detected!"
                        return $result
                    }
                    Start-Sleep -Seconds 3
                }

            } -Arguments @($tag, $timeout)

            break
        }
        catch {
            Write-Host $_
            Start-Sleep -Seconds 3
        }

        Start-Sleep -Seconds 1
    }
    return $result
}

WaitForTag -tag $tag -timeout $timeout