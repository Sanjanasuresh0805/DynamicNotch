using System.Windows;
using DynamicNotch.Services;

namespace DynamicNotch;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = SettingsService.Load();
        if (settings.IsFirstRun)
        {
            var onboarding = new Views.OnboardingWindow();
            onboarding.Show();
            settings.IsFirstRun = false;
            SettingsService.Save(settings);
        }
    }
}