using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches;

public class HandAddStateRepairPatch : IPatchMethod
{
    public static string PatchId => "hand_add_state_repair";
    public static string Description => "Normalize hand card visuals when a card holder is added";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(NPlayerHand), nameof(NPlayerHand.Add), [typeof(NCard), typeof(int)])];
    }

    public static void Postfix(NCard card, NHandCardHolder __result)
    {
        HandStateRepair.NormalizeHandCard(card, __result);
        HandStateRepair.RequestFullRepair();
    }
}

public class ReturnHolderToHandStateRepairPatch : IPatchMethod
{
    public static string PatchId => "return_holder_to_hand_state_repair";
    public static string Description => "Normalize hand card visuals when an awaiting holder returns to the hand";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(NPlayerHand), "ReturnHolderToHand", [typeof(NHandCardHolder)], true)];
    }

    public static void Postfix(NHandCardHolder holder)
    {
        HandStateRepair.NormalizeHandCard(holder?.CardNode, holder);
        HandStateRepair.RequestFullRepair();
    }
}

public class CardPlayCleanupStateRepairPatch : IPatchMethod
{
    public static string PatchId => "card_play_cleanup_state_repair";
    public static string Description => "Schedule hand state repair after local card play cleanup";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(NCardPlay), "Cleanup", [typeof(bool)], true)];
    }

    public static void Postfix(bool isFinished)
    {
        if (isFinished)
            return;

        HandStateRepair.RequestFullRepair();
    }
}

public class CardPileAddSingleStateRepairPatch : IPatchMethod
{
    public static string PatchId => "card_pile_add_single_state_repair";
    public static string Description => "Schedule hand state repair after single-card pile changes";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(CardPileCmd), nameof(CardPileCmd.Add),
            [
                typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)
            ]),
            new ModPatchTarget(typeof(CardPileCmd), nameof(CardPileCmd.Add),
            [
                typeof(CardModel), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)
            ])
        ];
    }

    public static void Postfix(ref Task<CardPileAddResult> __result)
    {
        __result = HandStateRepair.RepairAfterAsync(__result);
    }
}

public class CardPileAddManyStateRepairPatch : IPatchMethod
{
    public static string PatchId => "card_pile_add_many_state_repair";
    public static string Description => "Schedule hand state repair after multi-card pile changes";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        var cardPileAddParameters = ResolveCardPileAddManyParameters();
        return
        [
            new ModPatchTarget(typeof(CardPileCmd), nameof(CardPileCmd.Add),
            [
                typeof(IEnumerable<CardModel>), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel),
                typeof(bool)
            ]),
            new ModPatchTarget(typeof(CardPileCmd), nameof(CardPileCmd.Add), cardPileAddParameters)
        ];
    }

    public static void Postfix(ref Task<IReadOnlyList<CardPileAddResult>> __result)
    {
        __result = HandStateRepair.RepairAfterAsync(__result);
    }

    private static Type[] ResolveCardPileAddManyParameters()
    {
        Type[] currentParameters =
        [
            typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel),
            typeof(bool), typeof(bool)
        ];

        return typeof(CardPileCmd).GetMethod(nameof(CardPileCmd.Add), currentParameters) != null
            ? currentParameters
            :
            [
                typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel),
                typeof(bool)
            ];
    }
}

public class CardTransformStateRepairGuardPatch : IPatchMethod
{
    public static string PatchId => "card_transform_state_repair_guard";
    public static string Description => "Defer hand state repair while card transformations update their holders";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(CardCmd), nameof(CardCmd.Transform),
            [
                typeof(IEnumerable<CardTransformation>), typeof(Rng), typeof(CardPreviewStyle)
            ])
        ];
    }

    public static void Postfix(ref Task<IEnumerable<CardPileAddResult>> __result)
    {
        __result = HandStateRepair.GuardDuringHandMutationAsync(__result);
    }
}