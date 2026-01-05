
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Maui.Storage;
using HomeMaintenanceApp.Models;
// Avoid ambiguity with System.Threading.Tasks.TaskStatus
using ModelsTaskStatus = HomeMaintenanceApp.Models.TaskStatus;

namespace HomeMaintenanceApp.Services
{
    /// <summary>
    /// Central in-memory manager for property, appliances, tasks, issues, checklists, and knowledge.
    /// Bind ObservableCollection<T> directly to your CollectionViews for live updates.
    /// </summary>
    public class MaintenanceManager
    {
        // ------------------------- Core collections --------------------------
        public ObservableCollection<MaintenanceTask> Tasks { get; } = new();
        public ObservableCollection<Appliance> Appliances { get; } = new();

        // Issues: Active + History (resolved)
        public ObservableCollection<IssueRecord> Issues { get; } = new();
        public ObservableCollection<IssueRecord> IssuesHistory { get; } = new();

        // ------------------------- Property ----------------------------------
        public Property? Property { get; set; }

        // ------------------------- Checklists --------------------------------
        public ObservableCollection<string> SpringChecklist { get; } = new();
        public ObservableCollection<string> SummerChecklist { get; } = new();
        public ObservableCollection<string> AutumnChecklist { get; } = new();
        public ObservableCollection<string> WinterChecklist { get; } = new();
        public ObservableCollection<string> HurricaneChecklist { get; } = new();
        public ObservableCollection<string> PoolChecklist { get; } = new();

        // ------------------------- Knowledge ---------------------------------
        public ObservableCollection<string> KnowledgeResources { get; } = new(); // user notes
        public IList<string> KnowledgeToolsView { get; } = new List<string>();   // seeded tools (read-only)
        public record UsefulLifeRow(string Item, string Life);
        public IList<UsefulLifeRow> KnowledgeUsefulLife { get; } = new List<UsefulLifeRow>(); // seeded (read-only)

        // ------------------------- Preferences keys --------------------------
        private const string PREF_KEY_SEASONAL = "SeasonalListsJson";
        private const string PREF_KEY_HURRICANE = "HurricaneListJson";
        private const string PREF_KEY_TASKS = "TasksJson";
        private const string PREF_KEY_PROPERTY = "PropertyJson";
        private const string PREF_KEY_KNOWLEDGE = "KnowledgeJson";       // user notes only
        private const string PREF_KEY_ISSUES = "IssuesJson";          // active issues
        private const string PREF_KEY_ISSUES_HISTORY = "IssuesHistoryJson";   // resolved issues

        // ------------------------- Scheduling constants/helpers --------------
        // Vinegar cadence: monthly on the 28th (you can change this day if you prefer)
        private const int VINEGAR_DAY_OF_MONTH = 28;

        public MaintenanceManager()
        {
            // Defaults
            Property = new Property { Address = "", HasPool = false, TrashDay = null };

            // Hydrate previously saved data
            LoadPropertyFromPreferences();
            LoadListsFromPreferences();
            LoadTasksFromPreferences();
            LoadKnowledgeFromPreferences();
            LoadIssuesFromPreferences();
            LoadIssuesHistoryFromPreferences();

            // ------------------ Seed demo appliance/tasks --------------------
            if (!Appliances.Any())
            {
                var hvac = new Appliance
                {
                    Name = "Carrier HVAC",
                    Type = ApplianceType.HVAC,
                    Location = "Attic",
                    SerialNumber = "ABC-123",
                    InstallDate = DateTime.Today.AddYears(-5)
                };
                Appliances.Add(hvac);

                // Initial demo task
                if (!Tasks.Any())
                {
                    Tasks.Add(new MaintenanceTask
                    {
                        Title = "Change HVAC Filter",
                        Description = "Replace with MERV-11 filter",
                        Priority = TaskPriority.Medium,
                        Status = ModelsTaskStatus.NotStarted,
                        DueDate = DateTime.Today.AddDays(7),
                        ApplianceId = hvac.Id,
                        Recurrence = TaskRecurrence.Monthly
                    });
                }
            }

            // ✅ Keep one sample issue for onboarding (only if empty)
            if (!Issues.Any() && !IssuesHistory.Any())
            {
                Issues.Add(new IssueRecord
                {
                    Title = "Sink Leak - Kitchen",
                    Description = "Slow drip under the sink",
                    Severity = IssueSeverity.Moderate,
                    ReportedOn = DateTime.Now,
                    Resolved = false,
                    ResolvedOn = null,
                    AttemptedSteps = "Checked P-trap and tightened fittings.",
                    FixNotes = "Replaced worn gasket; monitored for 48h."
                });
            }

            // ------------------ Seed Seasonal/Hurricane ----------------------
            if (!SpringChecklist.Any())
            {
                SpringChecklist.Add("Inspect deck & patio (loose boards/nails)");
                SpringChecklist.Add("Check dryer vent (lint build-up)");
                SpringChecklist.Add("Prune trees and shrubs");
                SpringChecklist.Add("Check window seals");
            }

            if (!SummerChecklist.Any())
            {
                SummerChecklist.Add("Clean gutters and downspouts");
                SummerChecklist.Add("Service air conditioning (filters/clean coils)");
                SummerChecklist.Add("Check outdoor faucets for leaks");
                SummerChecklist.Add("Fertilize lawn");
                SummerChecklist.Add("Cut grass to HOA standard");
            }

            if (!AutumnChecklist.Any())
            {
                AutumnChecklist.Add("Service heating system; replace filters");
                AutumnChecklist.Add("Clean gutters; rake leaves; winterize lawn");
                AutumnChecklist.Add("Inspect/clean chimney & flues");
                AutumnChecklist.Add("Shut off & insulate outdoor faucets/hoses");
            }

            if (!WinterChecklist.Any())
            {
                WinterChecklist.Add("Replace smoke & CO detector batteries");
                WinterChecklist.Add("Add insulation/cover windows if needed");
                WinterChecklist.Add("Review maintenance schedule for next year");
            }

            if (!HurricaneChecklist.Any())
            {
                HurricaneChecklist.Add("Stock water & non-perishable food");
                HurricaneChecklist.Add("Flashlights, batteries, first-aid kit");
                HurricaneChecklist.Add("Secure outdoor furniture & equipment");
                HurricaneChecklist.Add("Verify evacuation routes & contacts");
            }

            SeedMonthlyHousekeepingIfMissing();  // includes vinegar & paint touch-up
            SeedPoolChecklistIfMissing();

            // Knowledge seeds (tools)
            if (KnowledgeToolsView.Count == 0)
            {
                KnowledgeToolsView.Add("Flashlight and batteries");
                KnowledgeToolsView.Add("Flat-head and Phillips screwdrivers");
                KnowledgeToolsView.Add("Work gloves and safety goggles");
                KnowledgeToolsView.Add("Claw hammer");
                KnowledgeToolsView.Add("Metal rasp");
                KnowledgeToolsView.Add("Wire cutter");
                KnowledgeToolsView.Add("Plunger");
                KnowledgeToolsView.Add("Pliers");
                KnowledgeToolsView.Add("Sanding blocks and sandpaper");
                KnowledgeToolsView.Add("Adjustable wrench");
                KnowledgeToolsView.Add("Handsaw");
                KnowledgeToolsView.Add("Socket wrench set");
                KnowledgeToolsView.Add("Nails, screws and bolts");
            }

            // Knowledge seeds (expected useful life)
            if (KnowledgeUsefulLife.Count == 0)
            {
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Clothes washer or dryer", "≈ 10 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Water heater", "≈ 11–14 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Furnace", "≈ 18 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Furnace w/ heat pump", "≈ 15 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Central air conditioner", "≈ 15 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Humidifier", "≈ 8 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Dishwasher", "≈ 10 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Range / Oven", "≈ 18–20 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Refrigerator", "≈ 14–19 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Freezer", "≈ 16 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Garbage disposal", "≈ 10 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Interior paint", "≈ 5–10 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Exterior paint", "≈ 7–10 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Wallpaper", "≈ 7 years"));
                KnowledgeUsefulLife.Add(new UsefulLifeRow("Carpeting", "≈ 5 years"));
            }

            // Persist seeds
            SavePropertyToPreferences();
            SaveListsToPreferences();
            SaveTasksToPreferences();
            SaveKnowledgeToPreferences();
            SaveIssuesToPreferences();
            SaveIssuesHistoryToPreferences();
        }

        // -------------------------- Tasks CRUD + Scheduling ------------------
        public void AddTask(MaintenanceTask task)
        {
            // If recurrence & no due date set, choose a sensible default
            if (task.DueDate is null && task.Recurrence != TaskRecurrence.None)
            {
                task.DueDate = NextDueFrom(DateTime.Today, task.Recurrence, monthlyDayOfMonth: VINEGAR_DAY_OF_MONTH);
            }

            Tasks.Add(task);
            SaveTasksToPreferences();
        }

        public void StartTask(Guid id)
        {
            var t = Tasks.FirstOrDefault(x => x.Id == id);
            if (t is null) return;

            t.Status = ModelsTaskStatus.InProgress;
            SaveTasksToPreferences();
        }

        public void UpdateTask(MaintenanceTask updated)
        {
            var existing = Tasks.FirstOrDefault(t => t.Id == updated.Id);
            if (existing is null) return;

            existing.Title = updated.Title;
            existing.Description = updated.Description;
            existing.Priority = updated.Priority;
            existing.Status = updated.Status;
            existing.DueDate = updated.DueDate;
            existing.Recurrence = updated.Recurrence;
            existing.LastCompletedOn = updated.LastCompletedOn;
            existing.ApplianceId = updated.ApplianceId;

            SaveTasksToPreferences();
        }

        public void DeleteTask(Guid id)
        {
            var existing = Tasks.FirstOrDefault(t => t.Id == id);
            if (existing is null) return;
            Tasks.Remove(existing);
            SaveTasksToPreferences();
        }

        /// <summary>
        /// Marks task Completed (stamps today's date) and auto-reschedules based on Recurrence.
        /// </summary>
        public void CompleteTask(Guid id)
        {
            var t = Tasks.FirstOrDefault(x => x.Id == id);
            if (t is null) return;

            t.Status = ModelsTaskStatus.Completed;
            t.LastCompletedOn = DateTime.Today;

            if (t.Recurrence != TaskRecurrence.None)
            {
                // Compute next due based on the recurrence and a stable monthly day-of-month
                var anchorDay = t.DueDate?.Day ?? VINEGAR_DAY_OF_MONTH; // keep same day-of-month if possible
                t.DueDate = NextDueFrom(DateTime.Today, t.Recurrence, monthlyDayOfMonth: anchorDay);

                // Return task to "NotStarted" so it's ready to do again
                t.Status = ModelsTaskStatus.NotStarted;
            }

            SaveTasksToPreferences();
        }

        public MaintenanceTask? GetTask(Guid id) => Tasks.FirstOrDefault(t => t.Id == id);

        // -------------------------- Issues + History -------------------------
        public void AddIssue(IssueRecord issue)
        {
            Issues.Add(issue);
            SaveIssuesToPreferences();
        }

        public void UpdateIssue(IssueRecord updated)
        {
            var existing = Issues.FirstOrDefault(i => i.Id == updated.Id)
                        ?? IssuesHistory.FirstOrDefault(i => i.Id == updated.Id);
            if (existing is null) return;

            existing.Title = updated.Title;
            existing.Description = updated.Description;
            existing.Severity = updated.Severity;
            existing.Resolved = updated.Resolved;
            existing.ResolvedOn = updated.ResolvedOn;
            existing.AttemptedSteps = updated.AttemptedSteps;
            existing.FixNotes = updated.FixNotes;
            existing.ApplianceId = updated.ApplianceId;
            existing.RelatedTaskId = updated.RelatedTaskId;

            SaveIssuesToPreferences();
            SaveIssuesHistoryToPreferences();
        }

        public void DeleteIssue(Guid id)
        {
            var existing = Issues.FirstOrDefault(i => i.Id == id);
            if (existing is not null)
            {
                Issues.Remove(existing);
                SaveIssuesToPreferences();
                return;
            }

            var hist = IssuesHistory.FirstOrDefault(i => i.Id == id);
            if (hist is not null)
            {
                IssuesHistory.Remove(hist);
                SaveIssuesHistoryToPreferences();
            }
        }

        public IssueRecord? GetIssue(Guid id)
            => Issues.FirstOrDefault(i => i.Id == id) ?? IssuesHistory.FirstOrDefault(i => i.Id == id);

        /// <summary>
        /// Mark resolved (stamp date) and move from Active Issues to IssuesHistory.
        /// </summary>
        public void ResolveIssue(Guid id)
        {
            var issue = Issues.FirstOrDefault(i => i.Id == id);
            if (issue is null) return;

            issue.Resolved = true;
            issue.ResolvedOn = DateTime.Today;

            Issues.Remove(issue);
            IssuesHistory.Add(issue);

            SaveIssuesToPreferences();
            SaveIssuesHistoryToPreferences();
        }

        // -------------------------- Appliances -------------------------------
        public void AddAppliance(Appliance appliance) => Appliances.Add(appliance);
        public Appliance? GetAppliance(Guid id) => Appliances.FirstOrDefault(a => a.Id == id);

        // -------------------------- Property ops -----------------------------
        public void SetPropertyAddress(string address)
        {
            if (Property is null) Property = new Property();
            Property.Address = address ?? string.Empty;
            SavePropertyToPreferences();
        }

        public void SetHasPool(bool hasPool)
        {
            if (Property is null) Property = new Property();
            var wasPool = Property.HasPool;
            Property.HasPool = hasPool;

            if (!wasPool && hasPool) SeedPoolTasks();

            SavePropertyToPreferences();
        }

        public void SetTrashDay(DayOfWeek? day)
        {
            if (Property is null) Property = new Property();
            Property.TrashDay = day;
            SavePropertyToPreferences();
        }

        private void SeedPoolTasks()
        {
            bool alreadySeeded = Tasks.Any(t => t.Title.StartsWith("Pool:", StringComparison.OrdinalIgnoreCase));
            if (alreadySeeded) return;

            AddTask(new MaintenanceTask
            {
                Title = "Pool: Weekly – skim surface, brush walls, empty baskets",
                Priority = TaskPriority.Low,
                Status = ModelsTaskStatus.NotStarted,
                DueDate = NextDueFrom(DateTime.Today, TaskRecurrence.Weekly),
                Recurrence = TaskRecurrence.Weekly
            });

            AddTask(new MaintenanceTask
            {
                Title = "Pool: Weekly – test chlorine & pH; adjust as needed",
                Priority = TaskPriority.Low,
                Status = ModelsTaskStatus.NotStarted,
                DueDate = NextDueFrom(DateTime.Today, TaskRecurrence.Weekly),
                Recurrence = TaskRecurrence.Weekly
            });

            AddTask(new MaintenanceTask
            {
                Title = "Pool: Monthly – inspect pump & filter; backwash/clean",
                Priority = TaskPriority.Medium,
                Status = ModelsTaskStatus.NotStarted,
                DueDate = NextDueFrom(DateTime.Today, TaskRecurrence.Monthly, monthlyDayOfMonth: VINEGAR_DAY_OF_MONTH),
                Recurrence = TaskRecurrence.Monthly
            });
        }

        private void SeedMonthlyHousekeepingIfMissing()
        {
            bool vinegarPresent = Tasks.Any(t => t.Title.Contains("vinegar", StringComparison.OrdinalIgnoreCase));
            bool paintPresent = Tasks.Any(t => t.Title.Contains("paint", StringComparison.OrdinalIgnoreCase));

            if (!vinegarPresent)
            {
                AddTask(new MaintenanceTask
                {
                    Title = "A/C: Add 1/3 cup vinegar to condensate line",
                    Description = "Pour into condensate drain to inhibit algae buildup",
                    Priority = TaskPriority.Low,
                    Status = ModelsTaskStatus.NotStarted,
                    DueDate = NextDueFrom(DateTime.Today, TaskRecurrence.Monthly, monthlyDayOfMonth: VINEGAR_DAY_OF_MONTH),
                    Recurrence = TaskRecurrence.Monthly
                });
            }

            if (!paintPresent)
            {
                AddTask(new MaintenanceTask
                {
                    Title = "Touch up paint chips",
                    Description = "Inspect high-traffic areas and exterior trim",
                    Priority = TaskPriority.Low,
                    Status = ModelsTaskStatus.NotStarted,
                    DueDate = NextDueFrom(DateTime.Today, TaskRecurrence.Monthly, monthlyDayOfMonth: DateTime.Today.Day),
                    Recurrence = TaskRecurrence.Monthly
                });
            }
        }

        private void SeedPoolChecklistIfMissing()
        {
            if (PoolChecklist.Any()) return;

            PoolChecklist.Add("Skim surface & empty skimmer baskets");
            PoolChecklist.Add("Brush walls, steps, and tile line");
            PoolChecklist.Add("Test chlorine & pH; adjust as needed");
            PoolChecklist.Add("Check pump basket & pressure gauge");
            PoolChecklist.Add("Backwash or clean filter (as required)");
            PoolChecklist.Add("Top off water level");
            PoolChecklist.Add("Inspect equipment (pump/heater/valves) for leaks");
        }

        // -------------------------- Persistence: Lists -----------------------
        public void LoadListsFromPreferences()
        {
            try
            {
                var seasonalJson = Preferences.Get(PREF_KEY_SEASONAL, string.Empty);
                if (!string.IsNullOrWhiteSpace(seasonalJson))
                {
                    var bag = JsonSerializer.Deserialize<SeasonalBag>(seasonalJson) ?? new SeasonalBag();

                    ReplaceList(SpringChecklist, bag.Spring ?? new List<string>());
                    ReplaceList(SummerChecklist, bag.Summer ?? new List<string>());
                    ReplaceList(AutumnChecklist, bag.Autumn ?? new List<string>());
                    ReplaceList(WinterChecklist, bag.Winter ?? new List<string>());
                    ReplaceList(PoolChecklist, bag.Pool ?? new List<string>());
                }

                var hurricaneJson = Preferences.Get(PREF_KEY_HURRICANE, string.Empty);
                if (!string.IsNullOrWhiteSpace(hurricaneJson))
                {
                    var h = JsonSerializer.Deserialize<List<string>>(hurricaneJson) ?? new List<string>();
                    ReplaceList(HurricaneChecklist, h);
                }
            }
            catch { /* swallow */ }
        }

        public void SaveListsToPreferences()
        {
            try
            {
                var bag = new SeasonalBag
                {
                    Spring = SpringChecklist.ToList(),
                    Summer = SummerChecklist.ToList(),
                    Autumn = AutumnChecklist.ToList(),
                    Winter = WinterChecklist.ToList(),
                    Pool = PoolChecklist.ToList()
                };
                Preferences.Set(PREF_KEY_SEASONAL, JsonSerializer.Serialize(bag));
                Preferences.Set(PREF_KEY_HURRICANE, JsonSerializer.Serialize(HurricaneChecklist.ToList()));
            }
            catch { /* swallow */ }
        }

        private static void ReplaceList(ObservableCollection<string> target, IEnumerable<string> src)
        {
            target.Clear();
            foreach (var s in src) target.Add(s);
        }

        private class SeasonalBag
        {
            public List<string>? Spring { get; set; }
            public List<string>? Summer { get; set; }
            public List<string>? Autumn { get; set; }
            public List<string>? Winter { get; set; }
            public List<string>? Pool { get; set; }
        }

        // -------------------------- Persistence: Tasks -----------------------
        public void LoadTasksFromPreferences()
        {
            try
            {
                var json = Preferences.Get(PREF_KEY_TASKS, string.Empty);
                if (string.IsNullOrWhiteSpace(json)) return;

                var list = JsonSerializer.Deserialize<List<MaintenanceTask>>(json) ?? new List<MaintenanceTask>();
                Tasks.Clear();
                foreach (var t in list) Tasks.Add(t);
            }
            catch { /* swallow */ }
        }

        public void SaveTasksToPreferences()
        {
            try
            {
                var list = Tasks.ToList();
                var json = JsonSerializer.Serialize(list);
                Preferences.Set(PREF_KEY_TASKS, json);
            }
            catch { /* swallow */ }
        }

        // -------------------------- Persistence: Property --------------------
        public void LoadPropertyFromPreferences()
        {
            try
            {
                var json = Preferences.Get(PREF_KEY_PROPERTY, string.Empty);
                if (string.IsNullOrWhiteSpace(json)) return;

                var p = JsonSerializer.Deserialize<Property>(json);
                if (p is not null) Property = p;
            }
            catch { /* swallow */ }
        }

        public void SavePropertyToPreferences()
        {
            try
            {
                var json = JsonSerializer.Serialize(Property);
                Preferences.Set(PREF_KEY_PROPERTY, json);
            }
            catch { /* swallow */ }
        }

        // -------------------------- Persistence: Knowledge -------------------
        public void LoadKnowledgeFromPreferences()
        {
            try
            {
                var json = Preferences.Get(PREF_KEY_KNOWLEDGE, string.Empty);
                if (string.IsNullOrWhiteSpace(json)) return;

                var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                KnowledgeResources.Clear();
                foreach (var item in list) KnowledgeResources.Add(item);
            }
            catch { /* swallow */ }
        }

        public void SaveKnowledgeToPreferences()
        {
            try
            {
                var list = KnowledgeResources.ToList();
                var json = JsonSerializer.Serialize(list);
                Preferences.Set(PREF_KEY_KNOWLEDGE, json);
            }
            catch { /* swallow */ }
        }

        // -------------------------- Persistence: Issues ----------------------
        public void LoadIssuesFromPreferences()
        {
            try
            {
                var json = Preferences.Get(PREF_KEY_ISSUES, string.Empty);
                if (string.IsNullOrWhiteSpace(json)) return;

                var list = JsonSerializer.Deserialize<List<IssueRecord>>(json) ?? new List<IssueRecord>();
                Issues.Clear();
                foreach (var i in list) Issues.Add(i);
            }
            catch { /* swallow */ }
        }

        public void SaveIssuesToPreferences()
        {
            try
            {
                var list = Issues.ToList();
                var json = JsonSerializer.Serialize(list);
                Preferences.Set(PREF_KEY_ISSUES, json);
            }
            catch { /* swallow */ }
        }

        public void LoadIssuesHistoryFromPreferences()
        {
            try
            {
                var json = Preferences.Get(PREF_KEY_ISSUES_HISTORY, string.Empty);
                if (string.IsNullOrWhiteSpace(json)) return;

                var list = JsonSerializer.Deserialize<List<IssueRecord>>(json) ?? new List<IssueRecord>();
                IssuesHistory.Clear();
                foreach (var i in list) IssuesHistory.Add(i);
            }
            catch { /* swallow */ }
        }

        public void SaveIssuesHistoryToPreferences()
        {
            try
            {
                var list = IssuesHistory.ToList();
                var json = JsonSerializer.Serialize(list);
                Preferences.Set(PREF_KEY_ISSUES_HISTORY, json);
            }
            catch { /* swallow */ }
        }

        // ====================================================================
        // ========================= RESET HELPERS =============================
        // ====================================================================

        /// <summary>
        /// Clears the Property information only (Address, HasPool, TrashDay) and saves.
        /// Use this to remove a test address on your device without touching other data.
        /// </summary>
        public void ResetAllData()
        {
            Property = new Property { Address = "", HasPool = false, TrashDay = null };
            SavePropertyToPreferences();

            try { Preferences.Remove(PREF_KEY_PROPERTY); } catch { /* swallow */ }
            SavePropertyToPreferences();
        }

        /// <summary>
        /// Clears ALL locally persisted app data on the device (Tasks, Issues + History,
        /// Seasonal/Pool/Hurricane lists, Knowledge notes, and Property).
        /// </summary>
        public void ResetEverything()
        {
            // Property
            Property = new Property { Address = "", HasPool = false, TrashDay = null };
            SavePropertyToPreferences();

            // Tasks
            Tasks.Clear();
            SaveTasksToPreferences();
            try { Preferences.Remove(PREF_KEY_TASKS); } catch { /* swallow */ }

            // Issues & History
            Issues.Clear();
            SaveIssuesToPreferences();
            IssuesHistory.Clear();
            SaveIssuesHistoryToPreferences();

            // Seasonal + Pool + Hurricane lists
            SpringChecklist.Clear();
            SummerChecklist.Clear();
            AutumnChecklist.Clear();
            WinterChecklist.Clear();
            PoolChecklist.Clear();
            HurricaneChecklist.Clear();
            SaveListsToPreferences();
            try { Preferences.Remove(PREF_KEY_SEASONAL); } catch { /* swallow */ }
            try { Preferences.Remove(PREF_KEY_HURRICANE); } catch { /* swallow */ }

            // Knowledge (user notes only; seeded tools/expected life are not persisted)
            KnowledgeResources.Clear();
            SaveKnowledgeToPreferences();
            try { Preferences.Remove(PREF_KEY_KNOWLEDGE); } catch { /* swallow */ }
        }

        // ====================================================================
        // ===================== SCHEDULING UTILITIES ==========================
        // ====================================================================

        /// <summary>
        /// Compute the next due date from a given "from" date for a recurrence.
        /// If monthly, you can pass a desired day-of-month (e.g., 28 for vinegar).
        /// </summary>
        public static DateTime NextDueFrom(DateTime from, TaskRecurrence recurrence, int? monthlyDayOfMonth = null)
        {
            from = from.Date;
            return recurrence switch
            {
                TaskRecurrence.Weekly => from.AddDays(7),
                TaskRecurrence.Monthly => NextMonthlyOnOrAfter(from, monthlyDayOfMonth ?? 28),
                TaskRecurrence.Yearly => from.AddYears(1),
                _ => from
            };
        }

        /// <summary>
        /// Next date on or after 'from' that falls on 'dayOfMonth', clamped to the last day if needed.
        /// Example: from=Feb 3, day=28 => Feb 28; from=Feb 29, day=28 => Mar 28; from=Feb 27, day=30 => Feb 28, etc.
        /// </summary>
        public static DateTime NextMonthlyOnOrAfter(DateTime from, int dayOfMonth)
        {
            var target = DateForMonth(from.Year, from.Month, dayOfMonth);
            if (from <= target) return target;

            var nextMonth = from.AddMonths(1);
            return DateForMonth(nextMonth.Year, nextMonth.Month, dayOfMonth);
        }

        private static DateTime DateForMonth(int year, int month, int dayOfMonth)
        {
            int days = DateTime.DaysInMonth(year, month);
            int day = Math.Min(Math.Max(1, dayOfMonth), days);
            return new DateTime(year, month, day);
        }
    }
}
