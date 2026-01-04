
using System;

namespace HomeMaintenanceApp.Models
{
    public class Appliance
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Display name (e.g., "LG Refrigerator" or "Carrier HVAC")
        public string Name { get; set; } = string.Empty;

        // Category/type (HVAC, Kitchen, Laundry, etc.)
        public ApplianceType Type { get; set; } = ApplianceType.Other;

        // Optional extra details
        public string Manufacturer { get; set; } = string.Empty;  // NEW
        public string Model { get; set; } = string.Empty;         // NEW
        public string SerialNumber { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public DateTime? InstallDate { get; set; }

        // Optional — useful for fridges, water filters, etc.
        public DateTime? LastFilterChangeDate { get; set; }       // NEW
    }
}
