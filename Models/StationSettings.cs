using System;
using System.ComponentModel.DataAnnotations;

namespace MultiFuelMaster.Models
{
    /// <summary>
    /// Station settings stored encrypted in database
    /// </summary>
    public class StationSettings
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Station name (encrypted)
        /// </summary>
        public string StationNameEncrypted { get; set; } = string.Empty;
        
        /// <summary>
        /// Station address (encrypted)
        /// </summary>
        public string StationAddressEncrypted { get; set; } = string.Empty;
        
        /// <summary>
        /// Company name (encrypted)
        /// </summary>
        public string CompanyNameEncrypted { get; set; } = string.Empty;
        
        /// <summary>
        /// Language code: ru, uz, en
        /// </summary>
        public string Language { get; set; } = "ru";
        
        /// <summary>
        /// Currency: UZS, USD, etc.
        /// </summary>
        public string Currency { get; set; } = "UZS";
        
        /// <summary>
        /// Archive path (encrypted)
        /// </summary>
        public string ArchivePathEncrypted { get; set; } = string.Empty;
        
        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}
