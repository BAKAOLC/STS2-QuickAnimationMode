using MegaCrit.Sts2.Core.Nodes;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    /// <summary>
    ///     Installs a mod-owned process pump under NGame so speed state is updated in every game scene.
    /// </summary>
    public class SpeedProcessPumpInstallPatch : IPatchMethod
    {
        public static string PatchId => "speed_process_pump_install";
        public static string Description => "Install persistent speed process pump";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NGame), "_Ready", Type.EmptyTypes),
            ];
        }

        public static void Postfix()
        {
            SpeedManager.EnsureProcessPump();
        }
    }
}
