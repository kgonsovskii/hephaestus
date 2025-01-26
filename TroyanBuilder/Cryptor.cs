using System;
using System.IO;
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

        public static string EncodeBytes(byte[] bytes)
        {
            var base64 = Convert.ToBase64String(bytes);
            return EncodeBase64(base64);
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
    }
}