
namespace HomeMaintenanceApp.Models
{
    public class Property
    {
        public string Address { get; set; } = string.Empty;
        public bool HasPool { get; set; } = false;

        // NEW: user-selected trash day (Monday..Sunday)
        public System.DayOfWeek? TrashDay { get; set; }

        public double? SquareFeet { get; set; }
        public int? YearBuilt { get; set; }
    }
}




