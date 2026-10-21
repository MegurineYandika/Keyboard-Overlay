using System.Diagnostics;
using System.Linq;
using KeysPerSecond.Models;
using KeysPerSecond.Services;
using KeysPerSecond.ViewModels;
using Xunit;

namespace KeysPerSecond.Tests
{
    public class RawInputServiceTests
    {
        [Fact]
        public void DispatchKey_PressAndRelease_UpdatesViewModelCorrectly()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            var service = new RawInputService(vm, () => { }, () => { });
            var keyZ = vm.TrackedKeys.First(k => k.VirtualKey == 90);

            Assert.False(keyZ.IsPressed);
            Assert.Equal(0, keyZ.TotalPresses);

            // Press Z
            service.SimulateRawKeyEvent(90, isDown: true);

            Assert.True(keyZ.IsPressed);
            Assert.Equal(1, keyZ.TotalPresses);
            Assert.Equal(1, vm.TotalPresses);

            // Release Z
            service.SimulateRawKeyEvent(90, isDown: false);

            Assert.False(keyZ.IsPressed);
            Assert.Equal(1, keyZ.TotalPresses);
            Assert.Equal(1, vm.TotalPresses);
        }

        [Fact]
        public void DispatchKey_IgnoresTypematicRepeat_WhileKeyIsHeldDown()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);

            var service = new RawInputService(vm, () => { }, () => { });
            var keyZ = vm.TrackedKeys.First(k => k.VirtualKey == 90);

            // Initial key press
            service.SimulateRawKeyEvent(90, isDown: true);
            Assert.Equal(1, keyZ.TotalPresses);
            Assert.Equal(1, vm.TotalPresses);

            // Typematic auto-repeat events while holding the key down
            for (int i = 0; i < 50; i++)
            {
                service.SimulateRawKeyEvent(90, isDown: true);
            }

            // Must NOT increment count for held down keys!
            Assert.Equal(1, keyZ.TotalPresses);
            Assert.Equal(1, vm.TotalPresses);

            // Release
            service.SimulateRawKeyEvent(90, isDown: false);

            // Press again
            service.SimulateRawKeyEvent(90, isDown: true);
            Assert.Equal(2, keyZ.TotalPresses);
            Assert.Equal(2, vm.TotalPresses);
        }

        [Fact]
        public void AlternatingStreaming_CalculatesTotalAndKpsAccurately()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            var service = new RawInputService(vm, () => { }, () => { });

            var keyZ = vm.TrackedKeys.First(k => k.VirtualKey == 90);
            var keyX = vm.TrackedKeys.First(k => k.VirtualKey == 88);

            // Simulate alternating streaming: Z, X, Z, X ... for 10 cycles (20 strokes)
            for (int i = 0; i < 10; i++)
            {
                service.SimulateRawKeyEvent(90, isDown: true);
                service.SimulateRawKeyEvent(90, isDown: false);

                service.SimulateRawKeyEvent(88, isDown: true);
                service.SimulateRawKeyEvent(88, isDown: false);
            }

            Assert.Equal(10, keyZ.TotalPresses);
            Assert.Equal(10, keyX.TotalPresses);
            Assert.Equal(20, vm.TotalPresses);

            vm.UpdateKps(Stopwatch.GetTimestamp());
            Assert.Equal(20, vm.CurrentKps);
            Assert.Equal(20, vm.MaxKps);
        }

        [Fact]
        public void Modifiers_ToggleEditMode_TriggersAction()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            bool editToggled = false;
            var service = new RawInputService(vm, () => editToggled = true, () => { });

            // Press Ctrl
            service.SimulateRawKeyEvent(0x11, isDown: true);
            Assert.False(editToggled);

            // Press Shift
            service.SimulateRawKeyEvent(0x10, isDown: true);
            Assert.False(editToggled);

            // Press F10 (VK 0x79)
            service.SimulateRawKeyEvent(0x79, isDown: true);
            Assert.True(editToggled);

            // Release F10, Shift, Ctrl
            service.SimulateRawKeyEvent(0x79, isDown: false);
            service.SimulateRawKeyEvent(0x10, isDown: false);
            service.SimulateRawKeyEvent(0x11, isDown: false);
        }

        [Fact]
        public void Modifiers_ResetStats_TriggersAction()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            bool resetTriggered = false;

            var service = new RawInputService(vm, () => { }, () => resetTriggered = true);

            // Hold Ctrl + Shift and press R (0x52)
            service.SimulateRawKeyEvent(0x11, isDown: true);
            service.SimulateRawKeyEvent(0x10, isDown: true);
            service.SimulateRawKeyEvent(0x52, isDown: true);

            Assert.True(resetTriggered);
        }
    }
}
