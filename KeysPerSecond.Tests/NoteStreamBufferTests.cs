using System;
using System.Diagnostics;
using KeysPerSecond.Models;
using KeysPerSecond.Services;
using KeysPerSecond.ViewModels;
using Xunit;

namespace KeysPerSecond.Tests
{
    public class NoteStreamBufferTests
    {
        [Fact]
        public void Note_Starts_And_Stretches_While_Key_Is_Active()
        {
            var buffer = new NoteStreamBuffer(capacity: 1024, initialColumns: 4);
            long startTick = 100_000;

            bool started = buffer.OnKeyDown(columnIndex: 0, timestamp: startTick);
            Assert.True(started);
            Assert.True(buffer.IsColumnActive(0));
            Assert.Equal(1, buffer.ActiveCount);
            Assert.Equal(0, buffer.CompletedCount);

            bool retrieved = buffer.TryGetActiveNote(0, out NoteSegment note);
            Assert.True(retrieved);
            Assert.Equal(0, note.ColumnIndex);
            Assert.Equal(startTick, note.StartTimestamp);
            Assert.True(note.IsActive);

            // Verify active note duration/length increases over simulated elapsed ticks
            long tick1 = startTick + 15_000;
            long tick2 = startTick + 45_000;

            long duration1 = note.GetDurationTicks(tick1);
            long duration2 = note.GetDurationTicks(tick2);

            Assert.Equal(15_000, duration1);
            Assert.Equal(45_000, duration2);
            Assert.True(duration2 > duration1);
        }

        [Fact]
        public void Note_Finalizes_On_KeyUp_With_Correct_Duration()
        {
            var buffer = new NoteStreamBuffer(capacity: 1024, initialColumns: 4);
            long startTick = 200_000;
            long releaseTick = 265_000;

            buffer.OnKeyDown(columnIndex: 2, timestamp: startTick);
            bool finalized = buffer.OnKeyUp(columnIndex: 2, timestamp: releaseTick);

            Assert.True(finalized);
            Assert.False(buffer.IsColumnActive(2));
            Assert.Equal(0, buffer.ActiveCount);
            Assert.Equal(1, buffer.CompletedCount);

            ref readonly NoteSegment completed = ref buffer.GetCompletedNote(0);
            Assert.Equal(2, completed.ColumnIndex);
            Assert.Equal(startTick, completed.StartTimestamp);
            Assert.Equal(releaseTick, completed.EndTimestamp);
            Assert.False(completed.IsActive);
            Assert.Equal(65_000, completed.GetDurationTicks(999_999));
        }

        [Fact]
        public void Typematic_Repeat_Does_Not_Spawn_Multiple_Notes()
        {
            var buffer = new NoteStreamBuffer(capacity: 1024, initialColumns: 4);
            long initialTick = 50_000;

            // Initial key down
            bool firstDown = buffer.OnKeyDown(columnIndex: 1, timestamp: initialTick);
            Assert.True(firstDown);

            // Subsequent repeated key down events from OS typematic auto-repeat
            bool repeat1 = buffer.OnKeyDown(columnIndex: 1, timestamp: initialTick + 2_000);
            bool repeat2 = buffer.OnKeyDown(columnIndex: 1, timestamp: initialTick + 4_000);
            bool repeat3 = buffer.OnKeyDown(columnIndex: 1, timestamp: initialTick + 6_000);

            Assert.False(repeat1);
            Assert.False(repeat2);
            Assert.False(repeat3);

            // Ensure no duplicate notes were spawned or committed
            Assert.Equal(1, buffer.ActiveCount);
            Assert.Equal(0, buffer.CompletedCount);

            Assert.True(buffer.TryGetActiveNote(1, out NoteSegment note));
            Assert.Equal(initialTick, note.StartTimestamp); // Start timestamp preserved
        }

        [Fact]
        public void Offscreen_Notes_Are_Recycled_Without_Memory_Leak()
        {
            var buffer = new NoteStreamBuffer(capacity: 16, initialColumns: 4);
            long maxDurationTicks = 20_000;

            // Note 1: released at 12,000
            buffer.OnKeyDown(0, 10_000);
            buffer.OnKeyUp(0, 12_000);

            // Note 2: released at 22,000
            buffer.OnKeyDown(0, 20_000);
            buffer.OnKeyUp(0, 22_000);

            // Note 3: released at 32,000
            buffer.OnKeyDown(0, 30_000);
            buffer.OnKeyUp(0, 32_000);

            Assert.Equal(3, buffer.CompletedCount);
            int initialTail = buffer.Tail;

            // At now = 35,000:
            // Note 1: elapsed since end = 35,000 - 12,000 = 23,000 >= 20,000 (EXPIRED)
            // Note 2: elapsed since end = 35,000 - 22,000 = 13,000 < 20,000 (VISIBLE)
            // Note 3: elapsed since end = 35,000 - 32,000 = 3,000 < 20,000 (VISIBLE)
            buffer.CullExpiredNotes(currentTimestamp: 35_000, maxDurationTicks: maxDurationTicks);

            Assert.Equal(2, buffer.CompletedCount);
            Assert.NotEqual(initialTail, buffer.Tail); // Tail pointer advanced

            ref readonly NoteSegment oldestVisible = ref buffer.GetCompletedNote(0);
            Assert.Equal(20_000, oldestVisible.StartTimestamp);
            Assert.Equal(22_000, oldestVisible.EndTimestamp);

            // Saturation test: verify ring buffer handles 500+ notes without exceeding capacity or leaking
            for (int i = 0; i < 500; i++)
            {
                long t = 40_000 + (i * 100);
                buffer.OnKeyDown(0, t);
                buffer.OnKeyUp(0, t + 50);
            }

            Assert.Equal(buffer.Capacity, buffer.CompletedCount);
        }

        [Fact]
        public void MultiKey_Simultaneous_Presses_Track_Independent_Columns()
        {
            var buffer = new NoteStreamBuffer(capacity: 1024, initialColumns: 4);
            long chordTick = 100_000;

            // 4-key simultaneous chord press (jump-jack pattern)
            buffer.OnKeyDown(0, chordTick);
            buffer.OnKeyDown(1, chordTick);
            buffer.OnKeyDown(2, chordTick);
            buffer.OnKeyDown(3, chordTick);

            Assert.Equal(4, buffer.ActiveCount);
            for (int col = 0; col < 4; col++)
            {
                Assert.True(buffer.IsColumnActive(col));
                Assert.True(buffer.TryGetActiveNote(col, out NoteSegment note));
                Assert.Equal(col, note.ColumnIndex);
                Assert.Equal(chordTick, note.StartTimestamp);
            }

            // Release col 1 and col 3 earlier
            buffer.OnKeyUp(1, chordTick + 15_000);
            buffer.OnKeyUp(3, chordTick + 25_000);

            Assert.Equal(2, buffer.ActiveCount);
            Assert.Equal(2, buffer.CompletedCount);

            // Cols 0 and 2 are still active and untouched
            Assert.True(buffer.IsColumnActive(0));
            Assert.True(buffer.IsColumnActive(2));
            Assert.False(buffer.IsColumnActive(1));
            Assert.False(buffer.IsColumnActive(3));

            // Verify completed notes maintain independent timestamps
            ref readonly NoteSegment noteCol1 = ref buffer.GetCompletedNote(0);
            ref readonly NoteSegment noteCol3 = ref buffer.GetCompletedNote(1);

            Assert.Equal(1, noteCol1.ColumnIndex);
            Assert.Equal(15_000, noteCol1.GetDurationTicks(999_999));

            Assert.Equal(3, noteCol3.ColumnIndex);
            Assert.Equal(25_000, noteCol3.GetDurationTicks(999_999));

            // Finally release cols 0 and 2
            buffer.OnKeyUp(0, chordTick + 35_000);
            buffer.OnKeyUp(2, chordTick + 45_000);

            Assert.Equal(0, buffer.ActiveCount);
            Assert.Equal(4, buffer.CompletedCount);
        }

        [Fact]
        public void Buffer_Clear_Resets_Active_And_Completed_Segments()
        {
            var buffer = new NoteStreamBuffer(capacity: 1024, initialColumns: 4);
            buffer.OnKeyDown(0, 1000);
            buffer.OnKeyDown(1, 1000);
            buffer.OnKeyUp(0, 2000);

            Assert.Equal(1, buffer.ActiveCount);
            Assert.Equal(1, buffer.CompletedCount);

            buffer.Clear();

            Assert.Equal(0, buffer.ActiveCount);
            Assert.Equal(0, buffer.CompletedCount);
            Assert.False(buffer.IsColumnActive(0));
            Assert.False(buffer.IsColumnActive(1));
        }

        [Fact]
        public void WaterfallConfig_Clone_Creates_Independent_Copy()
        {
            var original = new WaterfallConfig
            {
                IsEnabled = true,
                ScrollDirection = "Downscroll",
                ScrollSpeed = 1.25,
                MaxVisibleDurationMs = 2000.0,
                BarOpacity = 0.9,
                BarCornerRadius = 6.0,
                UseKeyAccentColors = false,
                Height = 250.0
            };

            var clone = original.Clone();

            Assert.Equal(original.IsEnabled, clone.IsEnabled);
            Assert.Equal(original.ScrollDirection, clone.ScrollDirection);
            Assert.Equal(original.ScrollSpeed, clone.ScrollSpeed);
            Assert.Equal(original.MaxVisibleDurationMs, clone.MaxVisibleDurationMs);
            Assert.Equal(original.BarOpacity, clone.BarOpacity);
            Assert.Equal(original.BarCornerRadius, clone.BarCornerRadius);
            Assert.Equal(original.UseKeyAccentColors, clone.UseKeyAccentColors);
            Assert.Equal(original.Height, clone.Height);

            clone.ScrollSpeed = 0.5;
            Assert.NotEqual(original.ScrollSpeed, clone.ScrollSpeed);
        }

        [Fact]
        public void MainViewModel_KeyItem_Dispatches_To_WaterfallBuffer()
        {
            var config = new AppConfig();
            var vm = new MainViewModel(config);

            Assert.Equal(0, vm.WaterfallBuffer.ActiveCount);
            Assert.Equal(0, vm.WaterfallBuffer.CompletedCount);

            long downTick = 10_000;
            long upTick = 15_000;

            // Trigger physical down on key 0 (Z)
            vm.TrackedKeys[0].OnKeyDown(downTick);

            Assert.True(vm.WaterfallBuffer.IsColumnActive(0));
            Assert.Equal(1, vm.WaterfallBuffer.ActiveCount);

            // Trigger physical up on key 0 (Z)
            vm.TrackedKeys[0].OnKeyUp(upTick);

            Assert.False(vm.WaterfallBuffer.IsColumnActive(0));
            Assert.Equal(0, vm.WaterfallBuffer.ActiveCount);
            Assert.Equal(1, vm.WaterfallBuffer.CompletedCount);

            ref readonly NoteSegment note = ref vm.WaterfallBuffer.GetCompletedNote(0);
            Assert.Equal(0, note.ColumnIndex);
            Assert.Equal(downTick, note.StartTimestamp);
            Assert.Equal(upTick, note.EndTimestamp);
        }
    }
}
