using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;

namespace Fika_ProfileManager.Resources.Functions.Services
{
    public class ConfigService
    {
        private readonly string _configFilePath;
        public ConfigService()
        {
            // Get the path of the 'configs' folder next to the app
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string configsDirectory = Path.Combine(appDirectory, "configs");

            // Ensure the 'configs' folder exists
            if (!Directory.Exists(configsDirectory))
            {
                Directory.CreateDirectory(configsDirectory);
            }

            // Config File Paths
            _configFilePath = Path.Combine(configsDirectory, "config.json"); // Main Config
        }

        public void EnsureConfigsExist()
        {
            if (!File.Exists(_configFilePath))
            {
                Debug.WriteLine("[DEBUG]: Config doesn't exist, creating one now.");
                CreateDefaultConfig();
            }
        }

        // Create Default UnrealPak Config
        private void CreateDefaultConfig()
        {
            var defaultConfig = new
            {
                SptPath = string.Empty
            };

            SaveConfig(_configFilePath, defaultConfig);

            Debug.WriteLine("[DEBUG]: Default Config created.");
        }

        // Load App Config
        public T LoadAppConfig<T>() where T : new()
        {
            return LoadConfig<T>(_configFilePath);
        }

        // Save App Config
        public void SaveAppConfig(object config)
        {
            SaveConfig(_configFilePath, config);
            Debug.WriteLine("[DEBUG]: Config saved.");
        }

        // Load Config Helper
        private T LoadConfig<T>(string filePath) where T : new()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<T>(json);
                }
                else
                {
                    return new T();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DEBUG]: Error loading config from {filePath}: {ex.Message}");
                return new T();
            }
        }

        // Save Config Helper
        private void SaveConfig<T>(string filePath, T config)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DEBUG]: Error saving config to {filePath}: {ex.Message}");
            }
        }
    }
}
