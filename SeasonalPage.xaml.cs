
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using HomeMaintenanceApp.Services;

namespace HomeMaintenanceApp.Pages
{
    // Row view-model so we can bind Text + Done + CompletedOn + Select
    public class SeasonRowItem : INotifyPropertyChanged
    {
        private string _text = "";
        private bool _isDone;
        private DateTime? _completedOn;
        private bool _isSelected;

        public string Text
        {
            get => _text;
            set { if (_text != value) { _text = value; OnPropertyChanged(nameof(Text)); } }
        }

        public bool IsDone
        {
            get => _isDone;
            set { if (_isDone != value) { _isDone = value; OnPropertyChanged(nameof(IsDone)); } }
        }

        public DateTime? CompletedOn
        {
            get => _completedOn;
            set { if (_completedOn != value) { _completedOn = value; OnPropertyChanged(nameof(CompletedOn)); } }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // DTO for persistence of "done" state per season
    public class SeasonDoneRecord
    {
        public string Text { get; set; } = "";
        public DateTime? CompletedOn { get; set; }
    }

    public partial class SeasonalPage : ContentPage
    {
        private readonly MaintenanceManager _manager;
        private string _currentSeason = "Spring";

        // Current season's rows
        private readonly ObservableCollection<SeasonRowItem> _items = new();

        private const string PREF_SEASON_DONE_PREFIX = "SeasonDone_";
        private const int AutoRestartDays = 30;

        public SeasonalPage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            // Load persisted lists
            _manager.LoadListsFromPreferences();

            // Ensure defaults exist (adds missing only; preserves user items)
            EnsureBaselineAllSeasons();

            // Bind default tab
            BindSeason("Spring");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Auto-restart across all seasons each time page opens
            foreach (var season in new[] { "Spring", "Summer", "Autumn", "Winter" })
                AutoRestartExpiredDoneForSeason(season);

            // Refresh current season view (in case some were auto-restarted)
            SyncFromManagerSeason();
            SeasonList.ItemsSource = _items;
            UpdateDeleteSelectedEnabled();
        }

        // -------- Season switching / binding --------
        private ObservableCollection<string> CurrentList => _currentSeason switch
        {
            "Spring" => _manager.SpringChecklist,
            "Summer" => _manager.SummerChecklist,
            "Autumn" => _manager.AutumnChecklist,
            "Winter" => _manager.WinterChecklist,
            "Pool" => _manager.PoolChecklist,
            _ => _manager.SpringChecklist
        };

        private void BindSeason(string season)
        {
            // Save any edits in the previous season before switching
            SaveDoneMapForSeason(_currentSeason);

            // Auto-restart for target season before binding
            AutoRestartExpiredDoneForSeason(season);

            _currentSeason = season;

            // Rebuild rows from manager + done map
            SyncFromManagerSeason();

            // Bind
            SeasonList.ItemsSource = _items;

            // Visual highlight
            SpringBtn.BackgroundColor = season == "Spring" ? Colors.LightGreen : Colors.Transparent;
            SummerBtn.BackgroundColor = season == "Summer" ? Colors.Khaki : Colors.Transparent;
            AutumnBtn.BackgroundColor = season == "Autumn" ? Colors.BurlyWood : Colors.Transparent;
            WinterBtn.BackgroundColor = season == "Winter" ? Colors.LightBlue : Colors.Transparent;
            PoolBtn.BackgroundColor = season == "Pool" ? Colors.LightCyan : Colors.Transparent;

            UpdateDeleteSelectedEnabled();
        }

        private void OnSpring(object sender, EventArgs e) => BindSeason("Spring");
        private void OnSummer(object sender, EventArgs e) => BindSeason("Summer");
        private void OnAutumn(object sender, EventArgs e) => BindSeason("Autumn");
        private void OnWinter(object sender, EventArgs e) => BindSeason("Winter");
        private void OnPool(object sender, EventArgs e) => BindSeason("Pool");

        // -------- Add --------
        private void OnAddItem(object sender, EventArgs e)
        {
            var text = NewItemEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            _items.Add(new SeasonRowItem { Text = text, IsDone = false, CompletedOn = null, IsSelected = false });
            SyncToManagerSeasonAndSave();

            NewItemEntry.Text = "";
            UpdateDeleteSelectedEnabled();
        }

        // -------- Tap to edit --------
        private async 
        // -------- Tap to edit --------
        Task
OnRowTapped(object sender, EventArgs e)
        {
            if (sender is Label lbl && lbl.BindingContext is SeasonRowItem item)
            {
                var edited = await DisplayPromptAsync(
                    "Edit item",
                    "Update the text:",
                    accept: "Save",
                    cancel: "Cancel",
                    placeholder: item.Text,
                    maxLength: 500,
                    keyboard: Keyboard.Text);

                if (edited is null) return; // canceled
                edited = edited.Trim();
                if (string.IsNullOrWhiteSpace(edited)) return;

                // Update VM
                string old = item.Text;
                item.Text = edited;

                // Persist: manager list + done map (rename key)
                SyncToManagerSeasonAndSave();
                SaveDoneMapForSeason(_currentSeason, renameOld: old, renameNew: edited);
            }
        }

        // -------- Swipe: Edit / Restart / Delete --------
        private async void OnEditSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is SeasonRowItem item)
            {
                await OnRowTapped(new Label { BindingContext = item }, EventArgs.Empty);
            }
        }

        private void OnRestartSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is SeasonRowItem item)
            {
                item.IsDone = false;
                item.CompletedOn = null;
                SaveDoneMapForSeason(_currentSeason);
            }
        }

        private async void OnDeleteSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is SeasonRowItem item)
            {
                await ConfirmDeleteOne(item);
            }
        }

        // -------- DONE checkbox --------
        private void OnDoneChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox cb && cb.BindingContext is SeasonRowItem item)
            {
                item.IsDone = e.Value;
                item.CompletedOn = e.Value ? DateTime.Today : null;
                SaveDoneMapForSeason(_currentSeason);
            }
        }

        // -------- SELECT checkbox (bulk delete) --------
        private void OnSelectChanged(object sender, CheckedChangedEventArgs e)
        {
            UpdateDeleteSelectedEnabled();
        }

        // -------- Per-row Delete button --------
        private async void OnDeleteRowClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is SeasonRowItem item)
            {
                await ConfirmDeleteOne(item);
            }
        }

        private async System.Threading.Tasks.Task ConfirmDeleteOne(SeasonRowItem item)
        {
            bool confirm = await DisplayAlert("Delete item",
                $"Remove \"{item.Text}\" from {_currentSeason}?",
                "Delete", "Cancel");
            if (!confirm) return;

            _items.Remove(item);
            SyncToManagerSeasonAndSave();
            SaveDoneMapForSeason(_currentSeason); // drop done entry if existed
            UpdateDeleteSelectedEnabled();
        }

        // -------- Top: Delete selected (bulk) --------
        private async void OnDeleteSelectedTapped(object sender, EventArgs e)
        {
            var selected = _items.Where(i => i.IsSelected).ToList();
            if (selected.Count == 0)
            {
                await DisplayAlert("Delete selected", "No items are selected.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Delete selected",
                $"Remove {selected.Count} selected item(s) from {_currentSeason}?",
                "Delete", "Cancel");
            if (!confirm) return;

            foreach (var item in selected) _items.Remove(item);

            SyncToManagerSeasonAndSave();
            SaveDoneMapForSeason(_currentSeason);
            UpdateDeleteSelectedEnabled();
        }

        private void UpdateDeleteSelectedEnabled()
        {
            var panel = this.FindByName<Border>("DeleteSelectedPanel");
            if (panel is null) return;

            bool enabled = _items.Any(i => i.IsSelected);
            panel.Opacity = enabled ? 1.0 : 0.4;
            panel.Stroke = enabled ? Colors.Red : Colors.DarkRed;
        }

        // -------- Top: Restore defaults (add missing only) --------
        private async void OnRestoreDefaultsTapped(object sender, EventArgs e)
        {
            var defaults = GetSeasonDefaults(_currentSeason);
            if (defaults.Count == 0) return;

            bool confirm = await DisplayAlert(
                "Restore defaults",
                $"Add missing default tasks to the {_currentSeason} list?",
                "Restore",
                "Cancel");

            if (!confirm) return;

            int added = 0;
            foreach (var d in defaults)
            {
                if (!_items.Any(x => string.Equals(x.Text, d, StringComparison.OrdinalIgnoreCase)))
                {
                    _items.Add(new SeasonRowItem { Text = d, IsDone = false, CompletedOn = null, IsSelected = false });
                    added++;
                }
            }

            SyncToManagerSeasonAndSave();

            if (added == 0)
                await DisplayAlert("Restore defaults", "All defaults are already present.", "OK");
        }

        // -------- Top: Restart all done --------
        private async void OnRestartAllDoneTapped(object sender, EventArgs e)
        {
            var doneCount = _items.Count(i => i.IsDone);
            if (doneCount == 0)
            {
                await DisplayAlert("Restart all done", "No completed items to restart.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Restart all done",
                $"Restart {doneCount} completed item(s) in {_currentSeason}?",
                "Restart", "Cancel");
            if (!confirm) return;

            foreach (var item in _items.Where(i => i.IsDone))
            {
                item.IsDone = false;
                item.CompletedOn = null;
            }

            SaveDoneMapForSeason(_currentSeason);
        }

        // -------- Auto-restart (30-day expiry) --------
        private void AutoRestartExpiredDoneForSeason(string season)
        {
            var key = PREF_SEASON_DONE_PREFIX + season;
            var records = LoadDoneMapForSeason(season);

            // Filter out expired completions (>= 30 days)
            var filtered = new List<SeasonDoneRecord>();
            foreach (var r in records)
            {
                if (r.CompletedOn.HasValue)
                {
                    var days = (DateTime.Today - r.CompletedOn.Value.Date).TotalDays;
                    if (days < AutoRestartDays)
                        filtered.Add(r);
                    // else: expired ? drop (auto-restart)
                }
            }

            // Persist filtered map
            SaveDoneMapRawForSeason(season, filtered);

            // If current season, also update the VM rows now
            if (season == _currentSeason)
            {
                var dict = filtered.ToDictionary(x => x.Text, x => x.CompletedOn, StringComparer.OrdinalIgnoreCase);

                foreach (var row in _items)
                {
                    if (dict.TryGetValue(row.Text, out var completed))
                    {
                        row.IsDone = completed.HasValue;
                        row.CompletedOn = completed;
                    }
                    else
                    {
                        // Not in filtered map means it was expired ? restart
                        row.IsDone = false;
                        row.CompletedOn = null;
                    }
                }
            }
        }

        // -------- Sync VM <-> Manager list + Save --------
        private void SyncFromManagerSeason()
        {
            _items.Clear();

            // Load done map for the current season
            var doneMap = LoadDoneMapForSeason(_currentSeason)
                .ToDictionary(r => r.Text, r => r.CompletedOn, StringComparer.OrdinalIgnoreCase);

            // Build rows
            foreach (var s in CurrentList)
            {
                if (doneMap.TryGetValue(s, out var finished))
                {
                    // Skip expired here as a precaution; auto-restart also runs in BindSeason/OnAppearing
                    bool expired = finished.HasValue &&
                                   (DateTime.Today - finished.Value.Date).TotalDays >= AutoRestartDays;

                    _items.Add(new SeasonRowItem
                    {
                        Text = s,
                        IsDone = !expired && finished.HasValue,
                        CompletedOn = !expired ? finished : null,
                        IsSelected = false
                    });
                }
                else
                {
                    _items.Add(new SeasonRowItem
                    {
                        Text = s,
                        IsDone = false,
                        CompletedOn = null,
                        IsSelected = false
                    });
                }
            }
        }

        private void SyncToManagerSeasonAndSave()
        {
            // Write VM texts back to manager list
            var target = CurrentList;
            target.Clear();
            foreach (var row in _items) target.Add(row.Text);

            _manager.SaveListsToPreferences();
        }

        // -------- Persistence for "done" state (Preferences + JSON) --------
        private List<SeasonDoneRecord> LoadDoneMapForSeason(string season)
        {
            try
            {
                var key = PREF_SEASON_DONE_PREFIX + season;
                var json = Preferences.Get(key, string.Empty);
                if (string.IsNullOrWhiteSpace(json)) return new List<SeasonDoneRecord>();

                var list = JsonSerializer.Deserialize<List<SeasonDoneRecord>>(json)
                           ?? new List<SeasonDoneRecord>();
                return list;
            }
            catch { return new List<SeasonDoneRecord>(); }
        }

        private void SaveDoneMapForSeason(string season, string? renameOld = null, string? renameNew = null)
        {
            try
            {
                var key = PREF_SEASON_DONE_PREFIX + season;

                // Build save list from current VM (_items)
                var saveList = _items
                    .Where(i => i.IsDone)
                    .Select(i => new SeasonDoneRecord { Text = i.Text, CompletedOn = i.CompletedOn })
                    .ToList();

                // Handle rename (preserve completion date when text changed)
                if (!string.IsNullOrWhiteSpace(renameOld) && !string.IsNullOrWhiteSpace(renameNew))
                {
                    var old = saveList.FirstOrDefault(r => string.Equals(r.Text, renameOld, StringComparison.OrdinalIgnoreCase));
                    if (old is not null) old.Text = renameNew!;
                }

                var json = JsonSerializer.Serialize(saveList);
                Preferences.Set(key, json);
            }
            catch { /* swallow */ }
        }

        private void SaveDoneMapRawForSeason(string season, List<SeasonDoneRecord> records)
        {
            try
            {
                var key = PREF_SEASON_DONE_PREFIX + season;
                var json = JsonSerializer.Serialize(records);
                Preferences.Set(key, json);
            }
            catch { /* swallow */ }
        }

        // -------- Baseline defaults (adds missing only) on startup --------
        private void EnsureBaselineAllSeasons()
        {
            MergeDefaults(_manager.SpringChecklist, GetSeasonDefaults("Spring"));
            MergeDefaults(_manager.SummerChecklist, GetSeasonDefaults("Summer"));
            MergeDefaults(_manager.AutumnChecklist, GetSeasonDefaults("Autumn"));
            MergeDefaults(_manager.WinterChecklist, GetSeasonDefaults("Winter"));

            _manager.SaveListsToPreferences();
        }

        private static void MergeDefaults(ObservableCollection<string> list, List<string> defaults)
        {
            foreach (var d in defaults)
            {
                if (!list.Any(x => string.Equals(x, d, StringComparison.OrdinalIgnoreCase)))
                    list.Add(d);
            }
        }

        // -------- Default tasks (from your screenshots), per season --------
        private List<string> GetSeasonDefaults(string season) => season switch
        {
            "Spring" => new List<string>
            {
                "Clean, patch and repair: windows & screens; doorsills; walls; ceilings; fireplaces.",
                "Sweep, mop, vacuum: floors; walls; closets; attic; basement; garage.",
                "Check and clean A/C system, filters, and vents.",
                "Clean blinds, curtains, and drapes.",
                "Clean kitchen appliances inside/out; dust refrigerator coils.",
                "Inspect deck & patio for loose boards or nails.",
                "Check dryer vent (clear lint build-up).",
                "Prune trees and shrubs.",
                "Check window seals.",
                "Check/repair snow & ice damage: roof, gutters, downspouts, walks, driveways.",
                "Check roof and foundation for damage and leaks; make repairs.",
                "Check yard for winter damage: fences, compost/mulch; remove dead leaves; trim trees.",
                "Plant flower and vegetable gardens.",
                "Check outdoor leaks: faucets and hoses; pools."
            },

            "Summer" => new List<string>
            {
                "Replace batteries in smoke & carbon monoxide detectors.",
                "Plan/perform major repairs or renovations (rooms, additions, rehabs).",
                "Major painting and renovation: walls & wood trim; wallpaper; major redecoration.",
                "Major repairs of structural components (sheds, fences, decks).",
                "Check/exterminate pests: ants, wasps, hornets, termites, rodents.",
                "Repair & paint/stain fences, sheds, porches, and decks.",
                "Check & repair lawn and garden tools and equipment.",
                "Clean, repair, and set out lawn furniture and grills.",
                "Clean gutters and downspouts.",
                "Service air conditioning unit (change filters, clean coils).",
                "Check outdoor faucets for leaks; turn on water and check flow.",
                "Fertilize the lawn."
            },

            "Autumn" => new List<string>
            {
                "Check and clean heating system, filters, and vents.",
                "Remove and store screens; install storm windows.",
                "Caulk & weather-strip windows/doors; add insulation if needed.",
                "Cover or remove window A/C units.",
                "Clean gutters and downspouts.",
                "Prepare lawn and garden for winter: rake/mulch leaves; trim trees/shrubs.",
                "Clean and store lawn & garden tools; outdoor sports equipment.",
                "Check and repair chimneys and flues; shut off outdoor faucets and hoses.",
                "Drain outdoor faucets; blow out irrigation (where applicable)."
            },

            "Winter" => new List<string>
            {
                "Replace batteries in smoke and carbon monoxide detectors.",
                "Add insulation/cover windows: plastic sheeting; wrap water heater; wrap exposed pipes.",
                "Review and update maintenance schedule for next year.",
                "Check insulation in attic.",
                "Test smoke and carbon monoxide detectors.",
                "Clean fireplace and chimney.",
                "Check for ice dams on roof."
            },

            // Pool: keep as-is from your manager seeds
            "Pool" => new List<string>(),

            _ => new List<string>()
        };
    }
}
