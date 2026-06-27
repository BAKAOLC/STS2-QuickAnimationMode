using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    public class RunLoadingSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_run_loading";
        public static string Description => "Accelerate safe run loading sequences";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NGame), nameof(NGame.LoadRun), [typeof(RunState), typeof(SerializableRoom)])];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.LoadingScreen);
        }
    }

    public class TransitionSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_transitions";
        public static string Description => "Accelerate screen and room transition sequences";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NTransition), nameof(NTransition.FadeOut),
                    [typeof(float), typeof(string), typeof(CancellationToken?)]),
                new(typeof(NTransition), nameof(NTransition.FadeIn),
                    [typeof(float), typeof(string), typeof(CancellationToken?)]),
                new(typeof(NTransition), nameof(NTransition.RoomFadeOut), Type.EmptyTypes),
                new(typeof(NTransition), nameof(NTransition.RoomFadeIn), [typeof(bool)]),
            ];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.LoadingScreen);
        }
    }
}
