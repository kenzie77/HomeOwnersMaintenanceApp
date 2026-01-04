
using System;
using Microsoft.Maui.Controls;
using HomeMaintenanceApp.Models;
using HomeMaintenanceApp.Services;

namespace HomeMaintenanceApp.Pages
{
    public partial class AppliancesPage : ContentPage
    {
        private readonly MaintenanceManager _manager;

        public AppliancesPage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            ApplianceList.ItemsSource = _manager.Appliances;

            TypePicker.SelectedIndex = 5; // Other
            InstallDatePicker.Date = DateTime.Today;
            LastFilterDatePicker.Date = DateTime.Today;
        }

        private void OnAddApplianceClicked(object sender, EventArgs e)
        {
            AddForm.IsVisible = true;

            NameEntry.Text = "";
            ManufacturerEntry.Text = "";
            ModelEntry.Text = "";
            SerialEntry.Text = "";
            LocationEntry.Text = "";
            TypePicker.SelectedIndex = 5;
            InstallDatePicker.Date = DateTime.Today;
            LastFilterDatePicker.Date = DateTime.Today;
        }

        private void OnCancelApplianceClicked(object sender, EventArgs e)
        {
            AddForm.IsVisible = false;
        }

        private void OnSaveApplianceClicked(object sender, EventArgs e)
        {
            var name = NameEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                DisplayAlert("Validation", "Please enter a name for the appliance.", "OK");
                return;
            }

            var typeText = (string)TypePicker.SelectedItem;
            var type = typeText switch
            {
                "HVAC" => ApplianceType.HVAC,
                "Plumbing" => ApplianceType.Plumbing,
                "Electrical" => ApplianceType.Electrical,
                "Kitchen" => ApplianceType.Kitchen,
                "Laundry" => ApplianceType.Laundry,
                _ => ApplianceType.Other
            };

            var newAppliance = new Appliance
            {
                Name = name,
                Manufacturer = ManufacturerEntry.Text?.Trim() ?? "",
                Model = ModelEntry.Text?.Trim() ?? "",
                SerialNumber = SerialEntry.Text?.Trim() ?? "",
                Location = LocationEntry.Text?.Trim() ?? "",
                InstallDate = InstallDatePicker.Date,
                LastFilterChangeDate = LastFilterDatePicker.Date,
                Type = type
            };

            // Use the correct field (_manager)
            _manager.AddAppliance(newAppliance);

            AddForm.IsVisible = false;
        }
    }
}
