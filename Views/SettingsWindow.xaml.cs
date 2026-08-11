using System.Windows;
using System.Windows.Input;
using DynamicNotch.Services;

namespace DynamicNotch.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        StartupCheckBox.IsChecked = StartupService.IsEnabled();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        var settings = SettingsService.Load();
        settings.RunAtStartup = StartupCheckBox.IsChecked == true;
        settings.ShowMedia = MediaCheckBox.IsChecked == true;
        settings.ShowCalendar = CalendarCheckBox.IsChecked == true;
        settings.ShowMirror = MirrorCheckBox.IsChecked == true;
        SettingsService.Save(settings);
        Close();
    }

    private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        StartupService.SetEnabled(StartupCheckBox.IsChecked == true);
    }

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}