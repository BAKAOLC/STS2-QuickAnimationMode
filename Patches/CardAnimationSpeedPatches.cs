using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    public class CardTweenDurationPatch : IPatchMethod
    {
        public static string PatchId => "card_animation_tween_duration";
        public static string Description => "Scale card-only tween durations without changing global game time";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Tween), nameof(Tween.TweenProperty),
                    [typeof(GodotObject), typeof(NodePath), typeof(Variant), typeof(double)]),
            ];
        }

        public static void Prefix(Tween __instance, GodotObject @object, ref double duration)
        {
            if (@object is NCard or NCardFlyShuffleVfx)
                CardAnimationSpeed.MarkCardTween(__instance);

            if (CardAnimationSpeed.IsCardTween(__instance))
                duration = CardAnimationSpeed.ScaleDuration(duration);

            duration = CombatPresentationSpeed.ScaleDuration(duration);
        }
    }

    public class CardTweenIntervalPatch : IPatchMethod
    {
        public static string PatchId => "card_animation_tween_interval";
        public static string Description => "Scale intervals belonging to confirmed card animation tweens";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(Tween), nameof(Tween.TweenInterval), [typeof(double)])];
        }

        public static void Prefix(Tween __instance, ref double time)
        {
            if (CardAnimationSpeed.IsCardTween(__instance))
                time = CardAnimationSpeed.ScaleDuration(time);

            time = CombatPresentationSpeed.ScaleDuration(time);
        }
    }

    public class CardFlyVfxSpeedPatch : IPatchMethod
    {
        private static readonly FieldInfo? SpeedField = AccessTools.Field(typeof(NCardFlyVfx), "_speed");
        private static readonly FieldInfo? AccelerationField = AccessTools.Field(typeof(NCardFlyVfx), "_accel");

        public static string PatchId => "card_animation_fly_arc";
        public static string Description => "Scale card fly arc animation progression";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardFlyVfx), nameof(NCardFlyVfx._Ready), Type.EmptyTypes)];
        }

        public static void Postfix(NCardFlyVfx __instance)
        {
            ScaleKinematics(__instance, SpeedField, AccelerationField);
        }

        internal static void ScaleKinematics(object instance, FieldInfo? speedField, FieldInfo? accelerationField)
        {
            if (!SpeedManager.IsCardAnimationAccelerationEnabled
                || speedField?.GetValue(instance) is not float speed
                || accelerationField?.GetValue(instance) is not float acceleration)
                return;

            CardAnimationSpeed.ScaleKinematics(ref speed, ref acceleration);
            speedField.SetValue(instance, speed);
            accelerationField.SetValue(instance, acceleration);
        }
    }

    public class CardFlyShuffleVfxSpeedPatch : IPatchMethod
    {
        private static readonly FieldInfo? SpeedField = AccessTools.Field(typeof(NCardFlyShuffleVfx), "_speed");

        private static readonly FieldInfo? AccelerationField =
            AccessTools.Field(typeof(NCardFlyShuffleVfx), "_accel");

        public static string PatchId => "card_animation_shuffle_arc";
        public static string Description => "Scale shuffle card arc animation progression";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardFlyShuffleVfx), nameof(NCardFlyShuffleVfx._Ready), Type.EmptyTypes)];
        }

        public static void Postfix(NCardFlyShuffleVfx __instance)
        {
            CardFlyVfxSpeedPatch.ScaleKinematics(__instance, SpeedField, AccelerationField);
        }
    }

    public class CardFlyPowerVfxSpeedPatch : IPatchMethod
    {
        public static string PatchId => "card_animation_power_arc";
        public static string Description => "Scale power card fly animation duration";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCardFlyPowerVfx), "GetDurationInternal", Type.EmptyTypes, true)];
        }

        public static void Postfix(ref float __result)
        {
            __result = CardAnimationSpeed.ScaleDuration(__result);
        }
    }

    public class CardExhaustQuickVfxSpeedPatch : IPatchMethod
    {
        private const string TargetTypeName =
            "MegaCrit.Sts2.Core.Nodes.Vfx.Cards.NCardExhaustQuickVfx";

        private static readonly Type? TargetType = AccessTools.TypeByName(TargetTypeName);

        private static readonly FieldInfo? DurationField =
            TargetType == null ? null : AccessTools.Field(TargetType, "_anticipationDuration");

        public static string PatchId => "card_animation_exhaust_quick";
        public static string Description => "Scale quick card exhaust animation duration";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return TargetType == null ? [] : [new(TargetType, "Create", [typeof(NCard)])];
        }

        public static void Postfix(object? __result)
        {
            ScaleDuration(__result, DurationField);
        }

        internal static void ScaleDuration(object? instance, FieldInfo? durationField)
        {
            if (!SpeedManager.IsCardAnimationAccelerationEnabled
                || instance == null
                || durationField?.GetValue(instance) is not float duration)
                return;

            durationField.SetValue(instance, CardAnimationSpeed.ScaleDuration(duration));
        }
    }

    public class CardExhaustVfxSpeedPatch : IPatchMethod
    {
        private const string TargetTypeName =
            "MegaCrit.Sts2.Core.Nodes.Vfx.Cards.NCardExhaustVfx";

        private static readonly Type? TargetType = AccessTools.TypeByName(TargetTypeName);

        private static readonly FieldInfo? DurationField =
            TargetType == null ? null : AccessTools.Field(TargetType, "_exhaustDuration");

        public static string PatchId => "card_animation_exhaust";
        public static string Description => "Scale card exhaust animation duration";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return TargetType == null ? [] : [new(TargetType, "Create", [typeof(NCard)])];
        }

        public static void Postfix(object? __result)
        {
            CardExhaustQuickVfxSpeedPatch.ScaleDuration(__result, DurationField);
        }
    }
}
