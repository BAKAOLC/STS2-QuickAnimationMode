using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.DailyRun;
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

    public class AssetLoadingSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_asset_loading";
        public static string Description => "Accelerate safe background asset loading";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NAssetLoader), nameof(NAssetLoader.LoadInTheBackground), [typeof(AssetLoadingSession)])];
        }

        public static void Postfix(ref Task<bool> __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.LoadingScreen);
        }
    }

    public class LoadingProcessSpeedPatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_loading_process";
        public static string Description => "Process speed transitions while loading screens are active";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NAssetLoader), "_Process", [typeof(double)]),
                new(typeof(NMultiplayerLoadGameScreen), "_Process", [typeof(double)], true),
                new(typeof(NCustomRunLoadScreen), "_Process", [typeof(double)], true),
                new(typeof(NDailyRunLoadScreen), "_Process", [typeof(double)], true),
            ];
        }

        public static void Postfix(double delta)
        {
            SpeedManager.ProcessFrame(delta);
        }
    }
}
