
using System;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
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

            // SAFELY attach to Loaded only if MainPage is a Page
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

            // Small delay helps on Windows/Mac where Shell may not be ready instantly
            await Task.Delay(100);

            // First-run logic: true if we have not completed onboarding
            bool isFirstRun = !Preferences.Get("FirstRunCompleted", false);

            // Navigate on UI thread; guard with try/catch to prevent startup crashes
            try
            {
                await Dispatcher.DispatchAsync(async () =>
                {
                    // Ensure Shell.Current is available before navigating
                    if (Shell.Current is null)
                        return;

                    if (isFirstRun)
                    {
                        // Navigate to onboarding (registered as //Onboarding in AppShell)
                        await Shell.Current.GoToAsync("//Onboarding");
                    }
                    else
                    {
                        // Navigate to Home
                        await Shell.Current.GoToAsync("//Home");
                    }
                });
            }
            catch (Exception ex)
            {
                // Swallow/optionally log; ensures startup is resilient
                System.Diagnostics.Debug.WriteLine($"[Startup Navigation] {ex}");
            }
        }
    }
}
