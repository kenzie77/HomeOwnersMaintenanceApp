using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HomeMaintenanceApp.Services;
using HomeMaintenanceApp.Models;
using ModelsTaskStatus = HomeMaintenanceApp.Models.TaskStatus;

namespace HomeMaintenanceApp.Pages
{
    public partial class HomePage : ContentPage
    {
        private readonly MaintenanceManager _manager;

        public HomePage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            _manager.LoadPropertyFromPreferences();
            _manager.LoadListsFromPreferences();
            _manager.LoadTasksFromPreferences();

            UpdatePropertyDisplay();
            RefreshKpis();
            LoadDueSoon();
        }

        // ? THIS FIXES YOUR ERROR (must match XAML Clicked="OnSetupClicked")
        private async void OnSetupClicked(object sender, EventArgs e)
        {
            // AppShell route is Route="Onboarding"
            await Shell.Current.GoToAsync("//Onboarding");
        }

        private void UpdatePropertyDisplay()
        {
            var address = _manager.Property?.Address;

            AddressLabel.Text = string.IsNullOrWhiteSpace(address)
                ? "No address set"
                : address;

            // Show Setup button only if missing address
            SetupButton.IsVisible = string.IsNullOrWhiteSpace(address);

            // Pool badge only if HasPool true
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

        private void LoadDueSoon()
        {
            var hasPool = _manager.Property?.HasPool == true;

            DateTime? vinegarDue = _manager.Tasks
                .Where(t => t.Status != ModelsTaskStatus.Completed && t.DueDate.HasValue)
                .Where(t =>
                    !string.IsNullOrWhiteSpace(t.Title) &&
                    (t.Title.Contains("vinegar", StringComparison.OrdinalIgnoreCase) ||
                     t.Title.Contains("condensate", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(t => t.DueDate)
                .Select(t => t.DueDate!.Value.Date)
                .FirstOrDefault();

            DateTime? EffectiveDueDate(HomeMaintenanceApp.Models.MaintenanceTask t)
            {
                if (!t.DueDate.HasValue) return null;

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

            bool IsPoolRelated(string? title)
            {
                if (string.IsNullOrWhiteSpace(title)) return false;

                return title.Contains("pool", StringComparison.OrdinalIgnoreCase) ||
                       title.Contains("skimmer", StringComparison.OrdinalIgnoreCase) ||
                       title.Contains("chlorine", StringComparison.OrdinalIgnoreCase) ||
                       title.Contains("pump", StringComparison.OrdinalIgnoreCase) ||
                       title.Contains("backwash", StringComparison.OrdinalIgnoreCase);
            }

            var items = _manager.Tasks
                .Where(t => t.Status != ModelsTaskStatus.Completed)
                .Select(t => new { Task = t, Due = EffectiveDueDate(t) })
                .Where(x => x.Due.HasValue)

                // Hide paint chips from home list
                .Where(x => !(x.Task.Title ?? "").Contains("paint chip", StringComparison.OrdinalIgnoreCase))

                // Hide pool tasks unless user has pool
                .Where(x => hasPool || !IsPoolRelated(x.Task.Title))

                // Remove duplicates by Title + DueDate
                .GroupBy(x => new
                {
                    Title = (x.Task.Title ?? "").Trim().ToLowerInvariant(),
                    DueDate = x.Due!.Value.Date
                })
                .Select(g => g.First())

                .OrderBy(x => x.Due)
                .Take(10)
                .Select(x => new DueSoonItem
                {
                    Title = x.Task.Title,
                    DueTag = BuildDueTag(x.Due!.Value),
                    DueColor = DueColor(x.Due!.Value)
                })
                .ToList();

            DueSoonView.ItemsSource = items;
        }

        private static string BuildDueTag(DateTime due)
        {
            if (due.Date < DateTime.Today) return $"Overdue: Due {due:MM/dd/yyyy}";
            if (due.Date == DateTime.Today) return "Due today";
            return $"Due {due:MM/dd/yyyy}";
        }

        private static Color DueColor(DateTime due)
        {
            if (due.Date < DateTime.Today) return Colors.OrangeRed;
            if (due.Date == DateTime.Today) return Colors.Gold;
            return Colors.MediumSpringGreen;
        }

        private static DateTime NextPickupDate(DateTime today, DayOfWeek trashDay)
        {
            int diff = ((int)trashDay - (int)today.DayOfWeek + 7) % 7;
            return today.AddDays(diff == 0 ? 7 : diff);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _manager.LoadPropertyFromPreferences();
            _manager.LoadTasksFromPreferences();
            _manager.LoadListsFromPreferences();

            UpdatePropertyDisplay();
            RefreshKpis();
            LoadDueSoon();
        }

        private class DueSoonItem
        {
            public string Title { get; set; } = "";
            public string DueTag { get; set; } = "";
            public Color DueColor { get; set; } = Colors.White;
        }
    }
}