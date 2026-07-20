using System.Reflection;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline;
using MegaCrit.Sts2.Core.Nodes.Screens.Timeline.UnlockScreens;
using MegaCrit.Sts2.Core.Timeline;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    internal static class TimelineSlotsContainerStabilizer
    {
        private const float MoveSmoothness = 20f;
        private const float BounceBackStrength = 36f;

        private static readonly FieldInfo? WhatsMovedField =
            typeof(NSlotsContainer).GetField("_whatsMoved", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? TargetPositionField =
            typeof(NSlotsContainer).GetField("_targetPosition", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? IsDraggingField =
            typeof(NSlotsContainer).GetField("_isDragging", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? EpochSlotsField =
            typeof(NSlotsContainer).GetField("_epochSlots", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool TryProcess(NSlotsContainer slotsContainer, double delta)
        {
            if (!SpeedManager.AreGlobalBehaviorPatchesEnabled
                || WhatsMovedField?.GetValue(slotsContainer) is not Control whatsMoved
                || TargetPositionField?.GetValue(slotsContainer) is not Vector2 targetPosition
                || IsDraggingField?.GetValue(slotsContainer) is not bool isDragging
                || EpochSlotsField?.GetValue(slotsContainer) is not Control epochSlots)
                return false;

            whatsMoved.Position = whatsMoved.Position.Lerp(targetPosition, Step(delta, MoveSmoothness));

            if (!isDragging)
            {
                var x = targetPosition.X;
                var minX = epochSlots.Position.X - whatsMoved.Size.X;
                var maxX = epochSlots.Position.X + epochSlots.Size.X - whatsMoved.Size.X;

                if (x < minX)
                    x = Mathf.Lerp(x, minX, Step(delta, BounceBackStrength));
                else if (x > maxX)
                    x = Mathf.Lerp(x, maxX, Step(delta, BounceBackStrength));

                targetPosition = new(x, targetPosition.Y);
                TargetPositionField.SetValue(slotsContainer, targetPosition);
            }

            return true;
        }

        private static float Step(double delta, float speed)
        {
            return Mathf.Clamp((float)delta * speed, 0f, 1f);
        }
    }

    internal static class TimelineEraColumnAnimationTracker
    {
        private static readonly FieldInfo? IconField =
            typeof(NEraColumn).GetField("_icon", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? IconTweenField =
            typeof(NEraColumn).GetField("_iconTween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? NameField =
            typeof(NEraColumn).GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? YearField =
            typeof(NEraColumn).GetField("_year", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? LabelTweenField =
            typeof(NEraColumn).GetField("_labelTween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? LabelSpawnedField =
            typeof(NEraColumn).GetField("_labelSpawned", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? IsAnimatedField =
            typeof(NEraColumn).GetField("_isAnimated", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? PrevLocalPosField =
            typeof(NEraColumn).GetField("_prevLocalPos", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? PrevGlobalPosField =
            typeof(NEraColumn).GetField("_prevGlobalPos", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? PredictedPositionField =
            typeof(NEraColumn).GetField("_predictedPosition", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? TargetPositionField =
            typeof(NEraColumn).GetField("_targetPosition", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool TrySpawnIcon(NEraColumn column)
        {
            if (!SpeedManager.AreGlobalBehaviorPatchesEnabled
                || IconField?.GetValue(column) is not TextureRect icon
                || IconTweenField == null)
                return false;

            TaskHelper.RunSafely(SpawnIconTracked(column, icon));
            return true;
        }

        public static bool TrySpawnNameAndYear(NEraColumn column, ref Task result)
        {
            if (!SpeedManager.AreGlobalBehaviorPatchesEnabled
                || NameField?.GetValue(column) is not MegaLabel name
                || YearField?.GetValue(column) is not MegaLabel year
                || LabelTweenField == null
                || LabelSpawnedField == null)
                return false;

            result = SpawnNameAndYearTracked(column, name, year);
            return true;
        }

        public static bool TrySaveBeforeAnimationPosition(NEraColumn column, ref Task result)
        {
            if (!SpeedManager.AreGlobalBehaviorPatchesEnabled
                || IsAnimatedField == null
                || PrevLocalPosField == null
                || PrevGlobalPosField == null
                || PredictedPositionField == null
                || TargetPositionField == null)
                return false;

            result = SaveBeforeAnimationPositionTracked(column);
            return true;
        }

        private static async Task SpawnIconTracked(NEraColumn column, TextureRect icon)
        {
            using var scope = SpeedManager.BeginScope(SafeSpeedReason.TimelineAnimation);
            var tween = column.CreateTween().SetParallel();
            IconTweenField?.SetValue(column, tween);
            tween.TweenProperty(icon, "modulate:a", 1f, 0.5);
            tween.TweenProperty(icon, "scale", Vector2.One, 0.5).From(Vector2.One * 0.1f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            await tween.AwaitFinished(column);
        }

        private static async Task SpawnNameAndYearTracked(NEraColumn column, MegaLabel name, MegaLabel year)
        {
            if (LabelSpawnedField?.GetValue(column) is true)
                return;

            LabelSpawnedField?.SetValue(column, true);

            using (SpeedManager.BeginScope(SafeSpeedReason.TimelineAnimation))
            {
                var tween = column.CreateTween().SetParallel();
                LabelTweenField?.SetValue(column, tween);
                name.SelfModulate = new(name.SelfModulate.R, name.SelfModulate.G, name.SelfModulate.B, 0f);
                year.Modulate = new(year.Modulate.R, year.Modulate.G, year.Modulate.B, 0f);
                tween.TweenProperty(name, "self_modulate:a", 1f, 1.0);
                tween.TweenProperty(name, "position:y", 28f, 1.0).From(-36f)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
                tween.TweenProperty(year, "modulate:a", 1f, 1.0).SetDelay(0.5);
                tween.TweenProperty(year, "position:y", 20f, 1.0).SetDelay(0.5).From(0f)
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
                await tween.AwaitFinished(column);
            }

            await Task.Delay(500);
        }

        private static async Task SaveBeforeAnimationPositionTracked(NEraColumn column)
        {
            IsAnimatedField?.SetValue(column, true);
            PrevLocalPosField?.SetValue(column, column.Position);
            PrevGlobalPosField?.SetValue(column, column.GlobalPosition);
            await column.AwaitProcessFrame();
            IsAnimatedField?.SetValue(column, false);

            var targetPosition = (Vector2)(PredictedPositionField?.GetValue(column) ?? column.Position);
            var previousGlobalPosition = (Vector2)(PrevGlobalPosField?.GetValue(column) ?? column.GlobalPosition);
            TargetPositionField?.SetValue(column, targetPosition);
            column.GlobalPosition = previousGlobalPosition;

            using var scope = SpeedManager.BeginScope(SafeSpeedReason.TimelineAnimation);
            var tween = column.CreateTween().SetParallel();
            tween.TweenProperty(column, "position", targetPosition, 2.0)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Cubic);
            await tween.AwaitFinished(column);
        }
    }

    internal static class TimelineTutorialAnimationTracker
    {
        private static readonly FieldInfo? TweenField =
            typeof(NTimelineTutorial).GetField("_tween", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void TrackTween(NTimelineTutorial tutorial)
        {
            if (!SpeedManager.AreGlobalBehaviorPatchesEnabled
                || TweenField?.GetValue(tutorial) is not Tween tween)
                return;

            TaskHelper.RunSafely(TrackTweenAsync(tutorial, tween));
        }

        private static async Task TrackTweenAsync(NTimelineTutorial tutorial, Tween tween)
        {
            using var scope = SpeedManager.BeginScope(SafeSpeedReason.TimelineAnimation);
            await tween.AwaitFinished(tutorial);
        }
    }

    internal static class TimelineUnlockInfoAnimationTracker
    {
        private static readonly FieldInfo? TweenField =
            typeof(NUnlockInfo).GetField("_tween", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void TrackTween(NUnlockInfo unlockInfo)
        {
            if (!SpeedManager.AreGlobalBehaviorPatchesEnabled
                || TweenField?.GetValue(unlockInfo) is not Tween tween)
                return;

            TaskHelper.RunSafely(TrackTweenAsync(unlockInfo, tween));
        }

        public static Task TrackAfterAsync(Task task, NUnlockInfo unlockInfo)
        {
            return SpeedManager.AreGlobalBehaviorPatchesEnabled
                ? TrackAfterAsyncCore(task, unlockInfo)
                : task;
        }

        private static async Task TrackAfterAsyncCore(Task task, NUnlockInfo unlockInfo)
        {
            await SpeedManager.TrackAsync(task, SafeSpeedReason.TimelineAnimation);
            TrackTween(unlockInfo);
        }

        private static async Task TrackTweenAsync(NUnlockInfo unlockInfo, Tween tween)
        {
            using var scope = SpeedManager.BeginScope(SafeSpeedReason.TimelineAnimation);
            await tween.AwaitFinished(unlockInfo);
        }
    }

    internal static class TimelineUnlockScreenAnimationTracker
    {
        private static readonly FieldInfo? BaseTweenField =
            typeof(NUnlockScreen).GetField("_tween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? CardsTweenField =
            typeof(NUnlockCardsScreen).GetField("_cardTween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? RelicsTweenField =
            typeof(NUnlockRelicsScreen).GetField("_relicTween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? PotionsTweenField =
            typeof(NUnlockPotionsScreen).GetField("_potionTween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? CharacterTweenField =
            typeof(NUnlockCharacterScreen).GetField("_tween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? EpochTweenField =
            typeof(NUnlockEpochScreen).GetField("_cardFlyTween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? MiscTweenField =
            typeof(NUnlockMiscScreen).GetField("_tween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? BannerTweenField =
            typeof(NCommonBanner).GetField("_tween", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo? BannerLabelTweenField =
            typeof(NCommonBanner).GetField("_labelTween", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void TrackBaseTween(NUnlockScreen screen)
        {
            TrackTween(screen, BaseTweenField?.GetValue(screen));
        }

        public static void TrackOpenTweens(NUnlockScreen screen)
        {
            TrackBaseTween(screen);

            var tween = screen switch
            {
                NUnlockCardsScreen cardsScreen => CardsTweenField?.GetValue(cardsScreen),
                NUnlockRelicsScreen relicsScreen => RelicsTweenField?.GetValue(relicsScreen),
                NUnlockPotionsScreen potionsScreen => PotionsTweenField?.GetValue(potionsScreen),
                NUnlockCharacterScreen characterScreen => CharacterTweenField?.GetValue(characterScreen),
                NUnlockEpochScreen epochScreen => EpochTweenField?.GetValue(epochScreen),
                NUnlockMiscScreen miscScreen => MiscTweenField?.GetValue(miscScreen),
                _ => null,
            };

            TrackTween(screen, tween);
        }

        public static void TrackBannerTween(NCommonBanner banner)
        {
            if (IsTimelineBanner(banner))
                TrackTween(banner, BannerTweenField?.GetValue(banner));
        }

        public static void TrackBannerLabelTween(NCommonBanner banner)
        {
            if (IsTimelineBanner(banner))
                TrackTween(banner, BannerLabelTweenField?.GetValue(banner));
        }

        private static bool IsTimelineBanner(Node node)
        {
            var current = node.GetParent();
            while (current != null)
            {
                if (current is NUnlockScreen or NTimelineScreen)
                    return true;

                current = current.GetParent();
            }

            return false;
        }

        private static void TrackTween(Node owner, object? value)
        {
            if (!SpeedManager.AreGlobalBehaviorPatchesEnabled || value is not Tween tween)
                return;

            TaskHelper.RunSafely(TrackTweenAsync(owner, tween));
        }

        private static async Task TrackTweenAsync(Node owner, Tween tween)
        {
            using var scope = SpeedManager.BeginScope(SafeSpeedReason.TimelineAnimation);
            await tween.AwaitFinished(owner);
        }
    }

    public class TimelineFirstTimeSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_first_time";
        public static string Description => "Accelerate first-time timeline slot animation";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTimelineScreen), nameof(NTimelineScreen.SpawnFirstTimeTimeline), Type.EmptyTypes)];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.TimelineAnimation);
        }
    }

    public class TimelineTutorialIntroSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_tutorial_intro";
        public static string Description => "Accelerate first timeline tutorial text and acknowledge button intro";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTimelineTutorial), "AnimateTutorial", Type.EmptyTypes)];
        }

        public static void Postfix(NTimelineTutorial __instance)
        {
            TimelineTutorialAnimationTracker.TrackTween(__instance);
        }
    }

    public class TimelineTutorialCloseSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_tutorial_close";
        public static string Description => "Accelerate first timeline tutorial close animation";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NTimelineTutorial), "CloseTutorial", [typeof(NButton)])];
        }

        public static void Postfix(NTimelineTutorial __instance)
        {
            TimelineTutorialAnimationTracker.TrackTween(__instance);
        }
    }

    public class TimelineUnlockInfoAnimInSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_info_anim_in";
        public static string Description => "Accelerate timeline epoch unlock info text intro";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NUnlockInfo), nameof(NUnlockInfo.AnimIn), [typeof(string)])];
        }

        public static void Postfix(NUnlockInfo __instance)
        {
            TimelineUnlockInfoAnimationTracker.TrackTween(__instance);
        }
    }

    public class TimelineUnlockInfoPaginatorSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_info_paginator";
        public static string Description => "Accelerate timeline epoch unlock info paginator text intro";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NUnlockInfo), nameof(NUnlockInfo.AnimInViaPaginator), [typeof(string)])];
        }

        public static void Postfix(NUnlockInfo __instance, ref Task __result)
        {
            __result = TimelineUnlockInfoAnimationTracker.TrackAfterAsync(__result, __instance);
        }
    }

    public class TimelineUnlockScreenBaseOpenSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_screen_base_open";
        public static string Description => "Accelerate timeline unlock screen base fade-in";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NUnlockScreen), nameof(NUnlockScreen.Open), Type.EmptyTypes)];
        }

        public static void Postfix(NUnlockScreen __instance)
        {
            TimelineUnlockScreenAnimationTracker.TrackBaseTween(__instance);
        }
    }

    public class TimelineUnlockCardsScreenOpenSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_cards_screen_open";
        public static string Description => "Accelerate timeline card unlock screen animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NUnlockCardsScreen), nameof(NUnlockCardsScreen.Open), Type.EmptyTypes)];
        }

        public static void Postfix(NUnlockCardsScreen __instance)
        {
            TimelineUnlockScreenAnimationTracker.TrackOpenTweens(__instance);
        }
    }

    public class TimelineUnlockRelicsScreenOpenSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_relics_screen_open";
        public static string Description => "Accelerate timeline relic unlock screen animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NUnlockRelicsScreen), nameof(NUnlockRelicsScreen.Open), Type.EmptyTypes)];
        }

        public static void Postfix(NUnlockRelicsScreen __instance)
        {
            TimelineUnlockScreenAnimationTracker.TrackOpenTweens(__instance);
        }
    }

    public class TimelineUnlockPotionsScreenOpenSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_potions_screen_open";
        public static string Description => "Accelerate timeline potion unlock screen animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NUnlockPotionsScreen), nameof(NUnlockPotionsScreen.Open), Type.EmptyTypes)];
        }

        public static void Postfix(NUnlockPotionsScreen __instance)
        {
            TimelineUnlockScreenAnimationTracker.TrackOpenTweens(__instance);
        }
    }

    public class TimelineUnlockCharacterScreenOpenSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_character_screen_open";
        public static string Description => "Accelerate timeline character unlock screen text animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NUnlockCharacterScreen), nameof(NUnlockCharacterScreen.Open), Type.EmptyTypes)];
        }

        public static void Postfix(NUnlockCharacterScreen __instance)
        {
            TimelineUnlockScreenAnimationTracker.TrackOpenTweens(__instance);
        }
    }

    public class TimelineUnlockEpochScreenOpenSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_epoch_screen_open";
        public static string Description => "Accelerate timeline epoch unlock screen text and card animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NUnlockEpochScreen), nameof(NUnlockEpochScreen.Open), Type.EmptyTypes)];
        }

        public static void Postfix(NUnlockEpochScreen __instance)
        {
            TimelineUnlockScreenAnimationTracker.TrackOpenTweens(__instance);
        }
    }

    public class TimelineUnlockMiscScreenOpenSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_misc_screen_open";
        public static string Description => "Accelerate timeline miscellaneous unlock text animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NUnlockMiscScreen), nameof(NUnlockMiscScreen.Open), Type.EmptyTypes)];
        }

        public static void Postfix(NUnlockMiscScreen __instance)
        {
            TimelineUnlockScreenAnimationTracker.TrackOpenTweens(__instance);
        }
    }

    public class TimelineCommonBannerAnimateInSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_common_banner_anim_in";
        public static string Description => "Accelerate timeline unlock screen banner intro animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCommonBanner), nameof(NCommonBanner.AnimateIn), Type.EmptyTypes)];
        }

        public static void Postfix(NCommonBanner __instance)
        {
            TimelineUnlockScreenAnimationTracker.TrackBannerTween(__instance);
        }
    }

    public class TimelineCommonBannerChangeTextSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_common_banner_change_text";
        public static string Description => "Accelerate timeline banner text change animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCommonBanner), nameof(NCommonBanner.ChangeText), [typeof(string)])];
        }

        public static void Postfix(NCommonBanner __instance)
        {
            TimelineUnlockScreenAnimationTracker.TrackBannerLabelTween(__instance);
        }
    }

    public class TimelineAddEpochSlotsSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_add_epoch_slots";
        public static string Description => "Accelerate animated timeline slot expansion";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NTimelineScreen), nameof(NTimelineScreen.AddEpochSlots),
                    [typeof(List<EpochSlotData>), typeof(bool)]),
            ];
        }

        public static void Postfix(bool isAnimated, ref Task __result)
        {
            if (isAnimated)
                __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.TimelineAnimation);
        }
    }

    public class TimelineAutoPanSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_auto_pan";
        public static string Description => "Accelerate timeline automatic panning";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NSlotsContainer), nameof(NSlotsContainer.LerpToSlot), [typeof(float)])];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.TimelineAnimation);
        }
    }

    public class TimelineUnlockAnimationSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_unlock_animation";
        public static string Description => "Accelerate timeline epoch reveal animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
                [new(typeof(NEpochInspectScreen), nameof(NEpochInspectScreen.UnlockAnimation), [typeof(EpochModel)])];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.TimelineAnimation);
        }
    }

    public class TimelineEpochSlotSpawnSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_epoch_slot_spawn";
        public static string Description => "Accelerate individual timeline epoch slot spawn animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NEpochSlot), nameof(NEpochSlot.SpawnSlot), Type.EmptyTypes)];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.TimelineAnimation);
        }
    }

    public class TimelineEraIconSpawnSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_era_icon_spawn";
        public static string Description => "Accelerate timeline era icon spawn animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NEraColumn), nameof(NEraColumn.SpawnIcon), Type.EmptyTypes)];
        }

        public static bool Prefix(NEraColumn __instance)
        {
            return !TimelineEraColumnAnimationTracker.TrySpawnIcon(__instance);
        }
    }

    public class TimelineEraLabelSpawnSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_era_label_spawn";
        public static string Description => "Accelerate timeline era label spawn animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NEraColumn), nameof(NEraColumn.SpawnNameAndYear), Type.EmptyTypes)];
        }

        public static bool Prefix(NEraColumn __instance, ref Task __result)
        {
            return !TimelineEraColumnAnimationTracker.TrySpawnNameAndYear(__instance, ref __result);
        }
    }

    public class TimelineEraColumnMoveSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_timeline_era_column_move";
        public static string Description => "Accelerate timeline era column move animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NEraColumn), nameof(NEraColumn.SaveBeforeAnimationPosition), Type.EmptyTypes)];
        }

        public static bool Prefix(NEraColumn __instance, ref Task __result)
        {
            return !TimelineEraColumnAnimationTracker.TrySaveBeforeAnimationPosition(__instance, ref __result);
        }
    }

    public class TimelineSlotsContainerProcessStabilizationPatch : IPatchMethod
    {
        public static string PatchId => "timeline_slots_container_process_stabilization";
        public static string Description => "Clamp timeline pan interpolation and bounds recovery";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NSlotsContainer), nameof(NSlotsContainer._Process), [typeof(double)])];
        }

        public static bool Prefix(NSlotsContainer __instance, double delta)
        {
            return !TimelineSlotsContainerStabilizer.TryProcess(__instance, delta);
        }
    }
}
