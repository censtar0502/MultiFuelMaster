using System.ComponentModel.DataAnnotations;

namespace MultiFuelMaster.Models
{
    /// <summary>
    /// Represents different types of fuel
    /// </summary>
    public class FuelType
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Sort order number
        /// </summary>
        public int Number { get; set; }
        
        /// <summary>
        /// Short name (e.g., A-95)
        /// </summary>
        [MaxLength(50)]
        public string ShortName { get; set; } = string.Empty;
        
        /// <summary>
        /// Full name (e.g., Автобензин A-95 Евро)
        /// </summary>
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;
        
        /// <summary>
        /// Name shown on receipt
        /// </summary>
        [MaxLength(100)]
        public string ReceiptName { get; set; } = string.Empty;
        
        /// <summary>
        /// Measurement unit (liters, kg)
        /// </summary>
        [MaxLength(20)]
        public string Unit { get; set; } = "литры";
        
        /// <summary>
        /// Color in hex format (e.g., #FF0000)
        /// </summary>
        [MaxLength(10)]
        public string? Color { get; set; }
        
        /// <summary>
        /// IKPU code (classifier code)
        /// </summary>
        [MaxLength(20)]
        public string? IkpuCode { get; set; }
        
        /// <summary>
        /// Package code
        /// </summary>
        [MaxLength(20)]
        public string? PackageCode { get; set; }
        
        /// <summary>
        /// Origin country code
        /// </summary>
        [MaxLength(20)]
        public string? OriginCode { get; set; }
        
        /// <summary>
        /// Client TIN (Taxpayer Identification Number)
        /// </summary>
        [MaxLength(20)]
        public string? ClientTin { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}