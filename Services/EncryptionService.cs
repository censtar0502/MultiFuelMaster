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

        /// <summary>
        /// Hash a password using PBKDF2
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                16,
                10000,
                HashAlgorithmName.SHA256);

            byte[] hash = pbkdf2.GetBytes(32);
            byte[] salt = pbkdf2.Salt;

            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Verify a password against a hash
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            try
            {
                var parts = storedHash.Split(':');
                if (parts.Length != 2)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expectedHash = Convert.FromBase64String(parts[1]);

                using var pbkdf2 = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    10000,
                    HashAlgorithmName.SHA256);

                byte[] hash = pbkdf2.GetBytes(32);
                return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
