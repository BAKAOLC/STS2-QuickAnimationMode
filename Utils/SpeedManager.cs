using Godot;

namespace STS2QuickAnimationMode.Utils
{
    /// <summary>
    ///     Manages the global game speed multiplier via Engine.TimeScale.
    /// </summary>
    public static class SpeedManager
    {
        public static readonly float[] SpeedOptions = [1.0f, 1.5f, 2.0f, 3.0f, 4.0f, 5.0f, 8.0f, 10.0f];

        public static readonly string[] SpeedLabels = ["1x", "1.5x", "2x", "3x", "4x", "5x", "8x", "10x"];

        private static Setting<SpeedSettings>? _settings;

        public static float CurrentMultiplier => _settings?.Data.SpeedMultiplier ?? 1.0f;

        public static int CurrentIndex
        {
            get
            {
                var multiplier = CurrentMultiplier;
                for (var i = 0; i < SpeedOptions.Length; i++)
                    if (Mathf.IsEqualApprox(SpeedOptions[i], multiplier))
                        return i;
                return 0;
            }
        }

        public static void Initialize()
        {
            _settings = new(
                Const.SettingsPath,
                new(),
                "SpeedManager"
            );
            _settings.Load();
            ApplySpeed();
            Main.Logger.Info($"SpeedManager initialized, multiplier: {CurrentMultiplier}x");
        }

        public static void SetSpeedIndex(int index)
        {
            if (index < 0 || index >= SpeedOptions.Length) return;

            var newSpeed = SpeedOptions[index];
            _settings?.Modify(data => data.SpeedMultiplier = newSpeed);
            _settings?.Save();
            ApplySpeed();
            Main.Logger.Info($"Speed set to {newSpeed}x (index {index})");
        }

        public static void ApplySpeed()
        {
            Engine.TimeScale = CurrentMultiplier;
        }
    }
}
