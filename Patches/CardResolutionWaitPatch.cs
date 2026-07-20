using MegaCrit.Sts2.Core.Commands;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    public class CardResolutionWaitPatch : IPatchMethod
    {
        public static string PatchId => "card_resolution_wait";
        public static string Description => "Scale waits inside committed card resolution without changing time scale";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Cmd), nameof(Cmd.Wait), [typeof(float), typeof(CancellationToken), typeof(bool)]),
            ];
        }

        public static void Prefix(ref float seconds)
        {
            seconds = CardResolutionSpeed.ScaleWait(seconds);
        }
    }
}
