using System.Text.Json.Serialization;

namespace STS2QuickAnimationMode.Utils
{
    public enum SpeedAccelerationMode
    {
        Off,
        SafeState,
        AlwaysOn,
    }

    public class SpeedSettings
    {
        public const int CurrentSchemaVersion = 1;

        [JsonPropertyName("schema_version")] public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        /// <summary>
        ///     Migration source for the old toggle. Use <see cref="AccelerationMode" /> for current behavior.
        /// </summary>
        [JsonPropertyName("progressive_enabled")]
        public bool ProgressiveEnabled { get; set; } = true;

        /// <summary>
        ///     Null means the settings were created before explicit modes existed.
        /// </summary>
        [JsonPropertyName("acceleration_mode")]
        public SpeedAccelerationMode? AccelerationModeOverride { get; set; }

        /// <summary>
        ///     Speed multiplier value (1.0 = normal, 2.0 = 2x speed, etc.).
        /// </summary>
        [JsonPropertyName("speed_multiplier")]
        public float SpeedMultiplier { get; set; } = 1.0f;

        [JsonPropertyName("progressive_acceleration_enabled")]
        public bool ProgressiveAccelerationEnabled { get; set; }

        /// <summary>
        ///     Duration of the ramp from 1x to the target multiplier when progressive acceleration is enabled.
        /// </summary>
        [JsonPropertyName("transition_duration")]
        public float TransitionDuration { get; set; } = 0.2f;

        /// <summary>
        ///     Delay before a safe state starts accelerating when progressive acceleration is enabled.
        /// </summary>
        [JsonPropertyName("time_threshold")]
        public float TimeThreshold { get; set; } = 0.05f;

        [JsonPropertyName("accelerate_card_pile_sequences")]
        public bool AccelerateCardPileSequences { get; set; } = true;

        [JsonPropertyName("accelerate_card_play_resolution")]
        public bool AccelerateCardPlayResolution { get; set; } = true;

        [JsonPropertyName("accelerate_turn_transitions")]
        public bool AccelerateTurnTransitions { get; set; } = true;

        [JsonPropertyName("accelerate_enemy_actions")]
        public bool AccelerateEnemyActions { get; set; } = true;

        [JsonPropertyName("accelerate_timeline_animations")]
        public bool AccelerateTimelineAnimations { get; set; } = true;

        [JsonPropertyName("accelerate_loading_screens")]
        public bool AccelerateLoadingScreens { get; set; } = true;

        [JsonIgnore]
        public SpeedAccelerationMode AccelerationMode
        {
            get => AccelerationModeOverride ?? (ProgressiveEnabled
                ? SpeedAccelerationMode.SafeState
                : SpeedAccelerationMode.AlwaysOn);
            set
            {
                AccelerationModeOverride = value;
                ProgressiveEnabled = value == SpeedAccelerationMode.SafeState;
            }
        }
    }
}
