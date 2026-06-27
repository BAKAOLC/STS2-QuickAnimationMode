using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    internal static class TimelineScreenSpeedScopeState
    {
        private static IDisposable? _scope;

        public static void Begin()
        {
            _scope ??= SpeedManager.BeginScope(SafeSpeedReason.TimelineAnimation);
        }

        public static void End()
        {
            _scope?.Dispose();
            _scope = null;
            SpeedManager.ClearReason(SafeSpeedReason.TimelineAnimation);
        }
    }

    public class TimelineScreenOpenSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_screen_open";
        public static string Description => "Accelerate while the timeline screen is open";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTimelineScreen), nameof(NTimelineScreen.OnSubmenuOpened), Type.EmptyTypes)];
        }

        public static void Postfix()
        {
            TimelineScreenSpeedScopeState.Begin();
        }
    }

    public class TimelineScreenCloseSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_screen_close";
        public static string Description => "Clear timeline acceleration when leaving the timeline screen";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTimelineScreen), nameof(NTimelineScreen.OnSubmenuClosed), Type.EmptyTypes)];
        }

        public static void Postfix()
        {
            TimelineScreenSpeedScopeState.End();
        }
    }
}
