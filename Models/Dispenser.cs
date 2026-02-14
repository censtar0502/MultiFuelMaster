using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MultiFuelMaster.Models
{
    /// <summary>
    /// Represents a dispenser/nozzle on a fuel station
    /// </summary>
    public class Dispenser
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int StationId { get; set; }
        
        [ForeignKey("StationId")]
        public virtual FuelStation Station { get; set; } = null!;
        
        [Required]
        public int FuelTypeId { get; set; }
        
        [ForeignKey("FuelTypeId")]
        public virtual FuelType FuelType { get; set; } = null!;
        
        [Required]
        [MaxLength(20)]
        public string DispenserNumber { get; set; } = string.Empty;
        
        public decimal CurrentVolume { get; set; } // Current fuel volume in liters
        public decimal TotalVolume { get; set; }   // Total dispensed volume
        public decimal CurrentPrice { get; set; }  // Current price per liter
        
        public bool IsOperational { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}