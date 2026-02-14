using System.ComponentModel.DataAnnotations;

namespace MultiFuelMaster.Models
{
    /// <summary>
    /// Represents fuel tank/reservoir for storing fuel
    /// </summary>
    public class Tank
    {
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Tank number/identifier
        /// </summary>
        public int Number { get; set; }
        
        /// <summary>
        /// Reference to fuel type
        /// </summary>
        public int FuelTypeId { get; set; }
        public FuelType? FuelType { get; set; }
        
        /// <summary>
        /// Maximum volume in liters
        /// </summary>
        public double MaxVolume { get; set; }
        
        /// <summary>
        /// Minimum volume in liters
        /// </summary>
        public double MinVolume { get; set; }
        
        /// <summary>
        /// Critical remaining level (percentage or volume)
        /// </summary>
        public double CriticalLevel { get; set; }
        
        /// <summary>
        /// Critical level control type
        /// </summary>
        public CriticalLevelControl CriticalControl { get; set; } = CriticalLevelControl.None;
        
        /// <summary>
        /// Whether tank is blocked during fuel arrival
        /// </summary>
        public bool IsBlockedDuringArrival { get; set; }
        
        /// <summary>
        /// Current fuel level in liters
        /// </summary>
        public double CurrentLevel { get; set; }
        
        /// <summary>
        /// Tank status
        /// </summary>
        public TankStatus Status { get; set; } = TankStatus.Active;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public enum CriticalLevelControl
    {
        None = 0,
        BySalesData = 1,
        ByManualSetting = 2,
        BySensor = 3
    }

    public enum TankStatus
    {
        Active = 0,
        Inactive = 1,
        Maintenance = 2,
        Blocked = 3
    }
}
