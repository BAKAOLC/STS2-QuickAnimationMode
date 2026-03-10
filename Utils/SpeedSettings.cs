using System.Text.Json.Serialization;

namespace STS2QuickAnimationMode.Utils
{
    public class SpeedSettings
    {
        /// <summary>
        ///     Speed multiplier value (1.0 = normal, 2.0 = 2x speed, etc.)
        /// </summary>
        [JsonPropertyName("speed_multiplier")]
        public float SpeedMultiplier { get; set; } = 1.0f;
    }
}
