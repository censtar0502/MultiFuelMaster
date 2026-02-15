using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MultiFuelMaster.Data;
using MultiFuelMaster.Models;

namespace MultiFuelMaster.Services
{
    /// <summary>
    /// Service for managing station settings
    /// </summary>
    public class StationSettingsService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly EncryptionService _encryptionService;

        public StationSettingsService(IDbContextFactory<AppDbContext> contextFactory, EncryptionService encryptionService)
        {
            _contextFactory = contextFactory;
            _encryptionService = encryptionService;
        }

        /// <summary>
        /// Get decrypted station settings
        /// </summary>
        public async Task<StationSettings> GetSettingsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.StationSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                // Create default settings
                settings = new StationSettings
                {
                    StationNameEncrypted = _encryptionService.Encrypt(""),
                    StationAddressEncrypted = _encryptionService.Encrypt(""),
                    CompanyNameEncrypted = _encryptionService.Encrypt(""),
                    Language = "ru",
                    Currency = "UZS",
                    ArchivePathEncrypted = _encryptionService.Encrypt("")
                };
                
                context.StationSettings.Add(settings);
                await context.SaveChangesAsync();
            }
            
            return settings;
        }

        /// <summary>
        /// Get decrypted settings as a display model
        /// </summary>
        public async Task<StationSettingsDisplayModel> GetDisplayModelAsync()
        {
            var settings = await GetSettingsAsync();
            
            return new StationSettingsDisplayModel
            {
                Id = settings.Id,
                StationName = _encryptionService.Decrypt(settings.StationNameEncrypted),
                StationAddress = _encryptionService.Decrypt(settings.StationAddressEncrypted),
                CompanyName = _encryptionService.Decrypt(settings.CompanyNameEncrypted),
                Language = settings.Language,
                Currency = settings.Currency,
                ArchivePath = _encryptionService.Decrypt(settings.ArchivePathEncrypted)
            };
        }

        /// <summary>
        /// Save station settings (encrypts sensitive fields)
        /// </summary>
        public async Task SaveSettingsAsync(StationSettingsDisplayModel model)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.StationSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                settings = new StationSettings { Id = model.Id };
                context.StationSettings.Add(settings);
            }
            
            settings.StationNameEncrypted = _encryptionService.Encrypt(model.StationName ?? "");
            settings.StationAddressEncrypted = _encryptionService.Encrypt(model.StationAddress ?? "");
            settings.CompanyNameEncrypted = _encryptionService.Encrypt(model.CompanyName ?? "");
            settings.Language = model.Language ?? "ru";
            settings.Currency = model.Currency ?? "UZS";
            settings.ArchivePathEncrypted = _encryptionService.Encrypt(model.ArchivePath ?? "");
            settings.LastModified = DateTime.Now;
            
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Display model for station settings (not encrypted)
    /// </summary>
    public class StationSettingsDisplayModel
    {
        public int Id { get; set; }
        public string? StationName { get; set; }
        public string? StationAddress { get; set; }
        public string? CompanyName { get; set; }
        public string Language { get; set; } = "ru";
        public string Currency { get; set; } = "UZS";
        public string? ArchivePath { get; set; }
    }
}
