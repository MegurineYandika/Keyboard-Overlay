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
    }
}
