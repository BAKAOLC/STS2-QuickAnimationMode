using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    public class CardPileDrawSingleSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_card_pile_draw_single";
        public static string Description => "Accelerate safe single-card draw sequences";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPileCmd), nameof(CardPileCmd.Draw),
                    [typeof(PlayerChoiceContext), typeof(Player)]),
            ];
        }

        public static void Postfix(ref Task<CardModel?> __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.CardPileSequence);
        }
    }

    public class CardPileDrawManySpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_card_pile_draw_many";
        public static string Description => "Accelerate safe multi-card draw sequences";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPileCmd), nameof(CardPileCmd.Draw),
                    [typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool)]),
            ];
        }

        public static void Postfix(ref Task<IEnumerable<CardModel>> __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.CardPileSequence);
        }
    }

    public class CardPileShuffleSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_card_pile_shuffle";
        public static string Description => "Accelerate safe shuffle sequences";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardPileCmd), nameof(CardPileCmd.Shuffle),
                    [typeof(PlayerChoiceContext), typeof(Player)]),
                new(typeof(CardPileCmd), nameof(CardPileCmd.ShuffleIfNecessary),
                    [typeof(PlayerChoiceContext), typeof(Player)]),
            ];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.CardPileSequence);
        }
    }
}
