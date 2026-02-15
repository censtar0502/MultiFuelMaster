using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiFuelMaster.Data;
using MultiFuelMaster.Models;

namespace MultiFuelMaster.Services
{
    public class FuelTypeService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public FuelTypeService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<List<FuelType>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.FuelTypes
                .OrderBy(f => f.Number)
                .ToListAsync();
        }

        public async Task<FuelType?> GetByIdAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.FuelTypes.FindAsync(id);
        }

        public async Task<FuelType> CreateAsync(FuelType fuelType)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            fuelType.CreatedDate = DateTime.Now;
            fuelType.LastUpdated = DateTime.Now;
            context.FuelTypes.Add(fuelType);
            await context.SaveChangesAsync();
            return fuelType;
        }

        public async Task UpdateAsync(FuelType fuelType)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.FuelTypes.FindAsync(fuelType.Id);
            if (existing != null)
            {
                existing.Number = fuelType.Number;
                existing.ShortName = fuelType.ShortName;
                existing.FullName = fuelType.FullName;
                existing.ReceiptName = fuelType.ReceiptName;
                existing.Unit = fuelType.Unit;
                existing.Color = fuelType.Color;
                existing.IkpuCode = fuelType.IkpuCode;
                existing.PackageCode = fuelType.PackageCode;
                existing.OriginCode = fuelType.OriginCode;
                existing.ClientTin = fuelType.ClientTin;
                existing.IsActive = fuelType.IsActive;
                existing.LastUpdated = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var fuelType = await context.FuelTypes.FindAsync(id);
            if (fuelType != null)
            {
                context.FuelTypes.Remove(fuelType);
                await context.SaveChangesAsync();
            }
        }

        public async Task<int> GetNextNumberAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var maxNumber = await context.FuelTypes.MaxAsync(f => (int?)f.Number) ?? 0;
            return maxNumber + 1;
        }
    }
}
