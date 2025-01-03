using Fika_ProfileManager.Resources.Functions.Services;
using System.Diagnostics;
using System.Windows;

namespace Fika_ProfileManager
{
    public partial class App : Application
    {
        private ConfigService _configService;

        private async void AppStartupAsync(object sender, StartupEventArgs e)
        {
            _configService = new ConfigService();
            _configService.EnsureConfigsExist();

            Debug.WriteLine("[DEBUG]: Checking for updates in the background.");
            await Task.Delay(1500);
            await UpdateService.CheckJsonForUpdatesAsyncSilent("https://raw.githubusercontent.com/AriesLR/Fika-Profile-Manager/refs/heads/main/docs/version/update.json");
        }
    }

}
