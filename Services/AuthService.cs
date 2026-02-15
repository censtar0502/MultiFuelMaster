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
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public AuthService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        /// <summary>
        /// Check if any admin user exists
        /// </summary>
        public async Task<bool> HasAdminUserAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users.AnyAsync(u => u.RoleId == 1 && u.IsActive);
        }

        /// <summary>
        /// Create first super admin user
        /// </summary>
        public async Task<(bool success, string message)> CreateSuperAdminAsync(string login, string password, string? fullName = null)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                
                // Check if already exists
                if (await context.Users.AnyAsync(u => u.Login == login))
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
                    RoleId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                context.Users.Add(user);
                await context.SaveChangesAsync();

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
                await using var context = await _contextFactory.CreateDbContextAsync();
                
                var user = await context.Users
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
                await context.SaveChangesAsync();

                return (true, user, "Вход выполнен успешно");
            }
            catch (Exception ex)
            {
                return (false, null, $"Ошибка входа: {ex.Message}");
            }
        }
    }
}
