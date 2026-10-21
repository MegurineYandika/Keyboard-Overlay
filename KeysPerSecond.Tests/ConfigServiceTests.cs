using System.IO;
using KeysPerSecond.Models;
using KeysPerSecond.Services;
using Xunit;

namespace KeysPerSecond.Tests
{
    public class ConfigServiceTests
    {
        [Fact]
        public void LoadConfig_CreatesDefaultWhenMissing()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_config_{System.Guid.NewGuid()}.json");

            try
            {
                var service = new ConfigService(tempFile);
                var config = service.LoadConfig();

                Assert.NotNull(config);
                Assert.True(config.TrackedKeys.Count >= 2);
                Assert.Equal(90, config.TrackedKeys[0].VirtualKey); // 'Z'
                Assert.Equal("Z", config.TrackedKeys[0].Label);
                Assert.True(File.Exists(tempFile));
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void SaveAndReloadConfig_PersistsValuesAccurately()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_config_{System.Guid.NewGuid()}.json");

            try
            {
                var service = new ConfigService(tempFile);
                var config = new AppConfig();
                config.Window.Left = 450.5;
                config.Window.Top = 320.0;
                config.Style.AccentColor = "#FF5500";
                config.TrackedKeys.Clear();
                config.TrackedKeys.Add(new KeyBindingConfig { Key = "D", Label = "D", VirtualKey = 68 });
                config.TrackedKeys.Add(new KeyBindingConfig { Key = "F", Label = "F", VirtualKey = 70 });
                config.TrackedKeys.Add(new KeyBindingConfig { Key = "J", Label = "J", VirtualKey = 74 });
                config.TrackedKeys.Add(new KeyBindingConfig { Key = "K", Label = "K", VirtualKey = 75 });

                bool saved = service.SaveConfig(config);
                Assert.True(saved);

                var loaded = service.LoadConfig();
                Assert.Equal(450.5, loaded.Window.Left);
                Assert.Equal(320.0, loaded.Window.Top);
                Assert.Equal("#FF5500", loaded.Style.AccentColor);
                Assert.Equal(4, loaded.TrackedKeys.Count);
                Assert.Equal(68, loaded.TrackedKeys[0].VirtualKey);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void NormalizeConfig_ResolvesMissingVirtualKeysAndLabels()
        {
            var config = new AppConfig();
            config.TrackedKeys = new()
            {
                new KeyBindingConfig { Key = "Space", Label = null, VirtualKey = 0 },
                new KeyBindingConfig { Key = "Z", Label = "", VirtualKey = 0 }
            };

            ConfigService.NormalizeConfig(config);

            Assert.Equal(0x20, config.TrackedKeys[0].VirtualKey);
            Assert.Equal("SPC", config.TrackedKeys[0].Label);

            Assert.Equal(90, config.TrackedKeys[1].VirtualKey);
            Assert.Equal("Z", config.TrackedKeys[1].Label);
        }
    }
}
