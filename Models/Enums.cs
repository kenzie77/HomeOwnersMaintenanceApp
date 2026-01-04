

namespace HomeMaintenanceApp.Models
{
    public enum TaskPriority { Low, Medium, High, Critical }
    public enum TaskStatus { NotStarted, InProgress, Completed, Deferred }

    // NEW: recurrence for tasks
    public enum TaskRecurrence { None, Weekly, Monthly, Yearly }

    public enum IssueSeverity { Minor, Moderate, Major, Critical }
    public enum ApplianceType { HVAC, Plumbing, Electrical, Kitchen, Laundry, Other }
}
