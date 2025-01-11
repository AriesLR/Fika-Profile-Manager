using Fika_ProfileManager.Resources.Functions.Services;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Fika_ProfileManager
{
    public partial class MainWindow : MetroWindow
    {
        private ConfigService _configService;

        private string? fikaProfilePath;
        private string? localProfilePath;
        private string? launcherConfigPath;

        public MainWindow()
        {
            InitializeComponent();
            _configService = new ConfigService();

            LoadAppConfigAsync();
        }

        // Open AriesLR's GitHub
        private void LaunchBrowserGitHubAriesLR(object sender, RoutedEventArgs e)
        {
            UrlService.OpenUrlAsync("https://github.com/AriesLR");
        }

        // Check for updates via json
        private async void CheckForUpdatesAsync(object sender, RoutedEventArgs e)
        {
            await UpdateService.CheckJsonForUpdatesAsync("https://raw.githubusercontent.com/AriesLR/Fika-Profile-Manager/refs/heads/main/docs/version/update.json");
        }

        // Load App Config
        private async void LoadAppConfigAsync()
        {
            try
            {
                var config = _configService.LoadAppConfig<dynamic>();
                string sptPath = config?.SptPath ?? string.Empty;

                if (!string.IsNullOrEmpty(sptPath))
                {
                    string userFolderPath = Path.Combine(sptPath, "user");
                    string launcherFolderPath = Path.Combine(userFolderPath, "launcher");
                    fikaProfilePath = Path.Combine(userFolderPath, "fika");
                    localProfilePath = Path.Combine(userFolderPath, "profiles");
                    launcherConfigPath = Path.Combine(launcherFolderPath, "config.json");

                    SptPathTextBox.Text = sptPath;
                    FikaIpTextBox.Text = config.ServerIp;

                    RefreshUI();
                }
            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"Error loading config: {ex.Message}");
            }
        }

        // Browse SPT path
        private void BrowseSptPath(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select SPT Folder",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Folder Selection"
            };

            var config = _configService.LoadAppConfig<dynamic>();
            string lastPath = config?.SptPath ?? string.Empty;
            if (!string.IsNullOrEmpty(lastPath))
            {
                dialog.InitialDirectory = lastPath;
            }

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    config.SptPath = selectedPath;
                    _configService.SaveAppConfig(config);

                    SptPathTextBox.Text = selectedPath;

                    string userFolderPath = Path.Combine(selectedPath, "user");
                    fikaProfilePath = Path.Combine(userFolderPath, "fika");
                    localProfilePath = Path.Combine(userFolderPath, "profiles");

                    RefreshUI();
                }
            }
        }

        // Save Fika IP
        private async Task SaveFikaIpAsync()
        {
            try
            {
                var config = _configService.LoadAppConfig<dynamic>();

                if (config != null && config.ServerIp != null)
                {
                    string newUrl = FikaIpTextBox.Text.Trim();

                    if (!string.IsNullOrEmpty(newUrl))
                    {
                        config.ServerIp = newUrl;

                        _configService.SaveAppConfig(config);

                        await MessageService.ShowSplash();
                    }
                    else
                    {
                        await MessageService.ShowWarning("Please provide a valid URL in the Fika IP textbox.");
                    }
                }
                else
                {
                    await MessageService.ShowError("Failed to load the configuration or locate the 'Server' section.");
                }
            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"An error occurred while saving the Fika IP: {ex.Message}");
            }
        }

        // Get Ip from app config
        private async Task SetServerUrlAsync()
        {
            try
            {
                var appConfig = _configService.LoadAppConfig<dynamic>();
                string serverIp = appConfig?.ServerIp;

                if (string.IsNullOrEmpty(serverIp))
                {
                    await MessageService.ShowError("Server IP is not defined in the application configuration.");
                    return;
                }

                if (File.Exists(launcherConfigPath))
                {
                    string jsonContent = await File.ReadAllTextAsync(launcherConfigPath);
                    var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                    if (config != null)
                    {
                        if (config.TryGetValue("Server", out var serverSection) &&
                            serverSection is JsonElement serverElement &&
                            serverElement.ValueKind == JsonValueKind.Object)
                        {
                            var serverConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(serverElement.GetRawText());

                            if (serverConfig != null && serverConfig.ContainsKey("Url"))
                            {
                                serverConfig["Url"] = serverIp;

                                config["Server"] = serverConfig;

                                string updatedJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                                await File.WriteAllTextAsync(launcherConfigPath, updatedJson);

                                Debug.WriteLine($"[DEBUG]: Updated Url to '{serverIp}'");
                            }
                            else
                            {
                                await MessageService.ShowError("The 'Server' section does not contain a 'Url' key.");
                            }
                        }
                        else
                        {
                            await MessageService.ShowError("The config does not contain a valid 'Server' section.");
                        }
                    }
                    else
                    {
                        await MessageService.ShowError("Failed to parse the configuration file.");
                    }
                }
                else
                {
                    await MessageService.ShowError("The configuration file does not exist.");
                }
            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"An error occurred while processing the configuration: {ex.Message}");
            }
        }

        // Save Fika IP
        private async void btnSaveFikaIp_ClickAsync(object sender, RoutedEventArgs e)
        {
            await SaveFikaIpAsync();
        }

        // Move Profile (Backup and Move)
        private async void btnMoveProfile_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (FikaProfilesListBox.SelectedItem is KeyValuePair<string, string> selectedFile)
            {
                string selectedFilePath = selectedFile.Value;
                string selectedFileName = selectedFile.Key;

                if (File.Exists(selectedFilePath))
                {
                    try
                    {
                        string backupFilePath = Path.Combine(fikaProfilePath, Path.GetFileNameWithoutExtension(selectedFileName) + "_backup" + Path.GetExtension(selectedFileName));
                        File.Copy(selectedFilePath, backupFilePath, true);
                        File.SetLastWriteTime(backupFilePath, DateTime.Now);

                        string destinationFilePath = Path.Combine(localProfilePath, selectedFileName);
                        File.Copy(selectedFilePath, destinationFilePath, true);
                        File.SetLastWriteTime(destinationFilePath, DateTime.Now);

                        RefreshUI();

                        await MessageService.ShowInfo("Success", "Profile successfully backed up and moved.");
                    }
                    catch (Exception ex)
                    {
                        await MessageService.ShowError($"Error during the file operation: {ex.Message}");
                    }
                }
                else
                {
                    await MessageService.ShowError("Selected file does not exist.");
                }
            }
            else
            {
                await MessageService.ShowWarning("Please select a fika profile to move.");
            }
        }

        // Play SPT (solo)
        private async void btnPlaySpt_ClickAsync(object sender, RoutedEventArgs e)
        {
            bool userConfirmed = await MessageService.ShowYesNo("Launch Game", "You're about to launch in Singleplayer Mode, are you sure?");
            if (userConfirmed)
            {
                await DisableDevMode();
                await Task.Delay(2000);
                await RunSptServerAsync();
                await Task.Delay(15000);
                await RunSptLauncherAsync();

                await Task.Delay(2000);
                Application.Current.Shutdown();
            }
        }

        // Play Fika (MP)
        private async void btnPlayFika_ClickAsync(object sender, RoutedEventArgs e)
        {
            bool userConfirmed = await MessageService.ShowYesNo("Launch Game", "You're about to launch in Multiplayer Mode, are you sure?");
            if (userConfirmed)
            {
                await EnableDevMode();
                await Task.Delay(1000);
                await SaveFikaIpAsync();
                await SetServerUrlAsync();
                await Task.Delay(2000);
                await RunSptLauncherAsync();

                await Task.Delay(2000);
                Application.Current.Shutdown();
            }
        }

        // Open Local Profile Path
        private async void btnOpenLocalProfilePath_ClickAsync(object sender, RoutedEventArgs e)
        {
            await OpenLocalProfileFolderAsync();
        }

        // Disable Dev Mode
        private async Task DisableDevMode()
        {
            try
            {
                if (File.Exists(launcherConfigPath))
                {
                    string jsonContent = await File.ReadAllTextAsync(launcherConfigPath);
                    var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                    if (config != null && config.TryGetValue("IsDevMode", out var isDevMode))
                    {
                        if (isDevMode is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.True)
                        {
                            config["IsDevMode"] = false;

                            string updatedJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                            await File.WriteAllTextAsync(launcherConfigPath, updatedJson);
                            Debug.WriteLine("[DEBUG]: IsDevMode = false");
                        }
                    }
                    else
                    {
                        await MessageService.ShowError("The config does not contain an 'IsDevMode' key.");
                    }
                }
                else
                {
                    await MessageService.ShowError("The config file does not exist.");
                }
            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"An error occurred while processing the config: {ex.Message}");
            }
        }

        // Enable Dev Mode
        private async Task EnableDevMode()
        {
            try
            {
                if (File.Exists(launcherConfigPath))
                {
                    string jsonContent = await File.ReadAllTextAsync(launcherConfigPath);
                    var config = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                    if (config != null && config.TryGetValue("IsDevMode", out var isDevMode))
                    {
                        if (isDevMode is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.False)
                        {
                            config["IsDevMode"] = true;

                            string updatedJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                            await File.WriteAllTextAsync(launcherConfigPath, updatedJson);
                            Debug.WriteLine("[DEBUG]: IsDevMode = true");

                        }
                    }
                    else
                    {
                        await MessageService.ShowError("The config does not contain an 'IsDevMode' key.");
                    }
                }
                else
                {
                    await MessageService.ShowError("The config file does not exist.");
                }
            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"An error occurred while processing the config: {ex.Message}");
            }
        }

        // Open Local Profile Folder
        private async Task OpenLocalProfileFolderAsync()
        {
            try
            {
                if (Directory.Exists(localProfilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = localProfilePath,
                        UseShellExecute = true,
                    });
                }
                else
                {
                    await MessageService.ShowError("The specified folder does not exist.");
                }
            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"An error occurred while trying to open the folder: {ex.Message}");
            }
        }

        // Run SPT Server
        private async Task RunSptServerAsync()
        {
            try
            {
                var config = _configService.LoadAppConfig<dynamic>();
                string sptPath = config?.SptPath ?? string.Empty;

                // Ensure the SPT.Server.exe exists
                string serverExecutablePath = Path.Combine(sptPath, "SPT.Server.exe");
                if (!File.Exists(serverExecutablePath))
                {
                    await MessageService.ShowError("SPT.Server.exe not found in the specified path.");
                    return;
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = serverExecutablePath,
                    WorkingDirectory = sptPath,
                    UseShellExecute = true
                };

                Debug.WriteLine("Starting SPT.Server.exe");
                Process.Start(processStartInfo);

            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"An error occurred while starting SPT.Server.exe: {ex.Message}");
            }
        }

        // Run SPT Launcher
        private async Task RunSptLauncherAsync()
        {
            try
            {
                var config = _configService.LoadAppConfig<dynamic>();
                string sptPath = config?.SptPath ?? string.Empty;

                // Ensure the SPT.Launcher.exe exists
                string serverExecutablePath = Path.Combine(sptPath, "SPT.Launcher.exe");
                if (!File.Exists(serverExecutablePath))
                {
                    await MessageService.ShowError("SPT.Launcher.exe not found in the specified path.");
                    return;
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = serverExecutablePath,
                    WorkingDirectory = sptPath,
                    UseShellExecute = true
                };

                Debug.WriteLine("Starting SPT.Launcher.exe");
                Process.Start(processStartInfo);

            }
            catch (Exception ex)
            {
                await MessageService.ShowError($"An error occurred while starting SPT.Launcher.exe: {ex.Message}");
            }
        }

        // Refresh Button
        private void btnRefreshUI_Click(object sender, RoutedEventArgs e)
        {
            RepopulateFikaListBox();
            RepopulateLocalListBox();
        }

        // Refresh UI Elements
        private void RefreshUI()
        {
            if (!string.IsNullOrEmpty(fikaProfilePath) && !string.IsNullOrEmpty(localProfilePath))
            {
                RepopulateFikaListBox();
                RepopulateLocalListBox();
            }
        }

        // Repopulate Fika ListBox
        private void RepopulateFikaListBox()
        {
            if (!string.IsNullOrEmpty(fikaProfilePath))
            {
                List<KeyValuePair<string, string>> files = new List<KeyValuePair<string, string>>();

                files.AddRange(Directory.GetFiles(fikaProfilePath, "*.json")
                .Select(filePath => new KeyValuePair<string, string>(Path.GetFileName(filePath), filePath)));

                FikaProfilesListBox.ItemsSource = files;
            }
        }

        // Repopulate Local ListBox
        private void RepopulateLocalListBox()
        {
            if (!string.IsNullOrEmpty(localProfilePath))
            {
                List<KeyValuePair<string, string>> files = new List<KeyValuePair<string, string>>();

                files.AddRange(Directory.GetFiles(localProfilePath, "*.json")
                .Select(filePath => new KeyValuePair<string, string>(Path.GetFileName(filePath), filePath)));

                LocalProfilesListBox.ItemsSource = files;
            }
        }
    }
}