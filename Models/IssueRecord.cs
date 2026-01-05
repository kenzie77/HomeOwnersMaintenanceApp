
using System;

namespace HomeMaintenanceApp.Models
{
    public enum IssueSeverity
    {
        Minor,
        Moderate,
        Major,
        Critical
    }

    /// <summary>
    /// Represents a single issue the user reports, tracks, and resolves.
    /// </summary>
    public class IssueRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Core details
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Severity & dates
        public IssueSeverity Severity { get; set; } = IssueSeverity.Moderate;

        /// <summary>
        /// When the issue was first reported/logged.
        /// </summary>
        public DateTime ReportedOn { get; set; } = DateTime.Now;

        // Resolution state
        public bool Resolved { get; set; } = false;

        /// <summary>
        /// Date when the issue was resolved (shown in History).
        /// Null means not yet resolved.
        /// </summary>
        public DateTime? ResolvedOn { get; set; }

        // Optional links (cross-references)
        public Guid? RelatedTaskId { get; set; }
        public Guid? ApplianceId { get; set; }

        // Notes
        /// <summary>
        /// What the user tried before calling a professional.
        /// </summary>
        public string? AttemptedSteps { get; set; }

        /// <summary>
        /// How the issue was ultimately fixed (parts replaced, vendor called, etc.).
        /// </summary>
        public string? FixNotes { get; set; }

        // Optional metadata (helpful for filtering/search)
        /// <summary>
        /// Where the issue occurred (e.g., Kitchen, Bathroom, Pool Equipment Pad).
        /// </summary>
        public string? Location { get; set; }

        // ----- UI convenience (computed) -----

        /// <summary>
        /// Convenience text for XAML labels. Returns "Resolved: MMM d, yyyy" or empty.
        /// </summary>
        public string ResolvedDisplay =>
            Resolved && ResolvedOn.HasValue
                ? $"Resolved: {ResolvedOn.Value:MMM d, yyyy}"
                : string.Empty;
    }
}
