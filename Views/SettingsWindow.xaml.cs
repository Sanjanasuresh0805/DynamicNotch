using System.Windows;
using System.Windows.Input;

namespace DynamicNotch.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            PositionNearNotch();
        }

        /// <summary>
        /// Positions the settings window to the right of the expanded notch,
        /// close to the gear button.
        /// </summary>
        private void PositionNearNotch()
        {
            // Get primary screen width
            double screenWidth = SystemParameters.PrimaryScreenWidth;

            // Notch is centered horizontally at top, expanded width = 680
            double notchExpandedWidth = 680;
            double notchLeftEdge = (screenWidth - notchExpandedWidth) / 2;
            double notchRightEdge = notchLeftEdge + notchExpandedWidth;

            // Place settings window right beside the notch (8px gap)
            Left = notchRightEdge + 8;

            // Vertically align near the top of the notch (notch top = 6)
            Top = 6;
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void QuitButton_Click(object sender, RoutedEventArgs e)
            => System.Windows.Application.Current.Shutdown();
    }
}