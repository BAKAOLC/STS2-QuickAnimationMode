using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2QuickAnimationMode.Patching.Models;
using STS2QuickAnimationMode.Utils;

namespace STS2QuickAnimationMode.Patches
{
    /// <summary>
    ///     Re-applies speed multiplier when settings screen closes,
    ///     ensuring our TimeScale persists.
    /// </summary>
    public class SettingsClosePatch : IPatchMethod
    {
        public static string PatchId => "settings_close_reapply";
        public static string Description => "Re-apply speed multiplier when settings close";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NSettingsScreen), nameof(NSettingsScreen.OnSubmenuClosed)),
            ];
        }

        public static void Postfix()
        {
            SpeedManager.ApplySpeed();
        }
    }
}
