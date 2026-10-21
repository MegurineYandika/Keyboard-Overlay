using System.Diagnostics;
using KeysPerSecond.Services;
using Xunit;

namespace KeysPerSecond.Tests
{
    public class KpsSlidingWindowTests
    {
        [Fact]
        public void RecordPress_IncreasesCount_WithinWindow()
        {
            var window = new KpsSlidingWindow(64);
            long freq = Stopwatch.Frequency;
            long t0 = 1000 * freq; // 1000 seconds in ticks

            window.RecordPress(t0);
            window.RecordPress(t0 + (freq / 4));  // +250ms
            window.RecordPress(t0 + (freq / 2));  // +500ms

            int count = window.UpdateAndGetCount(t0 + (freq / 2), freq);
            Assert.Equal(3, count);
        }

        [Fact]
        public void UpdateAndGetCount_EvictsTimestampsOlderThanWindow()
        {
            var window = new KpsSlidingWindow(64);
            long freq = Stopwatch.Frequency;
            long t0 = 1000 * freq;

            window.RecordPress(t0);                  // at t = 0ms
            window.RecordPress(t0 + (freq / 2));     // at t = 500ms
            window.RecordPress(t0 + (freq * 3 / 4)); // at t = 750ms

            // At t = 1100ms, the press at t=0ms should be evicted (older than 1000ms)
            long tCurrent = t0 + (long)(freq * 1.1);
            int count = window.UpdateAndGetCount(tCurrent, freq);

            Assert.Equal(2, count);

            // At t = 1800ms, all presses should be evicted
            long tLater = t0 + (long)(freq * 1.8);
            int finalCount = window.UpdateAndGetCount(tLater, freq);
            Assert.Equal(0, finalCount);
        }

        [Fact]
        public void CircularBuffer_HandlesWraparound_WithoutDataCorruption()
        {
            // Small buffer of size 8
            var window = new KpsSlidingWindow(8);
            long freq = 1000; // 1000 ticks per second

            // Record 20 presses, all within the same 500ms window
            for (int i = 0; i < 20; i++)
            {
                window.RecordPress(10000 + (i * 10));
            }

            // Buffer capacity is 8, so it should retain the last 8 presses
            Assert.Equal(8, window.CurrentCount);

            int count = window.UpdateAndGetCount(10000 + 200, freq);
            Assert.Equal(8, count);
        }

        [Fact]
        public void Reset_ClearsAllRecordedPresses()
        {
            var window = new KpsSlidingWindow(64);
            long freq = Stopwatch.Frequency;
            long t0 = 1000 * freq;

            window.RecordPress(t0);
            window.RecordPress(t0 + 100);

            Assert.Equal(2, window.UpdateAndGetCount(t0 + 200, freq));

            window.Reset();
            Assert.Equal(0, window.CurrentCount);
            Assert.Equal(0, window.UpdateAndGetCount(t0 + 200, freq));
        }

        [Fact]
        public void HighVolumeBurst_CalculatesAccurately()
        {
            var window = new KpsSlidingWindow(2048);
            long freq = 10_000_000; // 10 MHz
            long tBase = 50_000_000;

            // Simulate 120 keypresses spread evenly across 1000ms
            for (int i = 0; i < 120; i++)
            {
                long t = tBase + (long)(i * (freq / 120.0));
                window.RecordPress(t);
            }

            // Query at exact end of 1 second window
            int kps = window.UpdateAndGetCount(tBase + freq, freq);
            Assert.Equal(120, kps);

            // Query at 500ms after the burst ended: half should have expired
            int halfExpired = window.UpdateAndGetCount(tBase + freq + (freq / 2), freq);
            Assert.True(halfExpired < 120 && halfExpired > 0);

            // Query at 1001ms after burst ended: all should have expired
            int allExpired = window.UpdateAndGetCount(tBase + (2 * freq) + 1000, freq);
            Assert.Equal(0, allExpired);
        }
    }
}
