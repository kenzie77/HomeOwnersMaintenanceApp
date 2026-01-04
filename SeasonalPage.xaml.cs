
using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using HomeMaintenanceApp.Services;

namespace HomeMaintenanceApp.Pages
{
    public partial class SeasonalPage : ContentPage
    {
        private readonly MaintenanceManager _manager;
        private string _currentSeason = "Spring";

        public SeasonalPage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            // Load persisted lists (if any)
            _manager.LoadListsFromPreferences();

            BindSeason("Spring");
        }

        // Returns the list for the current season (including Pool)
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
            _currentSeason = season;

            SeasonList.ItemsSource = CurrentList;

            // Simple visual highlight for selected season
            SpringBtn.BackgroundColor = season == "Spring" ? Colors.LightGreen : Colors.Transparent;
            SummerBtn.BackgroundColor = season == "Summer" ? Colors.Khaki : Colors.Transparent;
            AutumnBtn.BackgroundColor = season == "Autumn" ? Colors.BurlyWood : Colors.Transparent;
            WinterBtn.BackgroundColor = season == "Winter" ? Colors.LightBlue : Colors.Transparent;
            PoolBtn.BackgroundColor = season == "Pool" ? Colors.LightCyan : Colors.Transparent;
        }

        private void OnSpring(object sender, EventArgs e) => BindSeason("Spring");
        private void OnSummer(object sender, EventArgs e) => BindSeason("Summer");
        private void OnAutumn(object sender, EventArgs e) => BindSeason("Autumn");
        private void OnWinter(object sender, EventArgs e) => BindSeason("Winter");
        private void OnPool(object sender, EventArgs e) => BindSeason("Pool");

        private void OnAddItem(object sender, EventArgs e)
        {
            var text = NewItemEntry.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                CurrentList.Add(text);
                _manager.SaveListsToPreferences();
                NewItemEntry.Text = "";
            }
        }
    }
}
