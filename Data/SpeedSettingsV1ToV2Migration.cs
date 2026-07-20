using System.Text.Json.Nodes;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace STS2QuickAnimationMode.Data
{
    internal sealed class SpeedSettingsV1ToV2Migration : IMigration
    {
        private const double RegressedDefaultTransitionDuration = 0.2d;
        private const double RegressedDefaultTimeThreshold = 0.05d;

        public int FromVersion => 1;
        public int ToVersion => 2;

        public bool Migrate(JsonObject data)
        {
            data["schema_version"] = ToVersion;
            EnsureTiming(
                data,
                "transition_duration",
                SpeedSettings.DefaultTransitionDuration,
                RegressedDefaultTransitionDuration,
                SpeedManager.MinTransitionDuration,
                SpeedManager.MaxTransitionDuration);
            EnsureTiming(
                data,
                "time_threshold",
                SpeedSettings.DefaultTimeThreshold,
                RegressedDefaultTimeThreshold,
                SpeedManager.MinTimeThreshold,
                SpeedManager.MaxTimeThreshold);
            return true;
        }

        private static void EnsureTiming(
            JsonObject data,
            string key,
            double defaultValue,
            double regressedDefault,
            double min,
            double max)
        {
            if (!TryGetDouble(data, key, out var value))
            {
                data[key] = defaultValue;
                return;
            }

            var migrated = IsEquivalent(value, regressedDefault)
                ? defaultValue
                : Math.Clamp(value, min, max);
            data[key] = migrated;
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

        private static bool IsEquivalent(double left, double right)
        {
            return Math.Abs(left - right) <= 0.000_001d;
        }
    }
}
