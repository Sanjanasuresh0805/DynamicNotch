using System.Windows;

namespace DynamicNotch.Views;

public partial class OnboardingWindow : Window
{
    public OnboardingWindow()
    {
        InitializeComponent();
    }

    private void GetStarted_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}