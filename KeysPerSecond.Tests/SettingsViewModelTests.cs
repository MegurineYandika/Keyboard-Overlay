using System.Linq;
using System.Threading;
using System.Windows;
using KeysPerSecond.Models;
using KeysPerSecond.ViewModels;
using Xunit;

namespace KeysPerSecond.Tests
{
    public class SettingsViewModelTests
    {
        [Fact]
        public void Initialize_PopulatesFromConfig()
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);

            Assert.Equal(config.TrackedKeys.Count, vm.Keys.Count);
            Assert.Equal(config.Style.AccentColor, vm.AccentColor);
            Assert.Equal(config.Style.KeyWidth, vm.KeyWidth);
            Assert.Equal("Horizontal", vm.Orientation);
            Assert.True(vm.IsHorizontal);
            Assert.False(vm.IsVertical);
        }

        [Fact]
        public void AddCurrentKey_AddsValidKey_ToCollection()
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);
            int initialCount = vm.Keys.Count;

            vm.InputKey = "Space";
            vm.InputLabel = "SPACE";
            vm.InputAccentColor = "#FF007F";

            bool success = vm.AddCurrentKey();

            Assert.True(success);
            Assert.Equal(initialCount + 1, vm.Keys.Count);
            var added = vm.Keys.Last();
            Assert.Equal("Space", added.Key);
            Assert.Equal("SPACE", added.Label);
            Assert.Equal(0x20, added.VirtualKey);
            Assert.Equal("#FF007F", added.AccentColor);
        }

        [Fact]
        public void AddCurrentKey_RejectsEmptyOrInvalidKey()
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);
            int initialCount = vm.Keys.Count;

            vm.InputKey = "";
            bool success = vm.AddCurrentKey();

            Assert.False(success);
            Assert.Equal(initialCount, vm.Keys.Count);
        }

        [Fact]
        public void DeleteKey_RemovesKeyFromCollection()
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);
            int initialCount = vm.Keys.Count;

            var keyToDelete = vm.Keys[1];
            vm.DeleteKey(keyToDelete);

            Assert.Equal(initialCount - 1, vm.Keys.Count);
            Assert.DoesNotContain(keyToDelete, vm.Keys);
        }

        [Fact]
        public void MoveKeyUpAndDown_ChangesOrderProperly()
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);

            var firstKey = vm.Keys[0];
            var secondKey = vm.Keys[1];

            // Move second key up
            vm.MoveKeyUp(secondKey);
            Assert.Equal(secondKey, vm.Keys[0]);
            Assert.Equal(firstKey, vm.Keys[1]);

            // Move secondKey down again
            vm.MoveKeyDown(secondKey);
            Assert.Equal(firstKey, vm.Keys[0]);
            Assert.Equal(secondKey, vm.Keys[1]);
        }

        [Theory]
        [InlineData("osu_2k", 2)]
        [InlineData("osu_4k", 4)]
        [InlineData("osu_7k", 7)]
        [InlineData("wasd", 4)]
        [InlineData("arrows", 4)]
        public void ApplyPreset_AppliesCorrectKeyCounts(string preset, int expectedCount)
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);

            vm.ApplyPreset(preset);

            Assert.Equal(expectedCount, vm.Keys.Count);
        }

        [Fact]
        public void SetCapturedKey_UpdatesInputState()
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);
            vm.IsCapturingKey = true;

            vm.SetCapturedKey(0x41, "A");

            Assert.False(vm.IsCapturingKey);
            Assert.Equal(0x41, vm.InputVirtualKey);
            Assert.Equal("A", vm.InputKey);
            Assert.Equal("A", vm.InputLabel);
        }

        [Fact]
        public void WindowScalingAndDimensions_UpdateAndBuildAccurately()
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);

            Assert.Equal(1.0, vm.WindowScale);
            Assert.True(vm.AutoFit);

            // Change scale
            vm.SetScale(1.5);
            Assert.Equal(1.5, vm.WindowScale);

            // Change custom dimensions
            vm.AutoFit = false;
            vm.CustomWidth = 520;
            vm.CustomHeight = 220;

            var result = vm.BuildResultConfig();

            Assert.Equal(1.5, result.Window.Scale);
            Assert.False(result.Window.AutoFit);
            Assert.Equal(520, result.Window.CustomWidth);
            Assert.Equal(220, result.Window.CustomHeight);
        }

        [Fact]
        public void HoldDurationAndBarSettings_UpdateAndBuildAccurately()
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);

            Assert.True(vm.ShowHoldDuration);
            Assert.True(vm.ShowHoldBar);
            Assert.Equal(300.0, vm.MaxHoldDurationMs);
            Assert.Equal(600.0, vm.HoldBarDecayMs);

            vm.ShowHoldDuration = false;
            vm.ShowHoldBar = false;
            vm.MaxHoldDurationMs = 450.0;
            vm.HoldBarDecayMs = 800.0;

            var result = vm.BuildResultConfig();

            Assert.False(result.Style.ShowHoldDuration);
            Assert.False(result.Style.ShowHoldBar);
            Assert.Equal(450.0, result.Style.MaxHoldDurationMs);
            Assert.Equal(800.0, result.Style.HoldBarDecayMs);
        }

        [Fact]
        public void BuildResultConfig_ReflectsAllModifications()
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);

            vm.AccentColor = "#123456";
            vm.KeyWidth = 72;
            vm.Orientation = "Vertical";
            vm.ToggleEditModeKey = "Ctrl+Alt+F12";
            vm.Keys.Clear();
            vm.Keys.Add(new KeyBindingConfig { Key = "Q", Label = "Q", VirtualKey = 81 });

            var result = vm.BuildResultConfig();

            Assert.Equal("#123456", result.Style.AccentColor);
            Assert.Equal(72, result.Style.KeyWidth);
            Assert.Equal("Vertical", result.Style.Orientation);
            Assert.Equal("Ctrl+Alt+F12", result.ToggleEditModeKey);
            Assert.Single(result.TrackedKeys);
            Assert.Equal(81, result.TrackedKeys[0].VirtualKey);
        }

        [Fact]
        public void WaterfallSettings_BuildResultConfig_PreservesAndAppliesValues()
        {
            var config = new AppConfig();
            var vm = new SettingsViewModel(config);

            Assert.True(vm.WaterfallEnabled);
            Assert.True(vm.IsUpscroll);
            Assert.False(vm.IsDownscroll);
            Assert.Equal(0.8, vm.WaterfallSpeed);
            Assert.Equal(1500.0, vm.WaterfallMaxVisibleMs);
            Assert.Equal(0.75, vm.WaterfallOpacity);
            Assert.Equal(4.0, vm.WaterfallCornerRadius);
            Assert.True(vm.WaterfallUseKeyAccentColors);
            Assert.Equal(200.0, vm.WaterfallHeight);
            Assert.False(vm.TransparentMode);
            Assert.False(vm.MatchPatternRatio);

            // Change values
            vm.WaterfallEnabled = false;
            vm.IsDownscroll = true;
            vm.WaterfallSpeed = 1.4;
            vm.WaterfallMaxVisibleMs = 2200.0;
            vm.WaterfallOpacity = 0.9;
            vm.WaterfallCornerRadius = 8.0;
            vm.WaterfallUseKeyAccentColors = false;
            vm.WaterfallHeight = 280.0;
            vm.TransparentMode = true;
            vm.MatchPatternRatio = true;

            Assert.True(vm.IsDownscroll);
            Assert.False(vm.IsUpscroll);
            Assert.Equal("Downscroll", vm.WaterfallDirection);

            var result = vm.BuildResultConfig();
            Assert.False(result.Waterfall.IsEnabled);
            Assert.Equal("Downscroll", result.Waterfall.ScrollDirection);
            Assert.Equal(1.4, result.Waterfall.ScrollSpeed);
            Assert.Equal(2200.0, result.Waterfall.MaxVisibleDurationMs);
            Assert.Equal(0.9, result.Waterfall.BarOpacity);
            Assert.Equal(8.0, result.Waterfall.BarCornerRadius);
            Assert.False(result.Waterfall.UseKeyAccentColors);
            Assert.True(result.Waterfall.TransparentMode);
            Assert.True(result.Waterfall.MatchPatternRatio);
        }

        [Fact]
        public void PatternPreset_CalculatesHeightTwiceOfWidth()
        {
            var config = new AppConfig();
            config.Style.KeyWidth = 64;
            config.TrackedKeys = new()
            {
                new KeyBindingConfig { Key = "D" },
                new KeyBindingConfig { Key = "F" },
                new KeyBindingConfig { Key = "J" },
                new KeyBindingConfig { Key = "K" }
            };

            var vm = new SettingsViewModel(config);

            // Total width for 4 keys with width 64 is: 4 * (64 + 8) = 288
            double expectedWidth = 4 * (64 + 8);
            double expectedHeight = expectedWidth * 2.0; // 576.0

            Assert.Equal(expectedWidth, vm.CurrentKeysWidth);
            Assert.Equal(expectedHeight, vm.CalculatedPatternHeight);

            // Apply preset height
            vm.ApplyPatternHeightPreset();
            Assert.Equal(expectedHeight, vm.WaterfallHeight);

            // Apply transparent + 2x pattern combo preset
            vm.WaterfallEnabled = false;
            vm.TransparentMode = false;
            vm.ApplyTransparentPatternPreset();

            Assert.True(vm.WaterfallEnabled);
            Assert.True(vm.TransparentMode);
            Assert.Equal(expectedHeight, vm.WaterfallHeight);

            // Test auto-lock MatchPatternRatio
            vm.MatchPatternRatio = true;
            vm.KeyWidth = 72; // changes global key width
            double newExpectedWidth = 4 * (72 + 8); // 320.0
            double newExpectedHeight = newExpectedWidth * 2.0; // 640.0

            Assert.Equal(newExpectedHeight, vm.WaterfallHeight);
        }

        [Fact]
        public void SettingsWindow_InitializesWithoutException_OnSTAThread()
        {
            Exception? caughtException = null;

            var staThread = new Thread(() =>
            {
                try
                {
                    WpfTestHelper.EnsureApplication();

                    var config = new AppConfig();
                    var window = new SettingsWindow(config);
                    Assert.NotNull(window);
                    Assert.NotNull(window.DataContext);

                    window.Close();
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join(5000);

            Assert.Null(caughtException);
        }
    }
}
