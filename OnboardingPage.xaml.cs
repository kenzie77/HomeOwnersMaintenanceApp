using System;
using Microsoft.Maui.Controls;
using HomeMaintenanceApp.Services;

namespace HomeMaintenanceApp.Pages
{
    public partial class OnboardingPage : ContentPage
    {
        private readonly MaintenanceManager _manager;

        public OnboardingPage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            // Load persisted property values into the UI
            _manager.LoadPropertyFromPreferences();

            AddressEntry.Text = _manager.Property?.Address ?? string.Empty;
            HasPoolSwitch.IsToggled = _manager.Property?.HasPool ?? false;

            // Trash day -> Picker; default to "(not set)" if null
            var trash = _manager.Property?.TrashDay;
            TrashDayPicker.SelectedIndex = trash.HasValue ? (int)trash.Value : 7; // index 7 == "(not set)"

            RefreshPropertySummary();
        }

        private void RefreshPropertySummary()
        {
            var addr = _manager.Property?.Address ?? "";
            AddressLabel.Text = string.IsNullOrWhiteSpace(addr) ? "(empty)" : addr;

            PoolLabel.Text = (_manager.Property?.HasPool ?? false) ? "Yes" : "No";
            var trash = _manager.Property?.TrashDay;
            TrashLabel.Text = trash.HasValue ? trash.Value.ToString() : "(not set)";
        }

        // Save property (Address, HasPool, TrashDay)
        private async void OnSavePropertyClicked(object sender, EventArgs e)
        {
            var newAddress = AddressEntry.Text?.Trim() ?? string.Empty;
            var hasPool = HasPoolSwitch.IsToggled;

            DayOfWeek? trashDay = null;
            if (TrashDayPicker.SelectedIndex >= 0 && TrashDayPicker.SelectedIndex <= 6)
                trashDay = (DayOfWeek)TrashDayPicker.SelectedIndex;

            // Save via manager
            _manager.SetPropertyAddress(newAddress);
            _manager.SetHasPool(hasPool);
            _manager.SetTrashDay(trashDay);

            RefreshPropertySummary();

            await DisplayAlert("Saved", "Property values updated.", "OK");
        }

        // Reset Property only (Address, HasPool, TrashDay)
        private async void OnResetPropertyClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Reset Property",
                "This will clear Address, Has Pool, and Trash Day. Continue?",
                "Reset", "Cancel");

            if (!confirm) return;

            _manager.ResetAllData(); // calls SavePropertyToPreferences internally

            // Refresh UI fields to blank/defaults
            AddressEntry.Text = string.Empty;
            HasPoolSwitch.IsToggled = false;
            TrashDayPicker.SelectedIndex = 7; // "(not set)"

            RefreshPropertySummary();

            await DisplayAlert("Done", "Property values cleared.", "OK");
        }

        // Reset ALL local data (factory reset)
        private async void OnResetAllClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert(
                "Reset ALL Data",
                "This will clear ALL local app data (Tasks, Issues + History, Seasonal/Pool/Hurricane lists, Knowledge notes, and Property). Continue?",
                "Reset ALL", "Cancel");

            if (!confirm) return;

            _manager.ResetAllData();

            // Refresh UI after a factory reset
            AddressEntry.Text = string.Empty;
            HasPoolSwitch.IsToggled = false;
            TrashDayPicker.SelectedIndex = 7;

            RefreshPropertySummary();

            await DisplayAlert("Done", "All local app data has been cleared.", "OK");
        }
    }
}
