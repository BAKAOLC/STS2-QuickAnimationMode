using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    public class CombatPresentationAnimationScopePatch : IPatchMethod
    {
        public static string PatchId => "combat_presentation_animation_scope";
        public static string Description => "Scale explicitly scoped combat presentation animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCombatStartBanner), "AnimateVfx", Type.EmptyTypes, true),
                new(typeof(NPlayerTurnBanner), "Display", Type.EmptyTypes, true),
                new(typeof(NEnemyTurnBanner), "Display", Type.EmptyTypes, true),
                new(typeof(NDamageNumVfx), "AnimVfx", Type.EmptyTypes, true),
                new(typeof(NHealNumVfx), "AnimVfx", Type.EmptyTypes, true),
                new(typeof(NDamageBlockedVfx), "BlockAnim", Type.EmptyTypes, true),
            ];
        }

        public static void Prefix(out (int AnimationDepth, int CustomWaitBudget) __state)
        {
            __state = CombatPresentationSpeed.Enter(false);
        }

        public static void Postfix(ref Task __result, (int AnimationDepth, int CustomWaitBudget) __state)
        {
            CombatPresentationSpeed.Restore(__state);
        }

        public static Exception? Finalizer(
            Exception? __exception,
            (int AnimationDepth, int CustomWaitBudget) __state)
        {
            CombatPresentationSpeed.Restore(__state);
            return __exception;
        }
    }

    public class EnemyIntentPresentationScopePatch : IPatchMethod
    {
        public static string PatchId => "combat_presentation_enemy_intent_scope";
        public static string Description => "Scale enemy intent presentation and its explicit pre-action wait";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NCreature), nameof(NCreature.PerformIntent), Type.EmptyTypes)];
        }

        public static void Prefix(out (int AnimationDepth, int CustomWaitBudget) __state)
        {
            __state = CombatPresentationSpeed.Enter(true);
        }

        public static void Postfix(ref Task __result, (int AnimationDepth, int CustomWaitBudget) __state)
        {
            CombatPresentationSpeed.Restore(__state);
        }

        public static Exception? Finalizer(
            Exception? __exception,
            (int AnimationDepth, int CustomWaitBudget) __state)
        {
            CombatPresentationSpeed.Restore(__state);
            return __exception;
        }
    }

    public class CombatPresentationWaitMarkerPatch : IPatchMethod
    {
        public static string PatchId => "combat_presentation_wait_marker";
        public static string Description => "Mark the presentation wait paired with a battle or turn banner";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NCombatStartBanner), nameof(NCombatStartBanner.Create), Type.EmptyTypes),
                new(typeof(NPlayerTurnBanner), nameof(NPlayerTurnBanner.Create), [typeof(int)]),
                new(typeof(NEnemyTurnBanner), nameof(NEnemyTurnBanner.Create), Type.EmptyTypes),
            ];
        }

        public static void Postfix()
        {
            CombatPresentationSpeed.MarkNextCustomWait();
        }
    }

    public class CombatPresentationTweenDelayPatch : IPatchMethod
    {
        public static string PatchId => "combat_presentation_tween_delay";
        public static string Description => "Scale delays belonging to scoped combat presentation tweens";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(PropertyTweener), nameof(PropertyTweener.SetDelay), [typeof(double)])];
        }

        public static void Prefix(ref double delay)
        {
            delay = CombatPresentationSpeed.ScaleDuration(delay);
        }
    }

    public class CombatPresentationWaitPatch : IPatchMethod
    {
        public static string PatchId => "combat_presentation_wait";
        public static string Description => "Scale one explicitly scoped combat presentation wait";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(Cmd), nameof(Cmd.CustomScaledWait),
                    [typeof(float), typeof(float), typeof(bool), typeof(CancellationToken)]),
            ];
        }

        public static void Prefix(ref float fastSeconds, ref float standardSeconds)
        {
            CombatPresentationSpeed.ScaleCustomWait(ref fastSeconds, ref standardSeconds);
        }
    }

    public class DamageNumberPresentationProcessPatch : IPatchMethod
    {
        public static string PatchId => "combat_presentation_damage_number_motion";
        public static string Description => "Scale damage number motion without changing global time";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NDamageNumVfx), nameof(NDamageNumVfx._Process), [typeof(double)])];
        }

        public static void Prefix(ref double delta)
        {
            delta = CombatPresentationSpeed.ScaleProcessDelta(delta);
        }
    }

    public class HealNumberPresentationProcessPatch : IPatchMethod
    {
        public static string PatchId => "combat_presentation_heal_number_motion";
        public static string Description => "Scale healing number motion without changing global time";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NHealNumVfx), nameof(NHealNumVfx._Process), [typeof(double)])];
        }

        public static void Prefix(ref double delta)
        {
            delta = CombatPresentationSpeed.ScaleProcessDelta(delta);
        }
    }
}
