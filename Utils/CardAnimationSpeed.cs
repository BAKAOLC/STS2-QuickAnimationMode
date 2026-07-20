using System.Runtime.CompilerServices;
using Godot;

namespace STS2QuickAnimationMode.Utils
{
    internal static class CardAnimationSpeed
    {
        private static readonly ConditionalWeakTable<Tween, object> CardTweens = new();

        public static float Multiplier => SpeedManager.EffectiveCardAnimationMultiplier;

        public static void MarkCardTween(Tween tween)
        {
            CardTweens.GetValue(tween, static _ => new());
        }

        public static bool IsCardTween(Tween tween)
        {
            return CardTweens.TryGetValue(tween, out _);
        }

        public static double ScaleDuration(double duration)
        {
            if (!SpeedManager.IsCardAnimationAccelerationEnabled || !double.IsFinite(duration) || duration <= 0d)
                return duration;

            return duration / Multiplier;
        }

        public static float ScaleDuration(float duration)
        {
            if (!SpeedManager.IsCardAnimationAccelerationEnabled || !float.IsFinite(duration) || duration <= 0f)
                return duration;

            return duration / Multiplier;
        }

        public static void ScaleKinematics(ref float speed, ref float acceleration)
        {
            if (!SpeedManager.IsCardAnimationAccelerationEnabled)
                return;

            var multiplier = Multiplier;
            speed *= multiplier;
            acceleration *= multiplier * multiplier;
        }
    }
}
