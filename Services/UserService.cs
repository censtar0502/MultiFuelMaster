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
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Login)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<List<UserRole>> GetAllRolesAsync()
        {
            return await _context.UserRoles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            user.CreatedDate = DateTime.Now;
            user.LastUpdated = DateTime.Now;
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateAsync(User user)
        {
            var existing = await _context.Users.FindAsync(user.Id);
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
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsLoginExistsAsync(string login, int? excludeId = null)
        {
            var query = _context.Users.Where(u => u.Login == login);
            if (excludeId.HasValue)
            {
                query = query.Where(u => u.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }

        public async Task<UserRole> CreateRoleAsync(UserRole role)
        {
            role.CreatedDate = DateTime.Now;
            role.LastUpdated = DateTime.Now;
            _context.UserRoles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        public async Task UpdateRoleAsync(UserRole role)
        {
            var existing = await _context.UserRoles.FindAsync(role.Id);
            if (existing != null)
            {
                existing.Name = role.Name;
                existing.Description = role.Description;
                existing.IsActive = role.IsActive;
                existing.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteRoleAsync(int id)
        {
            var role = await _context.UserRoles.FindAsync(id);
            if (role != null)
            {
                _context.UserRoles.Remove(role);
                await _context.SaveChangesAsync();
            }
        }
    }
}
