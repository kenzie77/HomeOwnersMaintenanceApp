
using Microsoft.Maui.Controls;
using HomeMaintenanceApp.Services;

namespace HomeMaintenanceApp.Pages
{
    public partial class HurricanePage : ContentPage
    {
        private readonly MaintenanceManager _manager;

        public HurricanePage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            // Bind list to the manager collection
            HList.ItemsSource = _manager.HurricaneChecklist;

            // Optional: load persisted lists, if your manager supports it
            _manager.LoadListsFromPreferences();
        }

        private void OnAddHItem(object sender, System.EventArgs e)
        {
            var text = NewHItemEntry.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                // ? Add directly to the checklist; no manager method needed
                _manager.HurricaneChecklist.Add(text);

                // Persist if your manager implements Preferences persistence
                _manager.SaveListsToPreferences();

                // Clear the entry
                NewHItemEntry.Text = string.Empty;
            }
        }
    }
}

