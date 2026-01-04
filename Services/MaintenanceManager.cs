
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
    /// Central in-memory manager for property, appliances, tasks, issues, and checklists.
    /// Bind ObservableCollection<T> directly to your CollectionViews for live updates.
    /// </summary>
    public class MaintenanceManager
    {
        // ------------------------- Core collections --------------------------
        public ObservableCollection<MaintenanceTask> Tasks { get; } = new();
        public ObservableCollection<Appliance> Appliances { get; } = new();
        public ObservableCollection<IssueRecord> Issues { get; } = new();

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
        public IList<string> KnowledgeToolsView { get; } = new List<string>();   // seeded
        public record UsefulLifeRow(string Item, string Life);
        public IList<UsefulLifeRow> KnowledgeUsefulLife { get; } = new List<UsefulLifeRow>(); // seeded

        // ------------------------- Preferences keys --------------------------
        private const string PREF_KEY_SEASONAL = "SeasonalListsJson";
        private const string PREF_KEY_HURRICANE = "HurricaneListJson";
        private const string PREF_KEY_TASKS = "TasksJson";
        private const string PREF_KEY_PROPERTY = "PropertyJson";
        private const string PREF_KEY_KNOWLEDGE = "KnowledgeJson";   // user notes
        private const string PREF_KEY_ISSUES = "IssuesJson";

        public MaintenanceManager()
        {
            // Initialize defaults
            Property = new Property { Address = "", HasPool = false, TrashDay = null };

            // Hydrate from Preferences
            LoadPropertyFromPreferences();
            LoadListsFromPreferences();
            LoadTasksFromPreferences();
            LoadKnowledgeFromPreferences(); // user notes
            LoadIssuesFromPreferences();

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

            if (!Issues.Any())
            {
                Issues.Add(new IssueRecord
                {
                    Title = "Sink Leak - Kitchen",
                    Description = "Slow drip under the sink",
                    Severity = IssueSeverity.Moderate,
                    ReportedOn = DateTime.Now,
                    Resolved = false,
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

            // Monthly housekeeping
            SeedMonthlyHousekeepingIfMissing();

            // Pool (Seasonal tab)
            SeedPoolChecklistIfMissing();

            // ------------------ Knowledge: Basic Tools (seeded view) ----------
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

            // ------------------ Knowledge: Expected Useful Life (seeded view) --
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

            // Persist seeds so they remain next launch
            SavePropertyToPreferences();
            SaveListsToPreferences();
            SaveTasksToPreferences();
            SaveKnowledgeToPreferences();   // user notes
            SaveIssuesToPreferences();
        }

        // -------------------------- Tasks CRUD -------------------------------
        public void AddTask(MaintenanceTask task)
        {
            Tasks.Add(task);
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

        public void CompleteTask(Guid id)
        {
            var t = Tasks.FirstOrDefault(x => x.Id == id);
            if (t is null) return;

            t.Status = ModelsTaskStatus.Completed;
            t.LastCompletedOn = DateTime.Today;

            if (t.Recurrence != TaskRecurrence.None)
            {
                DateTime baseDate = t.DueDate ?? DateTime.Today;
                DateTime nextDue = t.Recurrence switch
                {
                    TaskRecurrence.Weekly => baseDate.AddDays(7),
                    TaskRecurrence.Monthly => baseDate.AddMonths(1),
                    TaskRecurrence.Yearly => baseDate.AddYears(1),
                    _ => baseDate
                };

                t.Status = ModelsTaskStatus.NotStarted;
                t.DueDate = nextDue;
            }

            SaveTasksToPreferences();
        }

        public MaintenanceTask? GetTask(Guid id) => Tasks.FirstOrDefault(t => t.Id == id);
        public ObservableCollection<MaintenanceTask> GetTasksByStatus(ModelsTaskStatus status)
            => new(Tasks.Where(t => t.Status == status));

        // -------------------------- Issues CRUD ------------------------------
        public void AddIssue(IssueRecord issue)
        {
            Issues.Add(issue);
            SaveIssuesToPreferences();
        }

        public void UpdateIssue(IssueRecord updated)
        {
            var existing = Issues.FirstOrDefault(i => i.Id == updated.Id);
            if (existing is null) return;

            existing.Title = updated.Title;
            existing.Description = updated.Description;
            existing.Severity = updated.Severity;
            existing.Resolved = updated.Resolved;
            existing.AttemptedSteps = updated.AttemptedSteps;
            existing.FixNotes = updated.FixNotes;
            existing.ApplianceId = updated.ApplianceId;
            existing.RelatedTaskId = updated.RelatedTaskId;

            SaveIssuesToPreferences();
        }

        public void DeleteIssue(Guid id)
        {
            var existing = Issues.FirstOrDefault(i => i.Id == id);
            if (existing is null) return;
            Issues.Remove(existing);
            SaveIssuesToPreferences();
        }

        public IssueRecord? GetIssue(Guid id) => Issues.FirstOrDefault(i => i.Id == id);

        // -------------------------- Appliances CRUD --------------------------
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
                DueDate = DateTime.Today.AddDays(7),
                Recurrence = TaskRecurrence.Weekly
            });

            AddTask(new MaintenanceTask
            {
                Title = "Pool: Weekly – test chlorine & pH; adjust as needed",
                Priority = TaskPriority.Low,
                Status = ModelsTaskStatus.NotStarted,
                DueDate = DateTime.Today.AddDays(7),
                Recurrence = TaskRecurrence.Weekly
            });

            AddTask(new MaintenanceTask
            {
                Title = "Pool: Monthly – inspect pump & filter; backwash/clean",
                Priority = TaskPriority.Medium,
                Status = ModelsTaskStatus.NotStarted,
                DueDate = DateTime.Today.AddMonths(1),
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
                    DueDate = DateTime.Today.AddMonths(1),
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
                    DueDate = DateTime.Today.AddMonths(1),
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
    }
}
