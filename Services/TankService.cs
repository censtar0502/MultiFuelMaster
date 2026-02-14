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
        private readonly AppDbContext _context;

        public TankService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Tank>> GetAllAsync()
        {
            return await _context.Tanks
                .Include(t => t.FuelType)
                .OrderBy(t => t.Number)
                .ToListAsync();
        }

        public async Task<Tank?> GetByIdAsync(int id)
        {
            return await _context.Tanks
                .Include(t => t.FuelType)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<FuelType>> GetAvailableFuelTypesAsync()
        {
            return await _context.FuelTypes
                .Where(ft => ft.IsActive)
                .OrderBy(ft => ft.ShortName)
                .ToListAsync();
        }

        public async Task<Tank> CreateAsync(Tank tank)
        {
            tank.CreatedDate = DateTime.Now;
            tank.LastUpdated = DateTime.Now;
            _context.Tanks.Add(tank);
            await _context.SaveChangesAsync();
            return tank;
        }

        public async Task UpdateAsync(Tank tank)
        {
            var existing = await _context.Tanks.FindAsync(tank.Id);
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
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var tank = await _context.Tanks.FindAsync(id);
            if (tank != null)
            {
                _context.Tanks.Remove(tank);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetNextNumberAsync()
        {
            var maxNumber = await _context.Tanks.MaxAsync(t => (int?)t.Number) ?? 0;
            return maxNumber + 1;
        }

        public async Task<bool> IsNumberExistsAsync(int number, int? excludeId = null)
        {
            var query = _context.Tanks.Where(t => t.Number == number);
            if (excludeId.HasValue)
            {
                query = query.Where(t => t.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }
    }
}
