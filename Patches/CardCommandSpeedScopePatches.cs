using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    public class CardDiscardSingleSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_card_discard_single";
        public static string Description => "Accelerate safe single-card discard sequences";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardCmd), nameof(CardCmd.Discard),
                    [typeof(PlayerChoiceContext), typeof(CardModel)]),
            ];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.CardPileSequence);
        }
    }

    public class CardDiscardManySpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_card_discard_many";
        public static string Description => "Accelerate safe multi-card discard sequences";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardCmd), nameof(CardCmd.Discard),
                    [typeof(PlayerChoiceContext), typeof(IEnumerable<CardModel>)]),
                new(typeof(CardCmd), nameof(CardCmd.DiscardAndDraw),
                    [typeof(PlayerChoiceContext), typeof(IEnumerable<CardModel>), typeof(int)]),
            ];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.CardPileSequence);
        }
    }

    public class CardExhaustSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_card_exhaust";
        public static string Description => "Accelerate safe card exhaust sequences";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(CardCmd), nameof(CardCmd.Exhaust),
                    [typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool), typeof(bool)]),
            ];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.CardPileSequence);
        }
    }
}
