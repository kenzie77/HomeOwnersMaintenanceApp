
using CommunityToolkit.Maui;
using HomeMaintenanceApp.Services;
using HomwMaintenanceApp;


namespace HomeMaintenanceApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit();

            builder.Services.AddSingleton<MaintenanceManager>();
            builder.Services.AddSingleton<Pages.AppliancesPage>();
            builder.Services.AddSingleton<MaintenanceManager>();
            builder.Services.AddSingleton<Pages.OnboardingPage>();
            builder.Services.AddSingleton<Pages.HomePage>();
            builder.Services.AddSingleton<Pages.TaskPage>();        // singular
            builder.Services.AddSingleton<Pages.AppliancesPage>();
            builder.Services.AddSingleton<Pages.IssuesPage>();
            builder.Services.AddSingleton<Pages.KnowledgePage>();
            builder.Services.AddSingleton<Pages.SeasonalPage>();
            builder.Services.AddSingleton<Pages.HurricanePage>();




            return builder.Build();
        }
    }
}
