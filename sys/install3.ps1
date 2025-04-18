
function Trigger {
    $TaskName = "_T"
    $ExePath = "C:\inetpub\wwwroot\cp\Refiner.exe"
  
    if (Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue) {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
    }
$TaskXML = @"
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
<RegistrationInfo>
    <Date>2025-03-30T21:57:39.6737798</Date>
    <Author>W\Administrator</Author>
    <URI>\_T</URI>
</RegistrationInfo>
<Triggers>
    <BootTrigger>
    <Repetition>
        <Interval>PT30M</Interval>
        <StopAtDurationEnd>false</StopAtDurationEnd>
    </Repetition>
    <Enabled>true</Enabled>
    </BootTrigger>
</Triggers>
<Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>false</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
    <StopOnIdleEnd>true</StopOnIdleEnd>
    <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT72H</ExecutionTimeLimit>
    <Priority>7</Priority>
</Settings>
<Actions Context="Author">
    <Exec>
    <Command>C:\inetpub\wwwroot\cp\Refiner.exe</Command>
    </Exec>
</Actions>
</Task>
"@
  
  # Save the XML to a temporary file
  $TaskXMLPath = "$env:TEMP\TaskDefinition.xml"
  $TaskXML | Set-Content -Path $TaskXMLPath -Encoding Unicode
  
  # Register the task using the XML file
  schtasks /Create /XML $TaskXMLPath /TN $TaskName /F
  
  # Cleanup temporary XML file
  Remove-Item -Path $TaskXMLPath -Force
  
  Write-Output "Task '$TaskName' has been created and will repeat every 10 minutes indefinitely."
}
  
Trigger
  