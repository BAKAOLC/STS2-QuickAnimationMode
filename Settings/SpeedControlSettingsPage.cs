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
                        "Configure global speed or conservative acceleration for safe automatic sequences."))
                    .AddSection(Const.SettingsSectionId, section => section
                        .WithTitle(T("SETTINGS_SECTION_SPEED", "Basic speed"))
                        .AddChoice(
                            "mode",
                            T("MODE", "Mode"),
                            Bind("mode", settings => settings.AccelerationMode,
                                (settings, value) => settings.AccelerationMode = value),
                            [
                                new(SpeedAccelerationMode.SafeState,
                                    T("MODE_SAFE_STATE", "Safe state acceleration")),
                                new(SpeedAccelerationMode.AlwaysOn,
                                    T("MODE_ALWAYS_ON", "Global continuous acceleration")),
                                new(SpeedAccelerationMode.Off,
                                    T("MODE_OFF", "Off")),
                            ],
                            T("MODE_DESCRIPTION",
                                "Safe state acceleration only applies during whitelisted automatic sequences. Global continuous acceleration keeps the selected multiplier active."),
                            ModSettingsChoicePresentation.Dropdown)
                        .AddSlider(
                            "speed_multiplier",
                            T("SPEED_MULTIPLIER", "Speed Multiplier"),
                            BindDouble("speed_multiplier", settings => settings.SpeedMultiplier,
                                (settings, value) => settings.SpeedMultiplier = (float)value),
                            SpeedManager.MinSelectableMultiplier,
                            SpeedManager.MaxSelectableMultiplier,
                            SpeedManager.SpeedSliderStep,
                            value => $"{value:0.#}x",
                            T("SPEED_MULTIPLIER_DESCRIPTION",
                                "Target speed used immediately in global mode or when a safe automatic sequence is active.")))
                    .AddSection("safe-sequences", section => section
                        .WithTitle(T("SETTINGS_SECTION_SAFE_SEQUENCES", "Combat automation"))
                        .WithDescription(T("SETTINGS_SECTION_SAFE_SEQUENCES_DESCRIPTION",
                            "Choose which explicitly whitelisted combat flows may raise the game speed in safe state mode."))
                        .WithVisibleWhen(IsSafeStateMode)
                        .AddToggle(
                            "accelerate_card_pile_sequences",
                            T("ACCELERATE_CARD_PILE_SEQUENCES", "Card pile commands"),
                            Bind("accelerate_card_pile_sequences", settings => settings.AccelerateCardPileSequences,
                                (settings, value) => settings.AccelerateCardPileSequences = value),
                            T("ACCELERATE_CARD_PILE_SEQUENCES_DESCRIPTION",
                                "Allows safe acceleration during draw, shuffle, discard, discard-and-draw, and exhaust command execution."))
                        .AddToggle(
                            "accelerate_card_play_resolution",
                            T("ACCELERATE_CARD_PLAY_RESOLUTION", "Committed card resolution"),
                            Bind("accelerate_card_play_resolution", settings => settings.AccelerateCardPlayResolution,
                                (settings, value) => settings.AccelerateCardPlayResolution = value),
                            T("ACCELERATE_CARD_PLAY_RESOLUTION_DESCRIPTION",
                                "Allows safe acceleration after a card has been committed and is resolving its automatic effects. Local choices still pause acceleration."))
                        .AddToggle(
                            "accelerate_turn_transitions",
                            T("ACCELERATE_TURN_TRANSITIONS", "Turn transitions"),
                            Bind("accelerate_turn_transitions", settings => settings.AccelerateTurnTransitions,
                                (settings, value) => settings.AccelerateTurnTransitions = value),
                            T("ACCELERATE_TURN_TRANSITIONS_DESCRIPTION",
                                "Allows safe acceleration during player turn start, player turn end, side switching, and enemy turn cleanup sequences."))
                        .AddToggle(
                            "accelerate_enemy_actions",
                            T("ACCELERATE_ENEMY_ACTIONS", "Enemy actions"),
                            Bind("accelerate_enemy_actions", settings => settings.AccelerateEnemyActions,
                                (settings, value) => settings.AccelerateEnemyActions = value),
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
                                (settings, value) => settings.AccelerateTimelineAnimations = value),
                            T("ACCELERATE_TIMELINE_ANIMATIONS_DESCRIPTION",
                                "Allows safe acceleration during non-interactive timeline reveal, unlock, and slot animation sequences."))
                        .AddToggle(
                            "accelerate_loading_screens",
                            T("ACCELERATE_LOADING_SCREENS", "Run loading"),
                            Bind("accelerate_loading_screens", settings => settings.AccelerateLoadingScreens,
                                (settings, value) => settings.AccelerateLoadingScreens = value),
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
                                (settings, value) => settings.ProgressiveAccelerationEnabled = value),
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
                                (settings, value) => settings.TimeThreshold = (float)value),
                            0.0d,
                            2.0d,
                            0.05d,
                            FormatSeconds,
                            T("TIME_THRESHOLD_DESCRIPTION",
                                "How long an allowed safe sequence must stay active before it starts increasing above 1x."))
                        .AddSlider(
                            "transition_duration",
                            T("TRANSITION_DURATION", "Ramp Duration"),
                            BindDouble("transition_duration", settings => settings.TransitionDuration,
                                (settings, value) => settings.TransitionDuration = (float)value),
                            0.0d,
                            3.0d,
                            0.05d,
                            FormatSeconds,
                            T("TRANSITION_DURATION_DESCRIPTION",
                                "How long it takes to interpolate from 1x to the selected target speed after the activation delay."))),
                Const.SettingsPageId);
        }

        private static IModSettingsValueBinding<TValue> Bind<TValue>(
            string dataKey,
            Func<SpeedSettings, TValue> getter,
            Action<SpeedSettings, TValue> setter)
        {
            return ModSettingsBindings.Callback(
                Const.ModId,
                dataKey,
                () => getter(ModDataStore.Get<SpeedSettings>(ModDataStore.SettingsKey)),
                value =>
                {
                    ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey, settings => setter(settings, value));
                    SpeedManager.OnSettingsChanged();
                },
                () => ModDataStore.Save(ModDataStore.SettingsKey));
        }

        private static IModSettingsValueBinding<double> BindDouble(
            string dataKey,
            Func<SpeedSettings, float> getter,
            Action<SpeedSettings, double> setter)
        {
            return Bind(dataKey, settings => getter(settings), setter);
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
