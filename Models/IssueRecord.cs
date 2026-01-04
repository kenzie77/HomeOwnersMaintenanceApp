
using System;

namespace HomeMaintenanceApp.Models
{
    public class IssueRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public IssueSeverity Severity { get; set; } = IssueSeverity.Moderate;

        public DateTime ReportedOn { get; set; } = DateTime.Now;

        // Inline resolve checkbox is bound to this
        public bool Resolved { get; set; } = false;

        public Guid? RelatedTaskId { get; set; }
        public Guid? ApplianceId { get; set; }

        // NEW: record what the user tried before calling a pro
        public string? AttemptedSteps { get; set; }

        // NEW: record how the issue was ultimately fixed
        public string? FixNotes { get; set; }
    }
}
