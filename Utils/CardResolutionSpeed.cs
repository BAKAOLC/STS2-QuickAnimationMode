namespace STS2QuickAnimationMode.Utils
{
    internal static class CardResolutionSpeed
    {
        private static readonly AsyncLocal<int> ResolutionDepth = new();

        public static int Enter()
        {
            var previousDepth = ResolutionDepth.Value;
            ResolutionDepth.Value = previousDepth + 1;
            return previousDepth;
        }

        public static void Restore(int depth)
        {
            ResolutionDepth.Value = Math.Max(0, depth);
        }

        public static float ScaleWait(float seconds)
        {
            if (ResolutionDepth.Value <= 0
                || !SpeedManager.IsCardResolutionAccelerationEnabled
                || !float.IsFinite(seconds)
                || seconds <= 0f)
                return seconds;

            return seconds / SpeedManager.EffectiveCardResolutionMultiplier;
        }
    }
}
