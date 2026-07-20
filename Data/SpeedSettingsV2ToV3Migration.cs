using System.Text.Json.Nodes;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2QuickAnimationMode.Data
{
    internal sealed class SpeedSettingsV2ToV3Migration : IMigration
    {
        public int FromVersion => 2;
        public int ToVersion => SpeedSettings.CurrentSchemaVersion;

        public bool Migrate(JsonObject data)
        {
            data["schema_version"] = ToVersion;
            EnsureBool(data, "card_animation_acceleration_enabled",
                SpeedSettings.DefaultCardAnimationAccelerationEnabled);
            EnsureDouble(data, "card_animation_multiplier", SpeedSettings.DefaultCardAnimationMultiplier);
            EnsureBool(data, "card_resolution_acceleration_enabled",
                SpeedSettings.DefaultCardResolutionAccelerationEnabled);
            EnsureDouble(data, "card_resolution_multiplier", SpeedSettings.DefaultCardResolutionMultiplier);
            EnsureBool(data, "combat_presentation_acceleration_enabled",
                SpeedSettings.DefaultCombatPresentationAccelerationEnabled);
            EnsureDouble(data, "combat_presentation_multiplier", SpeedSettings.DefaultCombatPresentationMultiplier);
            return true;
        }

        private static void EnsureBool(JsonObject data, string key, bool defaultValue)
        {
            if (!TryGetBool(data, key, out var value))
                value = defaultValue;

            data[key] = value;
        }

        private static void EnsureDouble(JsonObject data, string key, double defaultValue)
        {
            var value = TryGetDouble(data, key, out var parsed) ? parsed : defaultValue;
            data[key] = Math.Clamp(value, SpeedManager.MinSelectableMultiplier,
                SpeedManager.MaxSelectableMultiplier);
        }

        private static bool TryGetBool(JsonObject data, string key, out bool value)
        {
            value = false;
            if (!data.TryGetPropertyValue(key, out var node) || node == null)
                return false;

            try
            {
                value = node.GetValue<bool>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetDouble(JsonObject data, string key, out double value)
        {
            value = 0d;
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
    }
}
