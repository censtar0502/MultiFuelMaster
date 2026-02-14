using System.ComponentModel.DataAnnotations;

namespace MultiFuelMaster.Models
{
    /// <summary>
    /// Represents a fuel station/trk (tank filling column)
    /// </summary>
    public class FuelStation
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string Address { get; set; } = string.Empty;
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}