
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls;
using HomeMaintenanceApp.Services;
using Microsoft.Maui.Graphics;

namespace HomeMaintenanceApp.Pages
{
    // Lightweight wrapper for selection + text
    public class ChecklistItem
    {
        public string Text { get; set; } = "";
        public bool IsSelected { get; set; } = false;
    }

    public partial class HurricanePage : ContentPage
    {
        private readonly MaintenanceManager _manager;

        // Internal view model list that mirrors manager.HurricaneChecklist (strings)
        private readonly ObservableCollection<ChecklistItem> _items = new();

        public HurricanePage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            // Load persisted lists (Seasonal/Hurricane)
            _manager.LoadListsFromPreferences();

            // Mirror manager list into view models (Text + selection)
            SyncFromManager();

            // Bind list
            HList.ItemsSource = _items;

            // Initialize top delete enabled state
            UpdateDeleteSelectedEnabled();
        }

        // -------------------- Sync helpers --------------------
        private void SyncFromManager()
        {
            _items.Clear();
            foreach (var s in _manager.HurricaneChecklist)
            {
                _items.Add(new ChecklistItem { Text = s, IsSelected = false });
            }
        }

        private void SyncToManagerAndSave()
        {
            _manager.HurricaneChecklist.Clear();
            foreach (var ci in _items)
                _manager.HurricaneChecklist.Add(ci.Text);

            _manager.SaveListsToPreferences();
        }

        private void UpdateDeleteSelectedEnabled()
        {
            bool enabled = _items.Any(i => i.IsSelected);

            // change opacity to signal disabled
            DeleteSelectedPanel.Opacity = enabled ? 1.0 : 0.4;

            // optional: change stroke to further signal
            DeleteSelectedPanel.Stroke = enabled ? Colors.Red : Colors.DarkRed;
        }

        // -------------------- Add --------------------
        private void OnAddHItem(object sender, EventArgs e)
        {
            var text = NewHItemEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            _items.Add(new ChecklistItem { Text = text });
            SyncToManagerAndSave();

            NewHItemEntry.Text = string.Empty;
            UpdateDeleteSelectedEnabled();
        }

        // -------------------- Edit via Swipe --------------------
        private async void OnEditSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is ChecklistItem item)
            {
                await PromptAndReplace(item);
            }
        }

        // -------------------- Delete via Swipe --------------------
        private async void OnDeleteSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is ChecklistItem item)
            {
                await ConfirmDelete(item);
            }
        }

        // -------------------- Tap to edit --------------------
        private async void OnRowTapped(object sender, EventArgs e)
        {
            if (sender is Label label && label.BindingContext is ChecklistItem item)
            {
                await PromptAndReplace(item);
            }
        }

        // -------------------- Check to select for top-delete --------------------
        private void OnRowSelectedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is CheckBox cb && cb.BindingContext is ChecklistItem item)
            {
                item.IsSelected = e.Value;
                UpdateDeleteSelectedEnabled();
            }
        }

        // -------------------- Per-row red Delete button --------------------
        private async void OnDeleteRowClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is ChecklistItem item)
            {
                await ConfirmDelete(item);
            }
        }

        // -------------------- Top red Delete Selected (Border tap) --------------------
        private async void OnDeleteSelectedTapped(object sender, EventArgs e)
        {
            var toDelete = _items.Where(i => i.IsSelected).ToList();
            if (toDelete.Count == 0)
            {
                await DisplayAlert("Delete selected", "No items are selected.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Delete selected",
                $"Remove {toDelete.Count} selected item(s)?",
                "Delete", "Cancel");
            if (!confirm) return;

            foreach (var item in toDelete)
                _items.Remove(item);

            SyncToManagerAndSave();
            UpdateDeleteSelectedEnabled();
        }

        // -------------------- Common edit prompt + replace --------------------
        private async System.Threading.Tasks.Task PromptAndReplace(ChecklistItem item)
        {
            var edited = await DisplayPromptAsync(
                "Edit checklist item",
                "Update the text:",
                accept: "Save",
                cancel: "Cancel",
                placeholder: item.Text,
                maxLength: 500,
                keyboard: Keyboard.Text
            );

            if (edited is null) return; // canceled
            edited = edited.Trim();
            if (string.IsNullOrWhiteSpace(edited)) return;

            item.Text = edited;
            SyncToManagerAndSave();
        }

        private async System.Threading.Tasks.Task ConfirmDelete(ChecklistItem item)
        {
            bool confirm = await DisplayAlert("Delete item",
                $"Remove \"{item.Text}\" from the checklist?",
                "Delete", "Cancel");
            if (!confirm) return;

            _items.Remove(item);
            SyncToManagerAndSave();
            UpdateDeleteSelectedEnabled();
        }
    }
}
