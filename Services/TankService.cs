using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiFuelMaster.Data;
using MultiFuelMaster.Models;

namespace MultiFuelMaster.Services
{
    public class TankService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public TankService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<Tank>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tanks
                .Include(t => t.FuelType)
                .OrderBy(t => t.Number)
                .ToListAsync();
        }

        public async Task<Tank?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tanks
                .Include(t => t.FuelType)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<FuelType>> GetAvailableFuelTypesAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.FuelTypes
                .Where(ft => ft.IsActive)
                .OrderBy(ft => ft.ShortName)
                .ToListAsync();
        }

        public async Task<Tank> CreateAsync(Tank tank)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            tank.CreatedDate = DateTime.Now;
            tank.LastUpdated = DateTime.Now;
            context.Tanks.Add(tank);
            await context.SaveChangesAsync();
            return tank;
        }

        public async Task UpdateAsync(Tank tank)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.Tanks.FindAsync(tank.Id);
            if (existing != null)
            {
                existing.Number = tank.Number;
                existing.FuelTypeId = tank.FuelTypeId;
                existing.MaxVolume = tank.MaxVolume;
                existing.MinVolume = tank.MinVolume;
                existing.CriticalLevel = tank.CriticalLevel;
                existing.CriticalControl = tank.CriticalControl;
                existing.IsBlockedDuringArrival = tank.IsBlockedDuringArrival;
                existing.CurrentLevel = tank.CurrentLevel;
                existing.Status = tank.Status;
                existing.IsActive = tank.IsActive;
                existing.LastUpdated = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var tank = await context.Tanks.FindAsync(id);
            if (tank != null)
            {
                context.Tanks.Remove(tank);
                await context.SaveChangesAsync();
            }
        }

        public async Task<int> GetNextNumberAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var maxNumber = await context.Tanks.MaxAsync(t => (int?)t.Number) ?? 0;
            return maxNumber + 1;
        }

        public async Task<bool> IsNumberExistsAsync(int number, int? excludeId = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Tanks.Where(t => t.Number == number);
            if (excludeId.HasValue)
            {
                query = query.Where(t => t.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }
    }
}
