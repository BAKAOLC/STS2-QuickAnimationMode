using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using MegaCrit.Sts2.Core.Timeline;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    public class TimelineTaskSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_task_animations";
        public static string Description => "Accelerate safe timeline task-based animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NTimelineScreen), nameof(NTimelineScreen.SpawnFirstTimeTimeline), Type.EmptyTypes, true),
                new(typeof(NTimelineScreen), nameof(NTimelineScreen.AddEpochSlots),
                    [typeof(List<EpochSlotData>), typeof(bool)], true),
                new(typeof(NEpochSlot), nameof(NEpochSlot.SpawnSlot), Type.EmptyTypes, true),
                new(typeof(NEpochInspectScreen), nameof(NEpochInspectScreen.Open),
                    [typeof(NEpochSlot), typeof(EpochModel), typeof(bool)], true),
                new(typeof(NEpochInspectScreen), nameof(NEpochInspectScreen.UnlockAnimation), [typeof(EpochModel)],
                    true),
                new(typeof(NUnlockScreen), "Close", Type.EmptyTypes, true),
            ];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.TimelineAnimation);
        }
    }

    public class TimelineUnlockScreenOpenSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_screen_open";
        public static string Description => "Accelerate safe timeline unlock screen opening animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NUnlockScreen), nameof(NUnlockScreen.Open), Type.EmptyTypes, true)];
        }

        public static void Postfix()
        {
            SpeedManager.ActivateTimed(SafeSpeedReason.TimelineAnimation);
        }
    }

    public class TimelineProcessSpeedPatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_process";
        public static string Description => "Process speed transitions while the timeline screen is active";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NSlotsContainer), "_Process", [typeof(double)], true)];
        }

        public static void Postfix(double delta)
        {
            SpeedManager.ProcessFrame(delta);
        }
    }
}
