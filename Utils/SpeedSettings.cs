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
        public const int CurrentSchemaVersion = 3;
        public const float DefaultSpeedMultiplier = 1.0f;
        public const SpeedAccelerationMode DefaultAccelerationMode = SpeedAccelerationMode.SafeState;
        public const bool DefaultCardAnimationAccelerationEnabled = false;
        public const float DefaultCardAnimationMultiplier = 3.0f;
        public const bool DefaultCardResolutionAccelerationEnabled = false;
        public const float DefaultCardResolutionMultiplier = 3.0f;
        public const bool DefaultCombatPresentationAccelerationEnabled = false;
        public const float DefaultCombatPresentationMultiplier = 3.0f;
        public const bool DefaultProgressiveAccelerationEnabled = false;
        public const float DefaultTransitionDuration = 10.0f;
        public const float DefaultTimeThreshold = 3.0f;
        public const bool DefaultAccelerateCardPileSequences = true;
        public const bool DefaultAccelerateCardPlayResolution = true;
        public const bool DefaultAccelerateTurnTransitions = true;
        public const bool DefaultAccelerateEnemyActions = true;
        public const bool DefaultAccelerateTimelineAnimations = true;
        public const bool DefaultAccelerateGameOverSummary = true;
        public const bool DefaultAccelerateLoadingScreens = true;

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
        public float SpeedMultiplier { get; set; } = DefaultSpeedMultiplier;

        [JsonPropertyName("card_animation_acceleration_enabled")]
        public bool CardAnimationAccelerationEnabled { get; set; } = DefaultCardAnimationAccelerationEnabled;

        [JsonPropertyName("card_animation_multiplier")]
        public float CardAnimationMultiplier { get; set; } = DefaultCardAnimationMultiplier;

        [JsonPropertyName("card_resolution_acceleration_enabled")]
        public bool CardResolutionAccelerationEnabled { get; set; } = DefaultCardResolutionAccelerationEnabled;

        [JsonPropertyName("card_resolution_multiplier")]
        public float CardResolutionMultiplier { get; set; } = DefaultCardResolutionMultiplier;

        [JsonPropertyName("combat_presentation_acceleration_enabled")]
        public bool CombatPresentationAccelerationEnabled { get; set; } =
            DefaultCombatPresentationAccelerationEnabled;

        [JsonPropertyName("combat_presentation_multiplier")]
        public float CombatPresentationMultiplier { get; set; } = DefaultCombatPresentationMultiplier;

        [JsonPropertyName("progressive_acceleration_enabled")]
        public bool ProgressiveAccelerationEnabled { get; set; } = DefaultProgressiveAccelerationEnabled;

        /// <summary>
        ///     Duration of the ramp from 1x to the target multiplier when progressive acceleration is enabled.
        /// </summary>
        [JsonPropertyName("transition_duration")]
        public float TransitionDuration { get; set; } = DefaultTransitionDuration;

        /// <summary>
        ///     Delay before a safe state starts accelerating when progressive acceleration is enabled.
        /// </summary>
        [JsonPropertyName("time_threshold")]
        public float TimeThreshold { get; set; } = DefaultTimeThreshold;

        [JsonPropertyName("accelerate_card_pile_sequences")]
        public bool AccelerateCardPileSequences { get; set; } = DefaultAccelerateCardPileSequences;

        [JsonPropertyName("accelerate_card_play_resolution")]
        public bool AccelerateCardPlayResolution { get; set; } = DefaultAccelerateCardPlayResolution;

        [JsonPropertyName("accelerate_turn_transitions")]
        public bool AccelerateTurnTransitions { get; set; } = DefaultAccelerateTurnTransitions;

        [JsonPropertyName("accelerate_enemy_actions")]
        public bool AccelerateEnemyActions { get; set; } = DefaultAccelerateEnemyActions;

        [JsonPropertyName("accelerate_timeline_animations")]
        public bool AccelerateTimelineAnimations { get; set; } = DefaultAccelerateTimelineAnimations;

        [JsonPropertyName("accelerate_game_over_summary")]
        public bool AccelerateGameOverSummary { get; set; } = DefaultAccelerateGameOverSummary;

        [JsonPropertyName("accelerate_loading_screens")]
        public bool AccelerateLoadingScreens { get; set; } = DefaultAccelerateLoadingScreens;

        [JsonIgnore]
        public SpeedAccelerationMode AccelerationMode
        {
            get => AccelerationModeOverride ?? (ProgressiveEnabled
                ? DefaultAccelerationMode
                : SpeedAccelerationMode.AlwaysOn);
            set
            {
                AccelerationModeOverride = value;
                ProgressiveEnabled = value == SpeedAccelerationMode.SafeState;
            }
        }
    }
}
