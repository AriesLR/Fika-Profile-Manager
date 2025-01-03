using Fika_ProfileManager.Resources.Functions.Services;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace Fika_ProfileManager
{
    public partial class MainWindow : MetroWindow
    {
        private ConfigService _configService;

        private string? fikaProfilePath;
        private string? localProfilePath;

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
                    fikaProfilePath = Path.Combine(userFolderPath, "fika");
                    localProfilePath = Path.Combine(userFolderPath, "profiles");

                    SptPathTextBox.Text = sptPath;

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