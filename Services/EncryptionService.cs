using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MultiFuelMaster.Services
{
    /// <summary>
    /// Service for encrypting and decrypting sensitive data
    /// </summary>
    public class EncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService()
        {
            // Generate key from a fixed password
            var password = "MultiFuelMaster2024SecureKey!@#$";
            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            _iv = new byte[16];
            Array.Copy(_key, _iv, 16);
        }

        /// <summary>
        /// Encrypt a string
        /// </summary>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                
                using var msEncrypt = new MemoryStream();
                using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                
                return Convert.ToBase64String(msEncrypt.ToArray());
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Decrypt a string
        /// </summary>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                var buffer = Convert.FromBase64String(cipherText);
                
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = _iv;
                
                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                
                using var msDecrypt = new MemoryStream(buffer);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);
                
                return srDecrypt.ReadToEnd();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
