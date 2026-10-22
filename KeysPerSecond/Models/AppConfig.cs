using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KeysPerSecond.Models
{
    public class WindowConfig
    {
        [JsonPropertyName("Left")]
        public double Left { get; set; } = 150.0;

        [JsonPropertyName("Top")]
        public double Top { get; set; } = 150.0;

        [JsonPropertyName("Scale")]
        public double Scale { get; set; } = 1.0;

        [JsonPropertyName("CustomWidth")]
        public double CustomWidth { get; set; } = 0.0;

        [JsonPropertyName("CustomHeight")]
        public double CustomHeight { get; set; } = 0.0;

        [JsonPropertyName("AutoFit")]
        public bool AutoFit { get; set; } = true;

        [JsonPropertyName("Topmost")]
        public bool Topmost { get; set; } = true;

        [JsonPropertyName("IsLocked")]
        public bool IsLocked { get; set; } = true;

        public WindowConfig Clone() => new()
        {
            Left = this.Left,
            Top = this.Top,
            Scale = this.Scale,
            CustomWidth = this.CustomWidth,
            CustomHeight = this.CustomHeight,
            AutoFit = this.AutoFit,
            Topmost = this.Topmost,
            IsLocked = this.IsLocked
        };
    }

    public class StyleConfig
    {
        [JsonPropertyName("AccentColor")]
        public string AccentColor { get; set; } = "#00E5FF";

        [JsonPropertyName("PressedColor")]
        public string PressedColor { get; set; } = "#FF007F";

        [JsonPropertyName("BackgroundColor")]
        public string BackgroundColor { get; set; } = "#12131C";

        [JsonPropertyName("BackgroundOpacity")]
        public double BackgroundOpacity { get; set; } = 0.85;

        [JsonPropertyName("BorderColor")]
        public string BorderColor { get; set; } = "#2E3048";

        [JsonPropertyName("TextColor")]
        public string TextColor { get; set; } = "#FFFFFF";

        [JsonPropertyName("KeyWidth")]
        public double KeyWidth { get; set; } = 64.0;

        [JsonPropertyName("KeyHeight")]
        public double KeyHeight { get; set; } = 76.0;

        [JsonPropertyName("KeySpacing")]
        public double KeySpacing { get; set; } = 8.0;

        [JsonPropertyName("CornerRadius")]
        public double CornerRadius { get; set; } = 12.0;

        [JsonPropertyName("ShowIndividualCounters")]
        public bool ShowIndividualCounters { get; set; } = true;

        [JsonPropertyName("ShowKpsCounter")]
        public bool ShowKpsCounter { get; set; } = true;

        [JsonPropertyName("ShowMaxKps")]
        public bool ShowMaxKps { get; set; } = true;

        [JsonPropertyName("ShowTotalCounter")]
        public bool ShowTotalCounter { get; set; } = true;

        [JsonPropertyName("Orientation")]
        public string Orientation { get; set; } = "Horizontal";

        [JsonPropertyName("ShowHoldDuration")]
        public bool ShowHoldDuration { get; set; } = true;

        [JsonPropertyName("ShowHoldBar")]
        public bool ShowHoldBar { get; set; } = true;

        [JsonPropertyName("MaxHoldDurationMs")]
        public double MaxHoldDurationMs { get; set; } = 300.0;

        [JsonPropertyName("HoldBarDecayMs")]
        public double HoldBarDecayMs { get; set; } = 600.0;

        public StyleConfig Clone() => new()
        {
            AccentColor = this.AccentColor,
            PressedColor = this.PressedColor,
            BackgroundColor = this.BackgroundColor,
            BackgroundOpacity = this.BackgroundOpacity,
            BorderColor = this.BorderColor,
            TextColor = this.TextColor,
            KeyWidth = this.KeyWidth,
            KeyHeight = this.KeyHeight,
            KeySpacing = this.KeySpacing,
            CornerRadius = this.CornerRadius,
            ShowIndividualCounters = this.ShowIndividualCounters,
            ShowKpsCounter = this.ShowKpsCounter,
            ShowMaxKps = this.ShowMaxKps,
            ShowTotalCounter = this.ShowTotalCounter,
            Orientation = this.Orientation,
            ShowHoldDuration = this.ShowHoldDuration,
            ShowHoldBar = this.ShowHoldBar,
            MaxHoldDurationMs = this.MaxHoldDurationMs,
            HoldBarDecayMs = this.HoldBarDecayMs
        };
    }

    public class AppConfig
    {
        [JsonPropertyName("Window")]
        public WindowConfig Window { get; set; } = new();

        [JsonPropertyName("Style")]
        public StyleConfig Style { get; set; } = new();

        [JsonPropertyName("ToggleEditModeKey")]
        public string ToggleEditModeKey { get; set; } = "Ctrl+Shift+F10";

        [JsonPropertyName("ResetStatsKey")]
        public string ResetStatsKey { get; set; } = "Ctrl+Shift+R";

        [JsonPropertyName("TrackedKeys")]
        public List<KeyBindingConfig> TrackedKeys { get; set; } = new()
        {
            new KeyBindingConfig { Key = "Z", Label = "Z", VirtualKey = 90, AccentColor = "#00E5FF" },
            new KeyBindingConfig { Key = "X", Label = "X", VirtualKey = 88, AccentColor = "#00E5FF" },
            new KeyBindingConfig { Key = "C", Label = "C", VirtualKey = 67, AccentColor = "#FF007F" },
            new KeyBindingConfig { Key = "V", Label = "V", VirtualKey = 86, AccentColor = "#FF007F" }
        };

        public AppConfig Clone()
        {
            var clone = new AppConfig
            {
                Window = this.Window.Clone(),
                Style = this.Style.Clone(),
                ToggleEditModeKey = this.ToggleEditModeKey,
                ResetStatsKey = this.ResetStatsKey,
                TrackedKeys = new List<KeyBindingConfig>()
            };

            foreach (var key in this.TrackedKeys)
            {
                clone.TrackedKeys.Add(key.Clone());
            }

            return clone;
        }
    }
}
