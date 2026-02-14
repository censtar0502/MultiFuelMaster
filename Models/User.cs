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
        
        public UserRole Role { get; set; } = UserRole.User;
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
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
        /// Hash password using SHA256 with salt
        /// </summary>
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var salt = "MultiFuelMaster2024";
            var combined = password + salt;
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(bytes);
        }
        
        /// <summary>
        /// Verify password
        /// </summary>
        public bool VerifyPassword(string password)
        {
            return PasswordHash == HashPassword(password);
        }
    }
    
    public enum UserRole
    {
        User,
        Admin,
        SuperAdmin
    }
}
