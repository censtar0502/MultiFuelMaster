using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MultiFuelMaster.Models
{
    /// <summary>
    /// Represents a fuel transaction
    /// </summary>
    public class Transaction
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int DispenserId { get; set; }
        
        [ForeignKey("DispenserId")]
        public virtual Dispenser Dispenser { get; set; } = null!;
        
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        
        public decimal Volume { get; set; }    // Liters dispensed
        public decimal PricePerLiter { get; set; }
        public decimal TotalAmount { get; set; } // Volume * PricePerLiter
        
        [MaxLength(50)]
        public string? VehiclePlate { get; set; }
        
        [MaxLength(100)]
        public string? DriverName { get; set; }
        
        public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
    
    public enum TransactionStatus
    {
        Started,
        InProgress,
        Completed,
        Cancelled,
        Error
    }
}