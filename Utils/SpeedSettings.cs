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

        /// <summary>
        ///     Whether progressive acceleration is enabled
        /// </summary>
        [JsonPropertyName("progressive_enabled")]
        public bool ProgressiveEnabled { get; set; } = true;

        /// <summary>
        ///     Duration of transition in seconds (time to reach max speed after threshold)
        /// </summary>
        [JsonPropertyName("transition_duration")]
        public float TransitionDuration { get; set; } = 10.0f;

        /// <summary>
        ///     Time threshold in seconds - accelerate if single action runs longer than this
        /// </summary>
        [JsonPropertyName("time_threshold")]
        public float TimeThreshold { get; set; } = 3.0f;
    }
}
