using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HomeMaintenanceApp.Services;
using HomeMaintenanceApp.Models;
// Alias to avoid ambiguity with System.Threading.Tasks.TaskStatus
using ModelsTaskStatus = HomeMaintenanceApp.Models.TaskStatus;

namespace HomeMaintenanceApp.Pages
{
    public partial class HomePage : ContentPage
    {
        private readonly MaintenanceManager _manager;

        public HomePage()
        {
            InitializeComponent();

            // Local instance (simpler, no DI)
            _manager = new MaintenanceManager();

            // Hydrate any persisted data
            _manager.LoadPropertyFromPreferences();
            _manager.LoadListsFromPreferences();
            _manager.LoadTasksFromPreferences();

            // Initial UI updates
            UpdatePropertyDisplay();
            RefreshKpis();
            LoadDueSoon();
        }

        /// <summary>
        /// Updates Address, Pool badge visibility, and Trash Day info line.
        /// </summary>
        private void UpdatePropertyDisplay()
        {
            // Address
            AddressLabel.Text = string.IsNullOrWhiteSpace(_manager.Property?.Address)
                ? "No address set"
                : _manager.Property!.Address;

            // Pool badge
            PoolBadge.IsVisible = _manager.Property?.HasPool == true;

            // Trash day + next pickup
            var trashDay = _manager.Property?.TrashDay;
            if (trashDay is null)
            {
                TrashInfoLabel.Text = "Trash day: Not set";
            }
            else
            {
                var next = NextPickupDate(DateTime.Today, trashDay.Value);
                TrashInfoLabel.Text = $"Trash day: {trashDay} • Next pickup: {next:ddd, MMM d}";
            }
        }

        /// <summary>
        /// Computes KPI counts and updates labels.
        /// </summary>
        private void RefreshKpis()
        {
            var active = _manager.Tasks.Count(t => t.Status != ModelsTaskStatus.Completed);

            var overdue = _manager.Tasks.Count(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value.Date < DateTime.Today &&
                t.Status != ModelsTaskStatus.Completed);

            var criticalAppliances = _manager.Issues.Count(i =>
                i.Severity == IssueSeverity.Critical &&
                i.ApplianceId.HasValue);

            var issues = _manager.Issues.Count();

            ActiveCount.Text = active.ToString();
            OverdueCount.Text = overdue.ToString();
            CriticalCount.Text = criticalAppliances.ToString();
            IssuesCount.Text = issues.ToString();
        }

        /// <summary>
        /// Builds and binds the "Due soon" items.
        /// Fixes:
        ///  - HVAC Filter due date matches vinegar/condensate task
        ///  - Removes duplicates like "New Task" showing twice
        ///  - Hides "paint chips" from Home screen list
        ///  - Ensures "Due today" shows on the correct day
        /// </summary>
        private void LoadDueSoon()
        {
            // --- Find the vinegar/condensate task due date (used to sync HVAC filter) ---
            DateTime? vinegarDue = _manager.Tasks
                .Where(t => t.Status != ModelsTaskStatus.Completed && t.DueDate.HasValue)
                .Where(t =>
                    !string.IsNullOrWhiteSpace(t.Title) &&
                    (t.Title.Contains("vinegar", StringComparison.OrdinalIgnoreCase) ||
                     t.Title.Contains("condensate", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(t => t.DueDate)
                .Select(t => t.DueDate!.Value.Date)
                .FirstOrDefault();

            // Local function: compute the due date we want to SHOW on Home (doesn't change stored data)
            DateTime? EffectiveDueDate(HomeMaintenanceApp.Models.MaintenanceTask t)
            {
                if (!t.DueDate.HasValue) return null;

                // If this is the HVAC filter task and we have a vinegar due date, use it
                if (vinegarDue.HasValue &&
                    !string.IsNullOrWhiteSpace(t.Title) &&
                    (t.Title.Contains("hvac filter", StringComparison.OrdinalIgnoreCase) ||
                     t.Title.Contains("change hvac filter", StringComparison.OrdinalIgnoreCase) ||
                     t.Title.Contains("air filter", StringComparison.OrdinalIgnoreCase)))
                {
                    return vinegarDue.Value;
                }

                return t.DueDate.Value.Date;
            }

            var items = _manager.Tasks
                // only tasks with due dates that aren't completed
                .Where(t => t.Status != ModelsTaskStatus.Completed)
                .Select(t => new
                {
                    Task = t,
                    Due = EffectiveDueDate(t)
                })
                .Where(x => x.Due.HasValue)

                // --- Remove "paint chips" from the Home screen list ---
                .Where(x => !(x.Task.Title ?? "")
                    .Contains("paint chip", StringComparison.OrdinalIgnoreCase))

                // --- Remove duplicates (Title + DueDate) e.g., "New Task" showing twice ---
                .GroupBy(x => new
                {
                    Title = (x.Task.Title ?? "").Trim().ToLowerInvariant(),
                    DueDate = x.Due!.Value.Date
                })
                .Select(g => g.First())

                // sort + take top 10
                .OrderBy(x => x.Due)
                .Take(10)

                // build DTO for binding
                .Select(x => new DueSoonItem
                {
                    Title = x.Task.Title,
                    DueTag = BuildDueTag(x.Due!.Value),
                    DueColor = DueColor(x.Due!.Value)
                })
                .ToList();

            DueSoonView.ItemsSource = items;
        }

        /// <summary>
        /// Returns a friendly tag for the due date (Overdue, Today, or a date).
        /// </summary>
        private static string BuildDueTag(DateTime due)
        {
            if (due.Date < DateTime.Today) return $"Overdue: Due {due:MM/dd/yyyy}";
            if (due.Date == DateTime.Today) return "Due today";
            return $"Due {due:MM/dd/yyyy}";
        }

        /// <summary>
        /// Returns a color for the due tag based on urgency.
        /// </summary>
        private static Color DueColor(DateTime due)
        {
            if (due.Date < DateTime.Today) return Colors.OrangeRed;
            if (due.Date == DateTime.Today) return Colors.Gold;
            return Colors.MediumSpringGreen;
        }

        /// <summary>
        /// Computes the next pickup date from today and the selected trash day.
        /// If today equals the trash day, we show next week’s date.
        /// </summary>
        private static DateTime NextPickupDate(DateTime today, DayOfWeek trashDay)
        {
            int diff = ((int)trashDay - (int)today.DayOfWeek + 7) % 7;
            return today.AddDays(diff == 0 ? 7 : diff);
        }

        /// <summary>
        /// Keep KPIs and due-soon fresh when returning to Home.
        /// </summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            _manager.LoadPropertyFromPreferences();
            _manager.LoadTasksFromPreferences();     // in case tasks changed elsewhere
            _manager.LoadListsFromPreferences();     // optional refresh

            UpdatePropertyDisplay();
            RefreshKpis();
            LoadDueSoon();
        }

        /// <summary>
        /// Lightweight DTO for the Due Soon list binding.
        /// </summary>
        private class DueSoonItem
        {
            public string Title { get; set; } = "";
            public string DueTag { get; set; } = "";
            public Color DueColor { get; set; } = Colors.White;
        }
    }
}