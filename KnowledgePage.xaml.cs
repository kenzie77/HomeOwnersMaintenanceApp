
using Microsoft.Maui.Controls;
using HomeMaintenanceApp.Services;

namespace HomeMaintenanceApp.Pages
{
    public partial class KnowledgePage : ContentPage
    {
        private readonly MaintenanceManager _manager;

        public KnowledgePage()
        {
            InitializeComponent();

            _manager = new MaintenanceManager();

            // Load persisted user notes
            _manager.LoadKnowledgeFromPreferences();

            // Bind seeded views + user notes
            ToolsList.ItemsSource = _manager.KnowledgeToolsView;
            UsefulLifeList.ItemsSource = _manager.KnowledgeUsefulLife;
            KnowledgeList.ItemsSource = _manager.KnowledgeResources;
        }

        private void OnAddKnowledgeClicked(object sender, System.EventArgs e)
        {
            var text = NewKnowledgeEntry.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                _manager.KnowledgeResources.Add(text);
                _manager.SaveKnowledgeToPreferences();
                NewKnowledgeEntry.Text = string.Empty;
            }
        }

        private void OnDeleteKnowledgeSwipe(object sender, System.EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is string item)
            {
                _manager.KnowledgeResources.Remove(item);
                _manager.SaveKnowledgeToPreferences();
            }
        }
    }
}
