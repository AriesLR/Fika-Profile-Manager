using Fika_ProfileManager.Resources.Functions.Services;
using MahApps.Metro.Controls;
using PakMaster.Resources.Functions.Services;
using System.Windows;

namespace Fika_ProfileManager
{
    public partial class MainWindow : MetroWindow
    {
        private ConfigService _configService;

        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            _isInitialized = true;
            _configService = new ConfigService();
        }

        // Open AriesLR's GitHub
        private void LaunchBrowserGitHubAriesLR(object sender, RoutedEventArgs e)
        {
            UrlService.OpenUrlAsync("https://github.com/AriesLR");
        }

        // Check for updates via json
        private async void CheckForUpdatesAsync(object sender, RoutedEventArgs e)
        {
            await UpdateService.CheckJsonForUpdatesAsync("https://raw.githubusercontent.com/AriesLR/PakMaster/refs/heads/main/docs/version/update.json");
        }
    }
}