using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiFuelMaster.Data;
using MultiFuelMaster.Models;

namespace MultiFuelMaster.Services
{
    /// <summary>
    /// Service for handling authentication
    /// </summary>
    public class AuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Check if any admin user exists
        /// </summary>
        public async Task<bool> HasAdminUserAsync()
        {
            return await _context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin && u.IsActive);
        }

        /// <summary>
        /// Create first super admin user
        /// </summary>
        public async Task<(bool success, string message)> CreateSuperAdminAsync(string login, string password, string? fullName = null)
        {
            try
            {
                // Check if already exists
                if (await _context.Users.AnyAsync(u => u.Login == login))
                {
                    return (false, "Пользователь с таким логином уже существует");
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(login) || login.Length < 3)
                {
                    return (false, "Логин должен содержать минимум 3 символа");
                }

                if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
                {
                    return (false, "Пароль должен содержать минимум 4 символа");
                }

                // Create user
                var user = new User
                {
                    Login = login,
                    PasswordHash = User.HashPassword(password),
                    FullName = fullName,
                    Role = UserRole.SuperAdmin,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return (true, "Администратор успешно создан");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при создании пользователя: {ex.Message}");
            }
        }

        /// <summary>
        /// Authenticate user
        /// </summary>
        public async Task<(bool success, User? user, string message)> LoginAsync(string login, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Login == login && u.IsActive);

                if (user == null)
                {
                    return (false, null, "Пользователь не найден");
                }

                if (!user.VerifyPassword(password))
                {
                    return (false, null, "Неверный пароль");
                }

                // Update last login
                user.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync();

                return (true, user, "Вход выполнен успешно");
            }
            catch (Exception ex)
            {
                return (false, null, $"Ошибка входа: {ex.Message}");
            }
        }
    }
}
