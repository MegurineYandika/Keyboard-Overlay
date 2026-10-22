using System;
using System.Text.Json.Serialization;

namespace KeysPerSecond.Models
{
    public class WaterfallConfig
    {
        [JsonPropertyName("IsEnabled")]
        public bool IsEnabled { get; set; } = true;

        [JsonPropertyName("ScrollDirection")]
        public string ScrollDirection { get; set; } = "Upscroll"; // "Upscroll" or "Downscroll"

        [JsonPropertyName("ScrollSpeed")]
        public double ScrollSpeed { get; set; } = 0.8; // Pixels per millisecond

        [JsonPropertyName("MaxVisibleDurationMs")]
        public double MaxVisibleDurationMs { get; set; } = 1500.0; // Window horizon before culling

        [JsonPropertyName("BarOpacity")]
        public double BarOpacity { get; set; } = 0.75;

        [JsonPropertyName("BarCornerRadius")]
        public double BarCornerRadius { get; set; } = 4.0;

        [JsonPropertyName("UseKeyAccentColors")]
        public bool UseKeyAccentColors { get; set; } = true;

        [JsonPropertyName("Height")]
        public double Height { get; set; } = 200.0;

        [JsonPropertyName("TransparentMode")]
        public bool TransparentMode { get; set; } = false;

        [JsonPropertyName("MatchPatternRatio")]
        public bool MatchPatternRatio { get; set; } = false;

        public WaterfallConfig Clone() => new()
        {
            IsEnabled = this.IsEnabled,
            ScrollDirection = this.ScrollDirection,
            ScrollSpeed = this.ScrollSpeed,
            MaxVisibleDurationMs = this.MaxVisibleDurationMs,
            BarOpacity = this.BarOpacity,
            BarCornerRadius = this.BarCornerRadius,
            UseKeyAccentColors = this.UseKeyAccentColors,
            Height = this.Height,
            TransparentMode = this.TransparentMode,
            MatchPatternRatio = this.MatchPatternRatio
        };
    }
}

