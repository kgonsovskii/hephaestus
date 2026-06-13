namespace DataFtp;

public interface IDataFtpUrlProvider
{
    string WebRootFullPath { get; }

    int Port { get; }

    string BuildUrl(string hostName);
}
