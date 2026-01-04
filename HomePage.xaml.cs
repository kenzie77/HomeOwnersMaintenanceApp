
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
        /// </summary>
        private void LoadDueSoon()
        {
            var items = _manager.Tasks
                .Where(t => t.DueDate.HasValue && t.Status != ModelsTaskStatus.Completed)
                .OrderBy(t => t.DueDate)
                .Take(10)
                .Select(t => new DueSoonItem
                {
                    Title = t.Title,
                    DueTag = BuildDueTag(t.DueDate!.Value),
                    DueColor = DueColor(t.DueDate!.Value)
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
