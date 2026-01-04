
namespace HomeMaintenanceApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

#if WINDOWS || MACCATALYST
            this.FlyoutBehavior = FlyoutBehavior.Locked;
#else
            this.FlyoutBehavior = FlyoutBehavior.Flyout;
#endif
        }
    }
}
