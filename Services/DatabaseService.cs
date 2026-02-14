using Microsoft.EntityFrameworkCore;
using MultiFuelMaster.Data;
using MultiFuelMaster.Models;

namespace MultiFuelMaster.Services
{
    /// <summary>
    /// Database service with read-only access control
    /// </summary>
    public class DatabaseService
    {
        private readonly AppDbContext _context;

        public DatabaseService(AppDbContext context)
        {
            _context = context;
        }

        #region Read Operations (Public)

        /// <summary>
        /// Get all fuel stations
        /// </summary>
        public async Task<List<FuelStation>> GetAllStationsAsync()
        {
            return await _context.FuelStations
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Get station by ID
        /// </summary>
        public async Task<FuelStation?> GetStationByIdAsync(int id)
        {
            return await _context.FuelStations
                .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
        }

        /// <summary>
        /// Get all fuel types
        /// </summary>
        public async Task<List<FuelType>> GetAllFuelTypesAsync()
        {
            return await _context.FuelTypes
                .Where(f => f.IsActive)
                .OrderBy(f => f.ShortName)
                .ToListAsync();
        }

        /// <summary>
        /// Get fuel type by ID
        /// </summary>
        public async Task<FuelType?> GetFuelTypeByIdAsync(int id)
        {
            return await _context.FuelTypes
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
        }

        /// <summary>
        /// Get dispensers for a station
        /// </summary>
        public async Task<List<Dispenser>> GetDispensersByStationAsync(int stationId)
        {
            return await _context.Dispensers
                .Include(d => d.FuelType)
                .Where(d => d.StationId == stationId && d.IsOperational)
                .OrderBy(d => d.DispenserNumber)
                .ToListAsync();
        }

        /// <summary>
        /// Get dispenser by ID
        /// </summary>
        public async Task<Dispenser?> GetDispenserByIdAsync(int id)
        {
            return await _context.Dispensers
                .Include(d => d.FuelType)
                .Include(d => d.Station)
                .FirstOrDefaultAsync(d => d.Id == id && d.IsOperational);
        }

        /// <summary>
        /// Get recent transactions
        /// </summary>
        public async Task<List<Transaction>> GetRecentTransactionsAsync(int count = 100)
        {
            return await _context.Transactions
                .Include(t => t.Dispenser)
                .ThenInclude(d => d.FuelType)
                .Include(t => t.Dispenser)
                .ThenInclude(d => d.Station)
                .OrderByDescending(t => t.TransactionDate)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// Get transactions by date range
        /// </summary>
        public async Task<List<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Transactions
                .Include(t => t.Dispenser)
                .ThenInclude(d => d.FuelType)
                .Include(t => t.Dispenser)
                .ThenInclude(d => d.Station)
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get transactions by dispenser
        /// </summary>
        public async Task<List<Transaction>> GetTransactionsByDispenserAsync(int dispenserId, int count = 50)
        {
            return await _context.Transactions
                .Include(t => t.Dispenser)
                .ThenInclude(d => d.FuelType)
                .Where(t => t.DispenserId == dispenserId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// Get statistics for a period
        /// </summary>
        public async Task<TransactionStatistics> GetTransactionStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.Transactions
                .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
                .ToListAsync();

            return new TransactionStatistics
            {
                TotalTransactions = transactions.Count,
                TotalVolume = transactions.Sum(t => t.Volume),
                TotalRevenue = transactions.Sum(t => t.TotalAmount),
                AverageTransactionAmount = transactions.Count > 0 ? transactions.Average(t => t.TotalAmount) : 0,
                StartDate = startDate,
                EndDate = endDate
            };
        }

        #endregion

        #region Write Operations (Internal/Protected - For Setup Only)

        /// <summary>
        /// Initialize database (create tables and seed data)
        /// This should only be called during application setup
        /// </summary>
        internal async Task InitializeDatabaseAsync()
        {
            await _context.Database.EnsureCreatedAsync();
            
            // Check if we need to migrate
            if (_context.Database.GetPendingMigrations().Any())
            {
                await _context.Database.MigrateAsync();
            }
        }

        #endregion
    }

    /// <summary>
    /// Statistics model for transactions
    /// </summary>
    public class TransactionStatistics
    {
        public int TotalTransactions { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageTransactionAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}