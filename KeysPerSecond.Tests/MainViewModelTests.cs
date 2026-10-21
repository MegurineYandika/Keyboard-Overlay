using System.Diagnostics;
using KeysPerSecond.Models;
using KeysPerSecond.ViewModels;
using Xunit;

namespace KeysPerSecond.Tests
{
    public class MainViewModelTests
    {
        [Fact]
        public void InitialState_ReflectsConfig()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);

            Assert.Equal(0, vm.CurrentKps);
            Assert.Equal(0, vm.MaxKps);
            Assert.Equal(0, vm.TotalPresses);
            Assert.False(vm.IsEditMode);
            Assert.Equal(4, vm.TrackedKeys.Count);
        }

        [Fact]
        public void OnKeyStrokeDown_IncrementsTotalPresses_And_UpdatesKps()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            long freq = Stopwatch.Frequency;
            long now = 1000 * freq;

            vm.OnKeyStrokeDown(now);
            Assert.Equal(1, vm.TotalPresses);

            vm.UpdateKps(now);
            Assert.Equal(1, vm.CurrentKps);
            Assert.Equal(1, vm.MaxKps);

            vm.OnKeyStrokeDown(now + 100);
            vm.OnKeyStrokeDown(now + 200);
            Assert.Equal(3, vm.TotalPresses);

            vm.UpdateKps(now + 200);
            Assert.Equal(3, vm.CurrentKps);
            Assert.Equal(3, vm.MaxKps);
        }

        [Fact]
        public void ResetStats_ClearsMetricsAndKeyCounters()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            long freq = Stopwatch.Frequency;
            long now = 1000 * freq;

            vm.OnKeyStrokeDown(now);
            vm.TrackedKeys[0].IncrementPresses();
            vm.UpdateKps(now);

            Assert.Equal(1, vm.TotalPresses);
            Assert.Equal(1, vm.CurrentKps);
            Assert.Equal(1, vm.TrackedKeys[0].TotalPresses);

            vm.ResetStats();

            Assert.Equal(0, vm.TotalPresses);
            Assert.Equal(0, vm.CurrentKps);
            Assert.Equal(0, vm.MaxKps);
            Assert.Equal(0, vm.TrackedKeys[0].TotalPresses);
        }

        [Fact]
        public void ToggleEditMode_FlipsState()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);

            Assert.False(vm.IsEditMode);
            vm.ToggleEditMode();
            Assert.True(vm.IsEditMode);
            vm.ToggleEditMode();
            Assert.False(vm.IsEditMode);
        }

        [Fact]
        public void AddTrackedKey_AddsNewKeyAndFiresEvent()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            bool eventFired = false;
            vm.ConfigurationApplied += () => eventFired = true;

            vm.AddTrackedKey(new KeyBindingConfig { Key = "Space", Label = "SPC" });

            Assert.True(eventFired);
            Assert.Equal(5, vm.TrackedKeys.Count);
            Assert.Equal("SPC", vm.TrackedKeys.Last().Label);
            Assert.Equal(0x20, vm.TrackedKeys.Last().VirtualKey);
        }

        [Fact]
        public void RemoveTrackedKey_RemovesKeyAndFiresEvent()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            bool eventFired = false;
            vm.ConfigurationApplied += () => eventFired = true;

            var keyToRemove = vm.TrackedKeys[0];
            bool removed = vm.RemoveTrackedKey(keyToRemove);

            Assert.True(removed);
            Assert.True(eventFired);
            Assert.Equal(3, vm.TrackedKeys.Count);
            Assert.DoesNotContain(keyToRemove, vm.TrackedKeys);
        }

        [Fact]
        public void UpdateConfig_ReplacesConfigurationCompletely()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            bool eventFired = false;
            vm.ConfigurationApplied += () => eventFired = true;

            var newConfig = new AppConfig();
            newConfig.Style.AccentColor = "#ABCDEF";
            newConfig.TrackedKeys.Clear();
            newConfig.TrackedKeys.Add(new KeyBindingConfig { Key = "Q", Label = "Q", VirtualKey = 81 });

            vm.UpdateConfig(newConfig);

            Assert.True(eventFired);
            Assert.Equal("#ABCDEF", vm.Config.Style.AccentColor);
            Assert.Single(vm.TrackedKeys);
            Assert.Equal(81, vm.TrackedKeys[0].VirtualKey);
        }
    }
}

