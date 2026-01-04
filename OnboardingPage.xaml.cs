
using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
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

            // Default: no selection
            TrashDayPicker.SelectedIndex = -1;
        }

        private async void OnContinueClicked(object sender, EventArgs e)
        {
            var addr = AddressEntry.Text?.Trim() ?? "";
            _manager.SetPropertyAddress(addr);
            _manager.SetHasPool(PoolSwitch.IsToggled);

            // Map picker to DayOfWeek?
            System.DayOfWeek? trashDay = null;
            if (TrashDayPicker.SelectedIndex >= 0)
            {
                var text = (string)TrashDayPicker.Items[TrashDayPicker.SelectedIndex];
                trashDay = text switch
                {
                    "Sunday" => DayOfWeek.Sunday,
                    "Monday" => DayOfWeek.Monday,
                    "Tuesday" => DayOfWeek.Tuesday,
                    "Wednesday" => DayOfWeek.Wednesday,
                    "Thursday" => DayOfWeek.Thursday,
                    "Friday" => DayOfWeek.Friday,
                    "Saturday" => DayOfWeek.Saturday,
                    _ => null
                };
                _manager.SetTrashDay(trashDay);
            }

            // mark first run complete
            Preferences.Set("FirstRunCompleted", true);

            await Shell.Current.GoToAsync("//Home");
        }
    }
}

