using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    public class CardPlayResolutionSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_card_play_resolution";
        public static string Description => "Accelerate safe card play resolution after a card has been committed";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardModel), nameof(CardModel.OnPlayWrapper),
                    [typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool)]),
            ];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.CardPlayResolution);
        }
    }
}
