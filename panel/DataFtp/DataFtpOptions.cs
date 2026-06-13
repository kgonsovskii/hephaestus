namespace DataFtp;

public sealed class DataFtpOptions
{
    public const string SectionName = "DataFtp";

    public int Port { get; set; } = DataFtpConstants.DefaultPort;
}
