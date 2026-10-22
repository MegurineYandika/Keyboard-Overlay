using System;
using System.IO;
using System.Text.Json;
using KeysPerSecond.Models;

namespace KeysPerSecond.Services
{
    public sealed class ConfigService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _configFilePath;

        public ConfigService(string? customPath = null)
        {
            if (!string.IsNullOrWhiteSpace(customPath))
            {
                _configFilePath = customPath;
            }
            else
            {
                // Prefer config.json in base directory, or current directory
                string baseDirFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                string currentDirFile = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

                if (File.Exists(baseDirFile))
                {
                    _configFilePath = baseDirFile;
                }
                else if (File.Exists(currentDirFile))
                {
                    _configFilePath = currentDirFile;
                }
                else
                {
                    _configFilePath = baseDirFile;
                }
            }
        }

        public string ConfigFilePath => _configFilePath;

        /// <summary>
        /// Loads the configuration or returns a new default configuration if the file does not exist or fails to parse.
        /// </summary>
        public AppConfig LoadConfig()
        {
            AppConfig config;

            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
                }
                else
                {
                    config = new AppConfig();
                    SaveConfig(config);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigService] Error loading config: {ex.Message}");
                config = new AppConfig();
            }

            NormalizeConfig(config);
            return config;
        }

        /// <summary>
        /// Saves the configuration to disk.
        /// </summary>
        public bool SaveConfig(AppConfig config)
        {
            try
            {
                string dir = Path.GetDirectoryName(_configFilePath)!;
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(_configFilePath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigService] Error saving config: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ensures all keys have a valid VirtualKey code, label, and colors.
        /// </summary>
        public static void NormalizeConfig(AppConfig config)
        {
            config.Window ??= new WindowConfig();
            config.Style ??= new StyleConfig();
            config.Waterfall ??= new WaterfallConfig();

            if (config.TrackedKeys == null || config.TrackedKeys.Count == 0)
            {
                config.TrackedKeys = new()
                {
                    new KeyBindingConfig { Key = "Z", Label = "Z", VirtualKey = 90, AccentColor = config.Style.AccentColor },
                    new KeyBindingConfig { Key = "X", Label = "X", VirtualKey = 88, AccentColor = config.Style.AccentColor },
                    new KeyBindingConfig { Key = "C", Label = "C", VirtualKey = 67, AccentColor = config.Style.PressedColor },
                    new KeyBindingConfig { Key = "V", Label = "V", VirtualKey = 86, AccentColor = config.Style.PressedColor }
                };
            }

            foreach (var keyConfig in config.TrackedKeys)
            {
                if (keyConfig.VirtualKey == 0)
                {
                    keyConfig.VirtualKey = KeyToVkHelper.ResolveVirtualKey(keyConfig.Key);
                }

                if (string.IsNullOrWhiteSpace(keyConfig.Label))
                {
                    keyConfig.Label = KeyToVkHelper.GetDefaultLabel(keyConfig.VirtualKey);
                }

                if (string.IsNullOrWhiteSpace(keyConfig.AccentColor))
                {
                    keyConfig.AccentColor = config.Style.AccentColor;
                }
            }
        }
    }
}

