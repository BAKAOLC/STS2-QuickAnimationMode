using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace STS2QuickAnimationMode.Utils
{
    internal static class HandStateRepair
    {
        private const int DeferredRepairFrames = 2;
        private static int _pendingRepairFrames;

        public static void RequestFullRepair()
        {
            if (!SpeedManager.AreBehaviorPatchesEnabled)
                return;

            _pendingRepairFrames = Math.Max(_pendingRepairFrames, DeferredRepairFrames);
        }

        public static Task RepairAfterAsync(Task task)
        {
            return SpeedManager.AreBehaviorPatchesEnabled ? RepairAfterAsyncCore(task) : task;
        }

        public static Task<T> RepairAfterAsync<T>(Task<T> task)
        {
            return SpeedManager.AreBehaviorPatchesEnabled ? RepairAfterAsyncCore(task) : task;
        }

        public static void ProcessFrame()
        {
            if (!SpeedManager.AreBehaviorPatchesEnabled)
            {
                _pendingRepairFrames = 0;
                return;
            }

            if (_pendingRepairFrames <= 0)
                return;

            if (IsRepairUnsafeNow())
            {
                _pendingRepairFrames = Math.Max(_pendingRepairFrames, DeferredRepairFrames);
                return;
            }

            _pendingRepairFrames--;
            if (_pendingRepairFrames <= 0)
                RepairLocalHand();
        }

        public static void NormalizeHandCard(NCard? card, NHandCardHolder? holder, bool resetHolderInteraction = false)
        {
            try
            {
                if (!SpeedManager.AreBehaviorPatchesEnabled)
                    return;

                if (IsRepairUnsafeNow())
                {
                    RequestFullRepair();
                    return;
                }

                if (card == null || !GodotObject.IsInstanceValid(card))
                    return;

                if (card.PlayPileTween != null && GodotObject.IsInstanceValid(card.PlayPileTween))
                    card.PlayPileTween.Kill();

                card.PlayPileTween = null;
                card.Visible = true;
                card.Modulate = Colors.White;
                card.Scale = Vector2.One;
                card.Rotation = 0f;
                card.Visibility = ModelVisibility.Visible;
                card.SetPretendCardCanBePlayed(false);
                card.SetForceUnpoweredPreview(false);

                if (card.IsNodeReady() && GodotObject.IsInstanceValid(card.Body))
                {
                    card.Body.Visible = true;
                    card.Body.Modulate = Colors.White;
                    card.Body.Scale = Vector2.One;
                }

                if (card.IsNodeReady())
                {
                    card.SetPreviewTarget(null);
                    card.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);
                }

                if (holder == null || !GodotObject.IsInstanceValid(holder))
                    return;

                if (resetHolderInteraction)
                {
                    holder.Visible = true;
                    holder.Modulate = Colors.White;
                    holder.Hitbox.MouseFilter = Control.MouseFilterEnum.Stop;
                    holder.Hitbox.SetEnabled(true);
                }

                holder.UpdateCard();
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Hand state repair failed: {ex}");
            }
        }

        private static async Task RepairAfterAsyncCore(Task task)
        {
            try
            {
                await task;
            }
            finally
            {
                RequestFullRepair();
            }
        }

        private static async Task<T> RepairAfterAsyncCore<T>(Task<T> task)
        {
            try
            {
                return await task;
            }
            finally
            {
                RequestFullRepair();
            }
        }

        private static void RepairLocalHand()
        {
            try
            {
                if (!SpeedManager.AreBehaviorPatchesEnabled || CombatManager.Instance?.IsInProgress != true)
                    return;

                if (IsRepairUnsafeNow())
                {
                    RequestFullRepair();
                    return;
                }

                var hand = NPlayerHand.Instance;
                var state = CombatManager.Instance.DebugOnlyGetState();
                var player = LocalContext.GetMe(state);
                if (hand == null || player == null || hand.CurrentMode != NPlayerHand.Mode.Play || hand.InCardPlay)
                    return;

                var handPile = PileType.Hand.GetPile(player);
                var handCards = handPile.Cards.ToList();
                RemoveStaleVisibleHolders(hand, handCards);

                foreach (var card in handCards)
                {
                    var holder = EnsureHandHolder(hand, handCards, card);
                    if (holder == null)
                        continue;

                    NormalizeHandCard(holder.CardNode, holder, true);
                }

                hand.ForceRefreshCardIndices();
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Full hand state repair failed: {ex}");
            }
        }

        private static NHandCardHolder? EnsureHandHolder(
            NPlayerHand hand,
            IReadOnlyList<CardModel> handCards,
            CardModel card
        )
        {
            if (hand.GetCardHolder(card) is NHandCardHolder awaitingHolder && hand.IsAwaitingPlay(awaitingHolder))
                return null;

            if (hand.GetCardHolder(card) is NHandCardHolder holder)
            {
                if (holder.GetParent() != hand.CardHolderContainer)
                {
                    holder.Reparent(hand.CardHolderContainer);
                    MoveHolderToBackendOrder(hand, handCards, holder);
                    holder.SetDefaultTargets();
                }

                return holder;
            }

            var cardNode = NCard.FindOnTable(card);
            var playQueue = NCombatRoom.Instance?.Ui.PlayQueue;
            if (cardNode != null && playQueue?.GetCardNode(card) == cardNode)
                return null;

            cardNode ??= NCard.Create(card);
            return cardNode == null ? null : hand.Add(cardNode, GetHandInsertIndex(hand, handCards, card));
        }

        private static void MoveHolderToBackendOrder(
            NPlayerHand hand,
            IReadOnlyList<CardModel> handCards,
            NHandCardHolder holder
        )
        {
            if (holder.CardNode?.Model == null)
                return;

            var insertIndex = GetHandInsertIndex(hand, handCards, holder.CardNode.Model);
            if (insertIndex >= 0)
                hand.CardHolderContainer.MoveChildSafely(holder, insertIndex);
        }

        private static int GetHandInsertIndex(
            NPlayerHand hand,
            IReadOnlyList<CardModel> handCards,
            CardModel card
        )
        {
            var presentCards = hand.CardHolderContainer
                .GetChildren()
                .OfType<NHandCardHolder>()
                .Select(holder => holder.CardNode?.Model)
                .Where(model => model != null)
                .Cast<CardModel>();

            return HandLayoutHelper.GetInsertIndex(handCards, presentCards, card);
        }

        private static void RemoveStaleVisibleHolders(NPlayerHand hand, IReadOnlyCollection<CardModel> handCards)
        {
            foreach (var holder in hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().ToList())
            {
                var card = holder.CardNode?.Model;
                if (card != null && handCards.Contains(card) && card.Pile?.Type == PileType.Hand)
                    continue;

                hand.RemoveCardHolder(holder);
            }
        }

        private static bool IsRepairUnsafeNow()
        {
            if (CombatManager.Instance?.IsInProgress != true)
                return false;

            var hand = NPlayerHand.Instance;
            if (hand is { InCardPlay: true } || hand?.IsInCardSelection == true)
                return true;

            var run = RunManager.Instance;
            if (!run.IsInProgress || run.IsSingleplayerOrFakeMultiplayer)
                return false;

            return run.ActionExecutor.CurrentlyRunningAction?.State is
                GameActionState.Executing
                or GameActionState.GatheringPlayerChoice
                or GameActionState.ReadyToResumeExecuting;
        }
    }
}
