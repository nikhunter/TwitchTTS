using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Windows;

namespace TwitchTTS
{
    public partial class SettingsWindow : Window
    {
        // List to hold the settings
        private List<SettingItem> _settingsList;

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load settings into the list using SettingItem
            _settingsList = ConfigurationManager.AppSettings
                .AllKeys
                .Select(key => new SettingItem { Key = key, Value = ConfigurationManager.AppSettings[key] })
                .ToList();

            // Bind the list to the ItemsControl
            SettingsItemsControl.ItemsSource = _settingsList;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
            Close();
        }

        private void SaveSettings()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = config.AppSettings.Settings;

            // Update settings based on the list
            foreach (var setting in _settingsList)
            {
                if (settings[setting.Key] == null)
                {
                    settings.Add(setting.Key, setting.Value);
                }
                else
                {
                    settings[setting.Key].Value = setting.Value;
                }
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }

    public class SettingItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
