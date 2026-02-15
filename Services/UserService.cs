using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiFuelMaster.Data;
using MultiFuelMaster.Models;

namespace MultiFuelMaster.Services
{
    public class UserService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public UserService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<User>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Login)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<List<UserRole>> GetAllRolesAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.UserRoles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            user.CreatedDate = DateTime.Now;
            user.LastUpdated = DateTime.Now;
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.Users.FindAsync(user.Id);
            if (existing != null)
            {
                existing.Login = user.Login;
                existing.FullName = user.FullName;
                existing.RoleId = user.RoleId;
                existing.IsActive = user.IsActive;
                existing.LastUpdated = DateTime.Now;
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    existing.PasswordHash = user.PasswordHash;
                }
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.FindAsync(id);
            if (user != null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsLoginExistsAsync(string login, int? excludeId = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Users.Where(u => u.Login == login);
            if (excludeId.HasValue)
            {
                query = query.Where(u => u.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<UserRole> CreateRoleAsync(UserRole role)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            role.CreatedDate = DateTime.Now;
            role.LastUpdated = DateTime.Now;
            context.UserRoles.Add(role);
            await context.SaveChangesAsync();
            return role;
        }

        public async Task UpdateRoleAsync(UserRole role)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.UserRoles.FindAsync(role.Id);
            if (existing != null)
            {
                existing.Name = role.Name;
                existing.Description = role.Description;
                existing.IsActive = role.IsActive;
                existing.LastUpdated = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteRoleAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var role = await context.UserRoles.FindAsync(id);
            if (role != null)
            {
                context.UserRoles.Remove(role);
                await context.SaveChangesAsync();
            }
        }
    }
}
