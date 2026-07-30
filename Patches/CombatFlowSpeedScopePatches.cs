using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    public class CombatTurnTransitionSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_combat_turn_transitions";
        public static string Description => "Accelerate safe combat turn transition sequences";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            var targets = new List<ModPatchTarget>();
            var combatTurnStateType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Combat.CombatTurnState");
            var endTurnSignalType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Combat.EndTurnSignal");
            AddExistingTarget(targets, "StartTurn",
                WithCombatTurnState(combatTurnStateType, [typeof(Func<Task>)]),
                [typeof(Func<Task>)]);
            AddExistingTarget(targets, "AfterAllPlayersReadyToEndTurn",
                WithCombatTurnStateAndTrailingType(combatTurnStateType, endTurnSignalType),
                [typeof(CombatState), typeof(int), typeof(Player), typeof(Func<Task>)],
                [typeof(Func<Task>)]);
            AddExistingTarget(targets, nameof(CombatManager.EndPlayerTurnPhaseOneInternal),
                WithCombatTurnState(combatTurnStateType),
                Type.EmptyTypes);
            AddExistingTarget(targets, "AfterAllPlayersReadyToBeginEnemyTurn",
                WithCombatTurnState(combatTurnStateType),
                [typeof(Func<Task>)]);
            AddExistingTarget(targets, nameof(CombatManager.EndPlayerTurnPhaseTwoInternal),
                WithCombatTurnState(combatTurnStateType),
                [typeof(CancellationToken?)],
                Type.EmptyTypes);
            AddExistingTarget(targets, nameof(CombatManager.SwitchFromPlayerToEnemySide),
                WithCombatTurnState(combatTurnStateType),
                [typeof(Func<Task>)],
                Type.EmptyTypes);
            AddExistingTarget(targets, "EndEnemyTurn",
                WithCombatTurnState(combatTurnStateType),
                [typeof(CancellationToken?)],
                Type.EmptyTypes);
            AddExistingTarget(targets, "EndEnemyTurnInternal", Type.EmptyTypes);
            return [.. targets];
        }

        private static Type[]? WithCombatTurnState(Type? combatTurnStateType, params Type[] trailingTypes)
        {
            return combatTurnStateType == null
                ? null
                : [combatTurnStateType, .. trailingTypes];
        }

        private static Type[]? WithCombatTurnStateAndTrailingType(
            Type? combatTurnStateType,
            Type? trailingType)
        {
            return combatTurnStateType == null || trailingType == null
                ? null
                : [combatTurnStateType, trailingType];
        }

        internal static void AddExistingTarget(
            ICollection<ModPatchTarget> targets,
            string methodName,
            params Type[]?[] signatures)
        {
            foreach (var signature in signatures)
            {
                if (signature == null)
                    continue;

                if (AccessTools.DeclaredMethod(typeof(CombatManager), methodName, signature) == null)
                    continue;

                targets.Add(new(typeof(CombatManager), methodName, signature, true));
                return;
            }
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.TurnTransition);
        }
    }

    public class EnemyActionSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_enemy_actions";
        public static string Description => "Accelerate safe enemy turn action sequences";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            var targets = new List<ModPatchTarget>();
            var combatTurnStateType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Combat.CombatTurnState");
            CombatTurnTransitionSpeedScopePatch.AddExistingTarget(targets, "ExecuteEnemyTurn",
                combatTurnStateType == null ? null : [combatTurnStateType, typeof(Func<Task>)],
                [typeof(Func<Task>)]);
            return [.. targets];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.EnemyAction);
        }
    }
}
