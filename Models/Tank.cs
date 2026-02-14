using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MultiFuelMaster.Models
{
    /// <summary>
    /// Represents a fuel tank/storage tank
    /// </summary>
    public class Tank
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Tank number
        /// </summary>
        public int Number { get; set; }
        
        /// <summary>
        /// Foreign key to FuelType
        /// </summary>
        public int? FuelTypeId { get; set; }
        
        /// <summary>
        /// Navigation property to FuelType
        /// </summary>
        [ForeignKey("FuelTypeId")]
        public FuelType? FuelType { get; set; }
        
        /// <summary>
        /// Minimum fuel level in centimeters
        /// </summary>
        public double MinLevel { get; set; }
        
        /// <summary>
        /// Maximum fuel level in centimeters
        /// </summary>
        public double MaxLevel { get; set; }
        
        /// <summary>
        /// Minimum fuel volume in liters
        /// </summary>
        public double MinVolume { get; set; }
        
        /// <summary>
        /// Maximum fuel volume in liters
        /// </summary>
        public double MaxVolume { get; set; }
        
        /// <summary>
        /// Critical fuel level threshold
        /// </summary>
        public double? CriticalLevel { get; set; }
        
        /// <summary>
        /// Reaction to critical level: "None", "Warning", "Block"
        /// </summary>
        [MaxLength(50)]
        public string CriticalReaction { get; set; } = "None";
        
        /// <summary>
        /// Whether the tank is locked
        /// </summary>
        public bool IsLocked { get; set; }
        
        /// <summary>
        /// Whether the tank is active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
