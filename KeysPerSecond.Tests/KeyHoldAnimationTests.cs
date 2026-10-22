using System;
using System.Diagnostics;
using KeysPerSecond.ViewModels;
using Xunit;

namespace KeysPerSecond.Tests
{
    public class KeyHoldAnimationTests
    {
        [Fact]
        public void OnKeyDown_InitializesHoldMetrics()
        {
            var key = new KeyItemViewModel(90, "Z", "Z", "#00E5FF", width: 64, height: 76);
            long now = 1000 * Stopwatch.Frequency;

            key.OnKeyDown(now);

            Assert.True(key.IsPressed);
            Assert.Equal(now, key.KeyDownTimestamp);
            Assert.Equal(0, key.CurrentHoldMs);
            Assert.Equal(0, key.HoldProgress);
            Assert.Equal(1.0, key.HoldBarOpacity);
            Assert.Equal("0ms", key.FormattedDuration);
            Assert.Equal(1, key.TotalPresses);
        }

        [Fact]
        public void UpdateHoldAnimation_WhilePressed_AdvancesHoldDurationAndBarWidth()
        {
            var key = new KeyItemViewModel(90, "Z", "Z", "#00E5FF", width: 64, height: 76);
            long freq = Stopwatch.Frequency;
            long tDown = 1000 * freq;

            key.OnKeyDown(tDown);

            // Advance by 150ms (with benchmark = 300ms)
            long tMid = tDown + (long)(freq * 0.150);
            key.UpdateHoldAnimation(tMid, maxBenchmarkMs: 300.0, decayWindowMs: 600.0);

            Assert.True(key.IsPressed);
            Assert.InRange(key.CurrentHoldMs, 149.0, 151.0);
            Assert.InRange(key.HoldProgress, 0.49, 0.51); // 150ms / 300ms = ~0.5
            Assert.InRange(key.HoldBarWidth, 23.5, 24.5); // (64 - 16) * 0.5 = 24
            Assert.Equal("150ms", key.FormattedDuration);
            Assert.Equal(1.0, key.HoldBarOpacity);
        }

        [Fact]
        public void OnKeyUp_ComputesExactFinalHoldDuration()
        {
            var key = new KeyItemViewModel(90, "Z", "Z", "#00E5FF", width: 64, height: 76);
            long freq = Stopwatch.Frequency;
            long tDown = 1000 * freq;
            long tUp = tDown + (long)(freq * 0.082); // held for 82ms

            key.OnKeyDown(tDown);
            key.OnKeyUp(tUp);

            Assert.False(key.IsPressed);
            Assert.Equal(tUp, key.KeyReleaseTimestamp);
            Assert.InRange(key.LastHoldMs, 81.0, 83.0);
            Assert.Equal("82ms", key.FormattedDuration);
        }

        [Fact]
        public void UpdateHoldAnimation_AfterRelease_FadesSmoothlyOverDecayWindow()
        {
            var key = new KeyItemViewModel(90, "Z", "Z", "#00E5FF", width: 64, height: 76);
            long freq = Stopwatch.Frequency;
            long tDown = 1000 * freq;
            long tUp = tDown + (long)(freq * 0.100); // 100ms press

            key.OnKeyDown(tDown);
            key.OnKeyUp(tUp);

            // 1. Immediately after release (0ms since release): Opacity = 1.0
            key.UpdateHoldAnimation(tUp, maxBenchmarkMs: 300.0, decayWindowMs: 600.0);
            Assert.Equal(1.0, key.HoldBarOpacity);

            // 2. Halfway through decay window (300ms after release): Opacity = ~0.5
            long tHalfDecay = tUp + (long)(freq * 0.300);
            key.UpdateHoldAnimation(tHalfDecay, maxBenchmarkMs: 300.0, decayWindowMs: 600.0);
            Assert.InRange(key.HoldBarOpacity, 0.48, 0.52);
            // Duration remains readable
            Assert.Equal("100ms", key.FormattedDuration);

            // 3. Past decay window (650ms after release): Opacity = 0.0
            long tExpired = tUp + (long)(freq * 0.650);
            key.UpdateHoldAnimation(tExpired, maxBenchmarkMs: 300.0, decayWindowMs: 600.0);
            Assert.Equal(0.0, key.HoldBarOpacity);
            Assert.Equal(0.0, key.HoldProgress);
        }

        [Fact]
        public void Reset_ClearsHoldStateAndDuration()
        {
            var key = new KeyItemViewModel(90, "Z", "Z", "#00E5FF", width: 64, height: 76);
            long freq = Stopwatch.Frequency;
            key.OnKeyDown(1000 * freq);
            key.OnKeyUp((long)(1000.1 * freq));

            Assert.Equal("100ms", key.FormattedDuration);
            Assert.Equal(1, key.TotalPresses);

            key.Reset();

            Assert.False(key.IsPressed);
            Assert.Equal(0, key.TotalPresses);
            Assert.Equal(0, key.CurrentHoldMs);
            Assert.Equal(0, key.LastHoldMs);
            Assert.Equal(0, key.HoldProgress);
            Assert.Equal(0, key.HoldBarOpacity);
            Assert.Equal("--", key.FormattedDuration);
        }
    }
}
