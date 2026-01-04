using Microsoft.Maui.Storage;

namespace HomeMaintenanceApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Set Shell as the root visual
            MainPage = new AppShell();

            // SAFELY attach to Loaded only if MainPage is not null and is a Page
            if (MainPage is Page page)
            {
                page.Loaded += OnMainPageLoaded;
            }
        }

        private async void OnMainPageLoaded(object? sender, EventArgs e)
        {
            // Unsubscribe immediately to avoid repeated calls
            if (sender is Page page)
                page.Loaded -= OnMainPageLoaded;

            // First-run logic
            bool isFirstRun = !Preferences.Get("FirstRunCompleted", false);

            // Navigate on UI thread
            await Dispatcher.DispatchAsync(async () =>
            {
                if (isFirstRun)
                {
                    await Shell.Current.GoToAsync("//Onboarding");
                }
                else
                {
                    await Shell.Current.GoToAsync("//Home");
                }
            });
        }
    }
}

