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
            return
            [
                new(typeof(CombatManager), "StartTurn", [typeof(Func<Task>)], true),
                new(typeof(CombatManager), "AfterAllPlayersReadyToEndTurn",
                    ResolveParameterTypes(
                        "AfterAllPlayersReadyToEndTurn",
                        [typeof(CombatState), typeof(int), typeof(Player), typeof(Func<Task>)],
                        [typeof(Func<Task>)]),
                    true),
                new(typeof(CombatManager), nameof(CombatManager.EndPlayerTurnPhaseOneInternal),
                    Type.EmptyTypes),
                new(typeof(CombatManager), "AfterAllPlayersReadyToBeginEnemyTurn", [typeof(Func<Task>)],
                    true),
                new(typeof(CombatManager), nameof(CombatManager.EndPlayerTurnPhaseTwoInternal),
                    ResolveParameterTypes(
                        nameof(CombatManager.EndPlayerTurnPhaseTwoInternal),
                        [typeof(CancellationToken?)],
                        Type.EmptyTypes)),
                new(typeof(CombatManager), nameof(CombatManager.SwitchFromPlayerToEnemySide),
                    [typeof(Func<Task>)]),
                new(typeof(CombatManager), "EndEnemyTurn",
                    ResolveParameterTypes("EndEnemyTurn", [typeof(CancellationToken?)], Type.EmptyTypes), true),
                new(typeof(CombatManager), "EndEnemyTurnInternal", Type.EmptyTypes, true),
            ];
        }

        private static Type[] ResolveParameterTypes(string methodName, params Type[][] candidates)
        {
            foreach (var candidate in candidates)
                if (AccessTools.DeclaredMethod(typeof(CombatManager), methodName, candidate) != null)
                    return candidate;

            return candidates[0];
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
            return [new(typeof(CombatManager), "ExecuteEnemyTurn", [typeof(Func<Task>)], true)];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.EnemyAction);
        }
    }
}
