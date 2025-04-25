// --- Helpers/SettingsManager.cs ---
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms; // For MessageBox
using Avatar_Elements.Data; // Your data classes namespace
using Newtonsoft.Json; // Make sure NuGet package is installed

namespace Avatar_Elements.Helpers {
    public static class SettingsManager {
        // Store settings in %AppData%\SimpleDynamicAvatar
        private static string settingsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SimpleDynamicAvatar");
        private static string settingsFilePath = Path.Combine(settingsFolderPath, "settings.json");

        public static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(settingsFilePath))
                {
                    string json = File.ReadAllText(settingsFilePath);
                    // Ensure nulls are handled gracefully during deserialization
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ObjectCreationHandling = ObjectCreationHandling.Replace // Important for lists
                    };
                    AppSettings appSettings = JsonConvert.DeserializeObject<AppSettings>(json, settings);

                    // Perform sanity checks after loading
                    if (appSettings == null) return new AppSettings();
                    appSettings.Profiles = appSettings.Profiles ?? new List<AvatarProfile>();
                    appSettings.GlobalHideShowHotkey = appSettings.GlobalHideShowHotkey ?? new HotkeyConfig();
                    foreach (var profile in appSettings.Profiles)
                    {
                        profile.Hotkey = profile.Hotkey ?? new HotkeyConfig();
                        // Initialize other potentially null properties if added later
                    }
                    return appSettings;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                MessageBox.Show($"Failed to load settings. Default settings will be used.\nError: {ex.Message}",
                                "Load Settings Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            // Return default settings if file doesn't exist or error occurs
            return new AppSettings();
        }

        public static void SaveSettings(AppSettings settings)
        {
            if (settings == null) return; // Don't save null settings

            try
            {
                // Ensure directory exists
                if (!Directory.Exists(settingsFolderPath))
                {
                    Directory.CreateDirectory(settingsFolderPath);
                }

                JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented, // Make readable
                    NullValueHandling = NullValueHandling.Ignore // Don't write null properties
                };
                string json = JsonConvert.SerializeObject(settings, jsonSettings);
                File.WriteAllText(settingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
                MessageBox.Show($"Error saving settings:\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}