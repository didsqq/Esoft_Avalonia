using Avalonia.Controls;
using Avalonia.Interactivity;
using ObjectivePlatformApp; // Corrected namespace

namespace ObjectivePlatformApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void ClientsButton_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)TopLevel.GetTopLevel(this);
            mainWindow.Content = new ClientsWindow();
        }

        private void RealtorsButton_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)TopLevel.GetTopLevel(this);
            mainWindow.Content = new AgentsWindow(); // Assuming AgentsWindow for Realtors
        }

        private void PropertiesButton_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)TopLevel.GetTopLevel(this);
            mainWindow.Content = new RealEstatesWindow();
        }
    }
}
