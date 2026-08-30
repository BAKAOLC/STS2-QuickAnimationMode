using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace STS2QuickAnimationMode.Utils
{
    internal static class HandStateRepair
    {
        private const int DeferredRepairFrames = 2;
        private static int _pendingRepairFrames;
        private static int _activeHandMutationDepth;

        public static void RequestFullRepair()
        {
            if (!SpeedManager.AreCardBehaviorPatchesEnabled)
                return;

            _pendingRepairFrames = Math.Max(_pendingRepairFrames, DeferredRepairFrames);
        }

        public static Task RepairAfterAsync(Task task)
        {
            return SpeedManager.AreCardBehaviorPatchesEnabled ? RepairAfterAsyncCore(task) : task;
        }

        public static Task<T> RepairAfterAsync<T>(Task<T> task)
        {
            return SpeedManager.AreCardBehaviorPatchesEnabled ? RepairAfterAsyncCore(task) : task;
        }

        public static Task<T> GuardDuringHandMutationAsync<T>(Task<T> task)
        {
            return SpeedManager.AreCardBehaviorPatchesEnabled ? GuardDuringHandMutationAsyncCore(task) : task;
        }

        public static void ProcessFrame()
        {
            if (!SpeedManager.AreCardBehaviorPatchesEnabled)
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

        public static void NormalizeHandCard(NCard? card, NHandCardHolder? holder)
        {
            try
            {
                if (!SpeedManager.AreCardBehaviorPatchesEnabled)
                    return;

                if (IsRepairUnsafeNow())
                {
                    RequestFullRepair();
                    return;
                }

                if (card == null || !GodotObject.IsInstanceValid(card))
                    return;

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

        private static async Task<T> GuardDuringHandMutationAsyncCore<T>(Task<T> task)
        {
            _activeHandMutationDepth++;
            try
            {
                return await task;
            }
            finally
            {
                _activeHandMutationDepth = Math.Max(0, _activeHandMutationDepth - 1);
                RequestFullRepair();
            }
        }

        private static void RepairLocalHand()
        {
            try
            {
                if (!SpeedManager.AreCardBehaviorPatchesEnabled || CombatManager.Instance?.IsInProgress != true)
                    return;

                if (IsRepairUnsafeNow())
                {
                    RequestFullRepair();
                    return;
                }

                var hand = NPlayerHand.Instance;
                if (hand == null || hand.CurrentMode != NPlayerHand.Mode.Play || hand.InCardPlay)
                    return;

                foreach (var holder in hand.CardHolderContainer.GetChildren().OfType<NHandCardHolder>().ToList())
                {
                    if (!GodotObject.IsInstanceValid(holder) || holder.GetParent() != hand.CardHolderContainer)
                        continue;

                    var card = holder.CardNode;
                    var model = card?.Model;
                    if (card == null
                        || !GodotObject.IsInstanceValid(card)
                        || model == null
                        || model.Pile?.Type != PileType.Hand
                        || hand.GetCardHolder(model) != holder
                        || hand.IsAwaitingPlay(holder))
                        continue;

                    NormalizeHandCard(card, holder);
                }
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Full hand state repair failed: {ex}");
            }
        }

        private static bool IsRepairUnsafeNow()
        {
            if (_activeHandMutationDepth > 0)
                return true;

            if (CombatManager.Instance?.IsInProgress != true)
                return false;

            var hand = NPlayerHand.Instance;
            if (hand is { InCardPlay: true } || hand?.IsInCardSelection == true)
                return true;

            if (NOverlayStack.Instance?.Peek() is ICardSelector
                || NRun.Instance?.GlobalUi?.TargetManager?.IsInSelection == true)
                return true;

            if (IsCardPreviewActive())
                return true;

            var run = RunManager.Instance;
            if (!run.IsInProgress)
                return false;

            return run.ActionExecutor.CurrentlyRunningAction?.State is
                GameActionState.Executing
                or GameActionState.GatheringPlayerChoice
                or GameActionState.ReadyToResumeExecuting;
        }

        private static bool IsCardPreviewActive()
        {
            var combatUi = NCombatRoom.Instance?.Ui;
            if ((combatUi?.CardPreviewContainer.GetChildCount() ?? 0) > 0
                || (combatUi?.MessyCardPreviewContainer.GetChildCount() ?? 0) > 0)
                return true;

            var globalUi = NRun.Instance?.GlobalUi;
            return (globalUi?.CardPreviewContainer.GetChildCount() ?? 0) > 0
                   || (globalUi?.MessyCardPreviewContainer.GetChildCount() ?? 0) > 0
                   || (globalUi?.EventCardPreviewContainer.GetChildCount() ?? 0) > 0
                   || (globalUi?.GridCardPreviewContainer.GetChildCount() ?? 0) > 0;
        }
    }
}
