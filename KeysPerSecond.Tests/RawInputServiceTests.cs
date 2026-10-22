using System.Diagnostics;
using System.Linq;
using KeysPerSecond.Models;
using KeysPerSecond.Native;
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
        public void Modifiers_ToggleEditMode_Via_F10_ScanCode0x44_TriggersAction()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            bool editToggled = false;
            var service = new RawInputService(vm, () => editToggled = true, () => { });

            // Press Ctrl + Shift
            service.SimulateRawKeyEvent(0x11, isDown: true);
            service.SimulateRawKeyEvent(0x10, isDown: true);

            // Raw input with F10 hardware scan code 0x44
            service.SimulateRawKeyEvent(0x79, isDown: true, flags: 0, makeCode: 0x44);
            Assert.True(editToggled);
        }

        [Fact]
        public void Modifiers_ToggleEditMode_Via_CtrlShiftE_Alias_TriggersAction()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            bool editToggled = false;
            var service = new RawInputService(vm, () => editToggled = true, () => { });

            // Press Ctrl + Shift
            service.SimulateRawKeyEvent(0x11, isDown: true);
            service.SimulateRawKeyEvent(0x10, isDown: true);

            // Press 'E' (0x45)
            service.SimulateRawKeyEvent(0x45, isDown: true);
            Assert.True(editToggled);
        }

        [Fact]
        public void RawInputService_Initializes_And_Disposes_Cleanly()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);
            var service = new RawInputService(vm, () => { }, () => { });

            var thread = new System.Threading.Thread(() =>
            {
                service.Start();
                Assert.NotEqual(IntPtr.Zero, service.WindowHandle);
                service.Dispose();
                Assert.Equal(IntPtr.Zero, service.WindowHandle);
            });
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join(5000);
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

        [Fact]
        public void DynamicKeyAdditionAndDeletion_UpdatesRawInputLookupTable()
        {
            var config = new AppConfig();
            config.TrackedKeys = new()
            {
                new KeyBindingConfig { Key = "Z", Label = "Z", VirtualKey = 90 },
                new KeyBindingConfig { Key = "X", Label = "X", VirtualKey = 88 }
            };

            var vm = new MainViewModel(config);
            var service = new RawInputService(vm, () => { }, () => { });

            // 1. Initially only Z (90) and X (88) are tracked.
            // Pressing C (67) should have no effect.
            service.SimulateRawKeyEvent(67, isDown: true);
            Assert.Equal(0, vm.TotalPresses);

            // 2. Add 'C' dynamically
            vm.AddTrackedKey(new KeyBindingConfig { Key = "C", Label = "C", VirtualKey = 67 });
            service.RebuildLookupTable();

            // Press 'C' now
            service.SimulateRawKeyEvent(67, isDown: true);
            Assert.Equal(1, vm.TotalPresses);
            var keyC = vm.TrackedKeys.First(k => k.VirtualKey == 67);
            Assert.Equal(1, keyC.TotalPresses);
            Assert.True(keyC.IsPressed);

            service.SimulateRawKeyEvent(67, isDown: false);
            Assert.False(keyC.IsPressed);

            // 3. Remove 'Z' dynamically
            var keyZ = vm.TrackedKeys.First(k => k.VirtualKey == 90);
            vm.RemoveTrackedKey(keyZ);
            service.RebuildLookupTable();

            // Press 'Z' now -> should NOT increment
            service.SimulateRawKeyEvent(90, isDown: true);
            Assert.Equal(1, vm.TotalPresses); // Still 1!

            // Press 'C' again -> increments
            service.SimulateRawKeyEvent(67, isDown: true);
            Assert.Equal(2, vm.TotalPresses);
            Assert.Equal(2, keyC.TotalPresses);
        }
    }
}

