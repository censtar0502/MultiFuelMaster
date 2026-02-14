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
        private readonly AppDbContext _context;

        public FuelTypeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<FuelType>> GetAllAsync()
        {
            return await _context.FuelTypes
                .OrderBy(f => f.Number)
                .ToListAsync();
        }

        public async Task<FuelType?> GetByIdAsync(int id)
        {
            return await _context.FuelTypes.FindAsync(id);
        }

        public async Task<FuelType> CreateAsync(FuelType fuelType)
        {
            fuelType.CreatedDate = DateTime.Now;
            fuelType.LastUpdated = DateTime.Now;
            _context.FuelTypes.Add(fuelType);
            await _context.SaveChangesAsync();
            return fuelType;
        }

        public async Task UpdateAsync(FuelType fuelType)
        {
            var existing = await _context.FuelTypes.FindAsync(fuelType.Id);
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
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var fuelType = await _context.FuelTypes.FindAsync(id);
            if (fuelType != null)
            {
                _context.FuelTypes.Remove(fuelType);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetNextNumberAsync()
        {
            var maxNumber = await _context.FuelTypes.MaxAsync(f => (int?)f.Number) ?? 0;
            return maxNumber + 1;
        }
    }
}
