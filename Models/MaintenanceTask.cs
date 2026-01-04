
using System;

namespace HomeMaintenanceApp.Models
{
    public class MaintenanceTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public TaskStatus Status { get; set; } = TaskStatus.NotStarted;
        public DateTime? DueDate { get; set; }

        public Guid? ApplianceId { get; set; }

        // NEW: recurrence + last completion
        public TaskRecurrence Recurrence { get; set; } = TaskRecurrence.None;
        public DateTime? LastCompletedOn { get; set; }
    }
}


