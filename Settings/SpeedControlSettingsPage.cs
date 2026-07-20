using STS2QuickAnimationMode.Data;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace STS2QuickAnimationMode.Settings
{
    public static class SpeedControlSettingsPage
    {
        public static void Register()
        {
            RitsuLibFramework.RegisterModSettings(
                Const.ModId,
                page => page
                    .WithTitle(T("SETTINGS_PAGE_TITLE", "Speed Control"))
                    .WithModDisplayName(T("SETTINGS_PAGE_MOD_NAME", "Speed Control"))
                    .WithDescription(T("SETTINGS_PAGE_DESCRIPTION",
                        "Configure game speed, card animations, committed card resolution, and combat presentation independently."))
                    .AddSection(Const.SettingsSectionId, section => section
                        .WithTitle(T("SETTINGS_SECTION_SPEED", "Basic speed"))
                        .AddChoice(
                            "mode",
                            T("MODE", "Mode"),
                            Bind("mode", settings => settings.AccelerationMode,
                                (settings, value) => settings.AccelerationMode = value,
                                () => SpeedSettings.DefaultAccelerationMode),
                            [
                                new(SpeedAccelerationMode.SafeState,
                                    T("MODE_SAFE_STATE", "Safe state acceleration")),
                                new(SpeedAccelerationMode.AlwaysOn,
                                    T("MODE_ALWAYS_ON", "Global continuous acceleration")),
                                new(SpeedAccelerationMode.Off,
                                    T("MODE_OFF", "Off")),
                            ],
                            T("MODE_DESCRIPTION",
                                "Controls overall game acceleration. Card animation, resolution, and combat presentation settings below are independent."),
                            ModSettingsChoicePresentation.Dropdown)
                        .AddSlider(
                            "speed_multiplier",
                            T("SPEED_MULTIPLIER", "Game Speed Multiplier"),
                            BindDouble("speed_multiplier", settings => settings.SpeedMultiplier,
                                (settings, value) => settings.SpeedMultiplier = (float)value,
                                () => SpeedSettings.DefaultSpeedMultiplier),
                            SpeedManager.MinSelectableMultiplier,
                            SpeedManager.MaxSelectableMultiplier,
                            SpeedManager.SpeedSliderStep,
                            value => $"{value:0.#}x",
                            T("SPEED_MULTIPLIER_DESCRIPTION",
                                "Target multiplier for global mode or the automatic sequences enabled in Safe State mode.")))
                    .AddSection("card-animations", section => section
                        .WithTitle(T("SETTINGS_SECTION_CARD_ANIMATIONS", "Card animations"))
                        .WithDescription(T("SETTINGS_SECTION_CARD_ANIMATIONS_DESCRIPTION",
                            "Speeds supported card visuals without changing overall game speed."))
                        .AddToggle(
                            "card_animation_acceleration_enabled",
                            T("CARD_ANIMATION_ACCELERATION_ENABLED", "Accelerate card animations"),
                            Bind("card_animation_acceleration_enabled",
                                settings => settings.CardAnimationAccelerationEnabled,
                                (settings, value) => settings.CardAnimationAccelerationEnabled = value,
                                () => SpeedSettings.DefaultCardAnimationAccelerationEnabled),
                            T("CARD_ANIMATION_ACCELERATION_ENABLED_DESCRIPTION",
                                "Speeds card movement, flying and shuffling, power-card travel, exhaust effects, and hand arrangement while preserving their normal completion flow."))
                        .AddSlider(
                            "card_animation_multiplier",
                            T("CARD_ANIMATION_MULTIPLIER", "Card Animation Multiplier"),
                            BindDouble("card_animation_multiplier", settings => settings.CardAnimationMultiplier,
                                (settings, value) => settings.CardAnimationMultiplier = (float)value,
                                () => SpeedSettings.DefaultCardAnimationMultiplier),
                            SpeedManager.MinSelectableMultiplier,
                            SpeedManager.MaxSelectableMultiplier,
                            SpeedManager.SpeedSliderStep,
                            value => $"{value:0.#}x",
                            T("CARD_ANIMATION_MULTIPLIER_DESCRIPTION",
                                "Multiplier applied only to supported card animations.")))
                    .AddSection("card-resolution", section => section
                        .WithTitle(T("SETTINGS_SECTION_CARD_RESOLUTION", "Card resolution"))
                        .WithDescription(T("SETTINGS_SECTION_CARD_RESOLUTION_DESCRIPTION",
                            "Shortens built-in pauses after a card has been played without changing overall game speed."))
                        .AddToggle(
                            "card_resolution_acceleration_enabled",
                            T("CARD_RESOLUTION_ACCELERATION_ENABLED", "Accelerate card resolution"),
                            Bind("card_resolution_acceleration_enabled",
                                settings => settings.CardResolutionAccelerationEnabled,
                                (settings, value) => settings.CardResolutionAccelerationEnabled = value,
                                () => SpeedSettings.DefaultCardResolutionAccelerationEnabled),
                            T("CARD_RESOLUTION_ACCELERATION_ENABLED_DESCRIPTION",
                                "Speeds the pauses between a played card's automatic effects. Effects, choices, interruptions, and completion order remain unchanged."))
                        .AddSlider(
                            "card_resolution_multiplier",
                            T("CARD_RESOLUTION_MULTIPLIER", "Card Resolution Multiplier"),
                            BindDouble("card_resolution_multiplier", settings => settings.CardResolutionMultiplier,
                                (settings, value) => settings.CardResolutionMultiplier = (float)value,
                                () => SpeedSettings.DefaultCardResolutionMultiplier),
                            SpeedManager.MinSelectableMultiplier,
                            SpeedManager.MaxSelectableMultiplier,
                            SpeedManager.SpeedSliderStep,
                            value => $"{value:0.#}x",
                            T("CARD_RESOLUTION_MULTIPLIER_DESCRIPTION",
                                "Multiplier for pauses between the automatic effects of a played card.")))
                    .AddSection("combat-presentation", section => section
                        .WithTitle(T("SETTINGS_SECTION_COMBAT_PRESENTATION", "Combat presentation"))
                        .WithDescription(T("SETTINGS_SECTION_COMBAT_PRESENTATION_DESCRIPTION",
                            "Speeds supported combat messages and visual pacing without changing overall game speed."))
                        .AddToggle(
                            "combat_presentation_acceleration_enabled",
                            T("COMBAT_PRESENTATION_ACCELERATION_ENABLED", "Accelerate combat presentation"),
                            Bind("combat_presentation_acceleration_enabled",
                                settings => settings.CombatPresentationAccelerationEnabled,
                                (settings, value) => settings.CombatPresentationAccelerationEnabled = value,
                                () => SpeedSettings.DefaultCombatPresentationAccelerationEnabled),
                            T("COMBAT_PRESENTATION_ACCELERATION_ENABLED_DESCRIPTION",
                                "Speeds battle-start and turn banners, the brief pause at turn start, enemy intent presentation, damage and healing numbers, and blocked text. Every action still runs in its normal order."))
                        .AddSlider(
                            "combat_presentation_multiplier",
                            T("COMBAT_PRESENTATION_MULTIPLIER", "Combat Presentation Multiplier"),
                            BindDouble("combat_presentation_multiplier",
                                settings => settings.CombatPresentationMultiplier,
                                (settings, value) => settings.CombatPresentationMultiplier = (float)value,
                                () => SpeedSettings.DefaultCombatPresentationMultiplier),
                            SpeedManager.MinSelectableMultiplier,
                            SpeedManager.MaxSelectableMultiplier,
                            SpeedManager.SpeedSliderStep,
                            value => $"{value:0.#}x",
                            T("COMBAT_PRESENTATION_MULTIPLIER_DESCRIPTION",
                                "Multiplier for supported combat messages, number popups, and their related presentation pauses.")))
                    .AddSection("safe-sequences", section => section
                        .WithTitle(T("SETTINGS_SECTION_SAFE_SEQUENCES", "Combat automation"))
                        .WithDescription(T("SETTINGS_SECTION_SAFE_SEQUENCES_DESCRIPTION",
                            "Choose which supported automatic combat flows may use the game speed multiplier in Safe State mode."))
                        .WithVisibleWhen(IsSafeStateMode)
                        .AddToggle(
                            "accelerate_card_pile_sequences",
                            T("ACCELERATE_CARD_PILE_SEQUENCES", "Card pile commands"),
                            Bind("accelerate_card_pile_sequences", settings => settings.AccelerateCardPileSequences,
                                (settings, value) => settings.AccelerateCardPileSequences = value,
                                () => SpeedSettings.DefaultAccelerateCardPileSequences),
                            T("ACCELERATE_CARD_PILE_SEQUENCES_DESCRIPTION",
                                "Allows safe acceleration during draw, shuffle, discard, discard-and-draw, and exhaust command execution."))
                        .AddToggle(
                            "accelerate_card_play_resolution",
                            T("ACCELERATE_CARD_PLAY_RESOLUTION", "Committed card resolution"),
                            Bind("accelerate_card_play_resolution", settings => settings.AccelerateCardPlayResolution,
                                (settings, value) => settings.AccelerateCardPlayResolution = value,
                                () => SpeedSettings.DefaultAccelerateCardPlayResolution),
                            T("ACCELERATE_CARD_PLAY_RESOLUTION_DESCRIPTION",
                                "Allows safe acceleration after a card has been played and is resolving its automatic effects. Acceleration pauses whenever the player must make a choice."))
                        .AddToggle(
                            "accelerate_turn_transitions",
                            T("ACCELERATE_TURN_TRANSITIONS", "Turn transitions"),
                            Bind("accelerate_turn_transitions", settings => settings.AccelerateTurnTransitions,
                                (settings, value) => settings.AccelerateTurnTransitions = value,
                                () => SpeedSettings.DefaultAccelerateTurnTransitions),
                            T("ACCELERATE_TURN_TRANSITIONS_DESCRIPTION",
                                "Allows safe acceleration during player turn start, player turn end, side switching, and enemy turn cleanup sequences."))
                        .AddToggle(
                            "accelerate_enemy_actions",
                            T("ACCELERATE_ENEMY_ACTIONS", "Enemy actions"),
                            Bind("accelerate_enemy_actions", settings => settings.AccelerateEnemyActions,
                                (settings, value) => settings.AccelerateEnemyActions = value,
                                () => SpeedSettings.DefaultAccelerateEnemyActions),
                            T("ACCELERATE_ENEMY_ACTIONS_DESCRIPTION",
                                "Allows safe acceleration while enemies perform intents and take their automatic turns.")))
                    .AddSection("safe-ui-loading", section => section
                        .WithTitle(T("SETTINGS_SECTION_SAFE_UI_LOADING", "UI and loading"))
                        .WithDescription(T("SETTINGS_SECTION_SAFE_UI_LOADING_DESCRIPTION",
                            "Choose which non-combat automatic UI and loading flows may raise the game speed in safe state mode."))
                        .WithVisibleWhen(IsSafeStateMode)
                        .AddToggle(
                            "accelerate_timeline_animations",
                            T("ACCELERATE_TIMELINE_ANIMATIONS", "Timeline animations"),
                            Bind("accelerate_timeline_animations", settings => settings.AccelerateTimelineAnimations,
                                (settings, value) => settings.AccelerateTimelineAnimations = value,
                                () => SpeedSettings.DefaultAccelerateTimelineAnimations),
                            T("ACCELERATE_TIMELINE_ANIMATIONS_DESCRIPTION",
                                "Allows safe acceleration during non-interactive timeline reveal, unlock, and slot animation sequences."))
                        .AddToggle(
                            "accelerate_game_over_summary",
                            T("ACCELERATE_GAME_OVER_SUMMARY", "Game over summary"),
                            Bind("accelerate_game_over_summary", settings => settings.AccelerateGameOverSummary,
                                (settings, value) => settings.AccelerateGameOverSummary = value,
                                () => SpeedSettings.DefaultAccelerateGameOverSummary),
                            T("ACCELERATE_GAME_OVER_SUMMARY_DESCRIPTION",
                                "Allows safe acceleration while the run summary, score lines, badges, score bar, and discovery counts animate in."))
                        .AddToggle(
                            "accelerate_loading_screens",
                            T("ACCELERATE_LOADING_SCREENS", "Run loading"),
                            Bind("accelerate_loading_screens", settings => settings.AccelerateLoadingScreens,
                                (settings, value) => settings.AccelerateLoadingScreens = value,
                                () => SpeedSettings.DefaultAccelerateLoadingScreens),
                            T("ACCELERATE_LOADING_SCREENS_DESCRIPTION",
                                "Allows safe acceleration during run loading and background asset loading screens.")))
                    .AddSection("progressive-acceleration", section => section
                        .WithTitle(T("SETTINGS_SECTION_PROGRESSIVE", "Progressive acceleration"))
                        .WithDescription(T("SETTINGS_SECTION_PROGRESSIVE_DESCRIPTION",
                            "Optional smoothing for safe state mode. When disabled, allowed safe sequences switch to the target speed immediately."))
                        .WithVisibleWhen(IsSafeStateMode)
                        .AddToggle(
                            "progressive_acceleration_enabled",
                            T("PROGRESSIVE_ACCELERATION_ENABLED", "Enable progressive acceleration"),
                            Bind("progressive_acceleration_enabled",
                                settings => settings.ProgressiveAccelerationEnabled,
                                (settings, value) => settings.ProgressiveAccelerationEnabled = value,
                                () => SpeedSettings.DefaultProgressiveAccelerationEnabled),
                            T("PROGRESSIVE_ACCELERATION_ENABLED_DESCRIPTION",
                                "Adds a configurable delay and ramp before safe automatic sequences reach the target speed.")))
                    .AddSection("progressive-tuning", section => section
                        .WithTitle(T("SETTINGS_SECTION_PROGRESSIVE_TUNING", "Progressive timing"))
                        .WithDescription(T("SETTINGS_SECTION_PROGRESSIVE_TUNING_DESCRIPTION",
                            "These values are only used when progressive acceleration is enabled."))
                        .WithVisibleWhen(IsProgressiveAccelerationVisible)
                        .AddSlider(
                            "time_threshold",
                            T("TIME_THRESHOLD", "Activation Delay"),
                            BindDouble("time_threshold", settings => settings.TimeThreshold,
                                (settings, value) => settings.TimeThreshold = (float)value,
                                () => SpeedSettings.DefaultTimeThreshold),
                            SpeedManager.MinTimeThreshold,
                            SpeedManager.MaxTimeThreshold,
                            0.05d,
                            FormatSeconds,
                            T("TIME_THRESHOLD_DESCRIPTION",
                                "How long an allowed safe sequence must stay active before it starts increasing above 1x."))
                        .AddSlider(
                            "transition_duration",
                            T("TRANSITION_DURATION", "Ramp Duration"),
                            BindDouble("transition_duration", settings => settings.TransitionDuration,
                                (settings, value) => settings.TransitionDuration = (float)value,
                                () => SpeedSettings.DefaultTransitionDuration),
                            SpeedManager.MinTransitionDuration,
                            SpeedManager.MaxTransitionDuration,
                            0.05d,
                            FormatSeconds,
                            T("TRANSITION_DURATION_DESCRIPTION",
                                "How long it takes to interpolate from 1x to the selected target speed after the activation delay.")))
                    .AddSection("multiplier-guide", section => section
                        .WithTitle(T("SETTINGS_SECTION_MULTIPLIER_GUIDE", "Multiplier guide"))
                        .WithDescription(T("SETTINGS_SECTION_MULTIPLIER_GUIDE_DESCRIPTION",
                            "Around 3x is recommended when using one speed control by itself: it removes much of the waiting while remaining visually comfortable and avoiding many very-high-speed issues."))
                        .AddInfoCard(
                            "game_speed_guide",
                            T("GAME_SPEED_GUIDE", "Game speed"),
                            T("GAME_SPEED_GUIDE_DESCRIPTION",
                                "Changes the pace of the whole game. Global Continuous applies broadly; Safe State applies only during enabled automatic flows such as pile commands, played-card effects, turn transitions, enemy actions, timelines, and loading."))
                        .AddInfoCard(
                            "card_animation_guide",
                            T("CARD_ANIMATION_GUIDE", "Card animation speed"),
                            T("CARD_ANIMATION_GUIDE_DESCRIPTION",
                                "Speeds supported card movement, flying and shuffling, power-card travel, exhaust effects, and hand arrangement. By itself it does not speed up the rest of the game."))
                        .AddInfoCard(
                            "card_resolution_guide",
                            T("CARD_RESOLUTION_GUIDE", "Card resolution speed"),
                            T("CARD_RESOLUTION_GUIDE_DESCRIPTION",
                                "Shortens pauses between the automatic effects of a card that has already been played. It does not skip effects or choices, change their order, or affect unrelated pauses."))
                        .AddInfoCard(
                            "combat_presentation_guide",
                            T("COMBAT_PRESENTATION_GUIDE", "Combat presentation speed"),
                            T("COMBAT_PRESENTATION_GUIDE_DESCRIPTION",
                                "Speeds battle-start and turn banners, the brief turn-start pause, enemy intent presentation, damage and healing numbers, and blocked text. Enemy attacks may begin sooner because the presentation is shorter, but no action or effect is skipped or reordered."))
                        .AddInfoCard(
                            "progressive_acceleration_guide",
                            T("PROGRESSIVE_ACCELERATION_GUIDE", "Progressive acceleration"),
                            T("PROGRESSIVE_ACCELERATION_GUIDE_DESCRIPTION",
                                "Only affects Safe State game speed. It stays at normal speed for the Activation Delay, then gradually reaches the target over the Ramp Duration. This keeps short flows from suddenly surging and makes speed changes smoother, but reaches full speed later. It does not affect the other three multipliers."))
                        .AddInfoCard(
                            "stacking_guide",
                            T("STACKING_GUIDE", "Using multiple speed controls"),
                            T("STACKING_GUIDE_DESCRIPTION",
                                "Game speed, card animation speed, card resolution speed, and combat presentation speed are independent. Their effects can compound where their coverage overlaps. The around-3x recommendation assumes one control is used by itself; when stacking them, start lower and adjust one multiplier at a time."))),
                Const.SettingsPageId);
        }

        private static IModSettingsValueBinding<TValue> Bind<TValue>(
            string dataKey,
            Func<SpeedSettings, TValue> getter,
            Action<SpeedSettings, TValue> setter,
            Func<TValue> defaultValueFactory)
        {
            var binding = ModSettingsBindings.Callback(
                Const.ModId,
                dataKey,
                () => getter(ModDataStore.Get<SpeedSettings>(ModDataStore.SettingsKey)),
                value =>
                {
                    ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey, settings => setter(settings, value));
                    SpeedManager.OnSettingsChanged();
                },
                () => ModDataStore.Save(ModDataStore.SettingsKey));
            return ModSettingsBindings.WithDefault(binding, defaultValueFactory);
        }

        private static IModSettingsValueBinding<double> BindDouble(
            string dataKey,
            Func<SpeedSettings, float> getter,
            Action<SpeedSettings, double> setter,
            Func<float> defaultValueFactory)
        {
            return Bind(dataKey, settings => getter(settings), setter, () => defaultValueFactory());
        }

        private static ModSettingsText T(string key, string fallback)
        {
            return ModSettingsText.I18N(Main.I18N, key, fallback);
        }

        private static bool IsSafeStateMode()
        {
            return ModDataStore.Get<SpeedSettings>(ModDataStore.SettingsKey).AccelerationMode ==
                   SpeedAccelerationMode.SafeState;
        }

        private static bool IsProgressiveAccelerationVisible()
        {
            var settings = ModDataStore.Get<SpeedSettings>(ModDataStore.SettingsKey);
            return settings.AccelerationMode == SpeedAccelerationMode.SafeState &&
                   settings.ProgressiveAccelerationEnabled;
        }

        private static string FormatSeconds(double value)
        {
            return value <= 0 ? "0s" : $"{value:0.##}s";
        }
    }
}
