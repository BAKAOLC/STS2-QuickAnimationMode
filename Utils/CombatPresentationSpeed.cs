namespace STS2QuickAnimationMode.Utils
{
    internal static class CombatPresentationSpeed
    {
        private static readonly AsyncLocal<int> AnimationDepth = new();
        private static readonly AsyncLocal<int> CustomWaitBudget = new();

        public static (int AnimationDepth, int CustomWaitBudget) Enter(bool scaleNextCustomWait)
        {
            var state = (AnimationDepth: AnimationDepth.Value, CustomWaitBudget: CustomWaitBudget.Value);
            AnimationDepth.Value = state.AnimationDepth + 1;
            if (scaleNextCustomWait)
                CustomWaitBudget.Value = state.CustomWaitBudget + 1;

            return state;
        }

        public static void Restore((int AnimationDepth, int CustomWaitBudget) state)
        {
            AnimationDepth.Value = Math.Max(0, state.AnimationDepth);
            CustomWaitBudget.Value = Math.Max(0, state.CustomWaitBudget);
        }

        public static void MarkNextCustomWait()
        {
            if (SpeedManager.IsCombatPresentationAccelerationEnabled)
                CustomWaitBudget.Value++;
        }

        public static double ScaleDuration(double duration)
        {
            if (AnimationDepth.Value <= 0
                || !SpeedManager.IsCombatPresentationAccelerationEnabled
                || !double.IsFinite(duration)
                || duration <= 0d)
                return duration;

            return duration / SpeedManager.EffectiveCombatPresentationMultiplier;
        }

        public static void ScaleCustomWait(ref float fastSeconds, ref float standardSeconds)
        {
            if (CustomWaitBudget.Value <= 0)
                return;

            CustomWaitBudget.Value--;
            if (!SpeedManager.IsCombatPresentationAccelerationEnabled
                || !float.IsFinite(fastSeconds)
                || !float.IsFinite(standardSeconds))
                return;

            var multiplier = SpeedManager.EffectiveCombatPresentationMultiplier;
            if (fastSeconds > 0f)
                fastSeconds /= multiplier;
            if (standardSeconds > 0f)
                standardSeconds /= multiplier;
        }

        public static double ScaleProcessDelta(double delta)
        {
            if (!SpeedManager.IsCombatPresentationAccelerationEnabled
                || !double.IsFinite(delta)
                || delta <= 0d)
                return delta;

            return delta * SpeedManager.EffectiveCombatPresentationMultiplier;
        }
    }
}
