using System.Text.Json.Nodes;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2QuickAnimationMode.Data
{
    internal sealed class SpeedSettingsV0ToV1Migration : IMigration
    {
        public int FromVersion => 0;
        public int ToVersion => 1;

        public bool Migrate(JsonObject data)
        {
            data["schema_version"] = ToVersion;
            EnsureSpeedMultiplier(data);
            EnsureAccelerationMode(data);
            EnsureBool(data, "progressive_acceleration_enabled",
                SpeedSettings.DefaultProgressiveAccelerationEnabled);
            EnsureBool(data, "accelerate_card_pile_sequences",
                SpeedSettings.DefaultAccelerateCardPileSequences);
            EnsureBool(data, "accelerate_card_play_resolution",
                SpeedSettings.DefaultAccelerateCardPlayResolution);
            EnsureBool(data, "accelerate_turn_transitions",
                SpeedSettings.DefaultAccelerateTurnTransitions);
            EnsureBool(data, "accelerate_enemy_actions",
                SpeedSettings.DefaultAccelerateEnemyActions);
            EnsureBool(data, "accelerate_timeline_animations",
                SpeedSettings.DefaultAccelerateTimelineAnimations);
            EnsureBool(data, "accelerate_loading_screens",
                SpeedSettings.DefaultAccelerateLoadingScreens);
            return true;
        }

        private static void EnsureSpeedMultiplier(JsonObject data)
        {
            if (!TryGetDouble(data, "speed_multiplier", out var value))
            {
                data["speed_multiplier"] = SpeedSettings.DefaultSpeedMultiplier;
                return;
            }

            data["speed_multiplier"] = Math.Clamp(value, SpeedManager.MinSelectableMultiplier,
                SpeedManager.MaxSelectableMultiplier);
        }

        private static void EnsureAccelerationMode(JsonObject data)
        {
            if (TryGetAccelerationMode(data, "acceleration_mode", out var mode))
            {
                data["acceleration_mode"] = (int)mode;
                return;
            }

            var oldProgressiveEnabled = !TryGetBool(data, "progressive_enabled", out var progressiveEnabled) ||
                                        progressiveEnabled;
            data["acceleration_mode"] = oldProgressiveEnabled
                ? (int)SpeedAccelerationMode.SafeState
                : (int)SpeedAccelerationMode.AlwaysOn;
        }

        private static void EnsureBool(JsonObject data, string key, bool defaultValue)
        {
            data[key] = TryGetBool(data, key, out var value) ? value : defaultValue;
        }

        private static bool TryGetAccelerationMode(JsonObject data, string key, out SpeedAccelerationMode mode)
        {
            mode = SpeedAccelerationMode.SafeState;
            if (!data.TryGetPropertyValue(key, out var node) || node == null)
                return false;

            if (TryGetInt(node, out var intValue) && Enum.IsDefined(typeof(SpeedAccelerationMode), intValue))
            {
                mode = (SpeedAccelerationMode)intValue;
                return true;
            }

            if (TryGetString(node, out var stringValue) &&
                Enum.TryParse(stringValue, true, out SpeedAccelerationMode parsed))
            {
                mode = parsed;
                return true;
            }

            return false;
        }

        private static bool TryGetBool(JsonObject data, string key, out bool value)
        {
            value = false;
            return data.TryGetPropertyValue(key, out var node) && node != null && TryGetBool(node, out value);
        }

        private static bool TryGetBool(JsonNode node, out bool value)
        {
            try
            {
                value = node.GetValue<bool>();
                return true;
            }
            catch
            {
                value = false;
                return false;
            }
        }

        private static bool TryGetDouble(JsonObject data, string key, out double value)
        {
            value = 0;
            if (!data.TryGetPropertyValue(key, out var node) || node == null)
                return false;

            try
            {
                value = node.GetValue<double>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetInt(JsonNode node, out int value)
        {
            try
            {
                value = node.GetValue<int>();
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        private static bool TryGetString(JsonNode node, out string value)
        {
            try
            {
                value = node.GetValue<string>();
                return true;
            }
            catch
            {
                value = "";
                return false;
            }
        }
    }
}
