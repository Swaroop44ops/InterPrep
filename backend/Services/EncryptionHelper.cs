using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace backend.Services
{
    public static class EncryptionHelper
    {
        // 32-byte key for AES-256 (256 bits)
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("aG93IGRvZXMgdGhlIGRvZyBnb2VzPyE="); // 32 characters key
        // 16-byte initialization vector (IV)
        private static readonly byte[] Iv = Encoding.UTF8.GetBytes("MTIzNDU2Nzg5MDEy"); // 16 characters IV (pad/resize to fit 16 bytes)

        private static byte[] EnsureLength(byte[] input, int length)
        {
            var result = new byte[length];
            Array.Copy(input, result, Math.Min(input.Length, length));
            return result;
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = EnsureLength(Key, 32);
            aes.IV = EnsureLength(Iv, 16);

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.Key = EnsureLength(Key, 32);
                aes.IV = EnsureLength(Iv, 16);

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using var ms = new MemoryStream(cipherBytes);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch
            {
                return "Decryption Error";
            }
        }
    }
}
