using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace MultiFuelMaster.Models
{
    /// <summary>
    /// Represents a user in the system
    /// </summary>
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Login { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? FullName { get; set; }
        
        /// <summary>
        /// ID роли пользователя
        /// </summary>
        public int RoleId { get; set; }
        
        /// <summary>
        /// Навигационное свойство роли
        /// </summary>
        public UserRole? Role { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public DateTime LastLogin { get; set; }
        
        /// <summary>
        /// Время входа в систему
        /// </summary>
        public DateTime? LoginTime { get; set; }
        
        /// <summary>
        /// Время выхода из системы
        /// </summary>
        public DateTime? LogoutTime { get; set; }
        
        /// <summary>
        /// Hash password using PBKDF2
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
        /// Verify password
        /// </summary>
        public bool VerifyPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(PasswordHash))
                return false;

            try
            {
                var parts = PasswordHash.Split(':');
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
