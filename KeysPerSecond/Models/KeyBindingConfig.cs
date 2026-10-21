using System.Text.Json.Serialization;

namespace KeysPerSecond.Models
{
    public class KeyBindingConfig
    {
        [JsonPropertyName("Key")]
        public string Key { get; set; } = "Z";

        [JsonPropertyName("Label")]
        public string? Label { get; set; }

        [JsonPropertyName("VirtualKey")]
        public byte VirtualKey { get; set; } = 0;

        [JsonPropertyName("AccentColor")]
        public string? AccentColor { get; set; }

        [JsonPropertyName("Width")]
        public double? Width { get; set; }

        [JsonPropertyName("Height")]
        public double? Height { get; set; }
    }
}
