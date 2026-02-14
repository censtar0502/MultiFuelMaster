using System;
using System.Collections.Generic;
using System.Linq;
using MultiFuelMaster.Models;

namespace MultiFuelMaster.Converters
{
    public static class EnumHelper
    {
        public static List<CriticalLevelControl> CriticalLevelControls { get; } = 
            Enum.GetValues(typeof(CriticalLevelControl)).Cast<CriticalLevelControl>().ToList();

        public static List<TankStatus> TankStatuses { get; } = 
            Enum.GetValues(typeof(TankStatus)).Cast<TankStatus>().ToList();
    }
}
