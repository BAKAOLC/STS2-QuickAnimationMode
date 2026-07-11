using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    public class GameActionLocalPlayerChoiceSpeedGuardPatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_game_action_local_player_choice_guard";
        public static string Description => "Pause safe acceleration only for local game-action player choices";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(GameActionPlayerChoiceContext),
                    nameof(GameActionPlayerChoiceContext.SignalPlayerChoiceBegun),
                    ResolveSignalPlayerChoiceBegunParameters(typeof(GameActionPlayerChoiceContext))),
                new(typeof(GameActionPlayerChoiceContext),
                    nameof(GameActionPlayerChoiceContext.SignalPlayerChoiceEnded),
                    Type.EmptyTypes),
            ];
        }

        private static Type[] ResolveSignalPlayerChoiceBegunParameters(Type contextType)
        {
            Type[] currentParameters = [typeof(Player), typeof(PlayerChoiceOptions)];
            return AccessTools.DeclaredMethod(
                contextType,
                nameof(GameActionPlayerChoiceContext.SignalPlayerChoiceBegun),
                currentParameters) != null
                ? currentParameters
                : [typeof(PlayerChoiceOptions)];
        }

        public static void Prefix(GameActionPlayerChoiceContext __instance, MethodBase __originalMethod)
        {
            if (__instance.Action.OwnerId != LocalContext.NetId)
                return;

            if (__originalMethod.Name == nameof(GameActionPlayerChoiceContext.SignalPlayerChoiceBegun))
                SpeedManager.BeginLocalPlayerChoice();
            else
                SpeedManager.EndLocalPlayerChoice();
        }
    }

    public class HookLocalPlayerChoiceSpeedGuardPatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_hook_local_player_choice_guard";
        public static string Description => "Pause safe acceleration only for local hook-driven player choices";

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(HookPlayerChoiceContext), nameof(HookPlayerChoiceContext.SignalPlayerChoiceBegun),
                    ResolveSignalPlayerChoiceBegunParameters()),
                new(typeof(HookPlayerChoiceContext), nameof(HookPlayerChoiceContext.SignalPlayerChoiceEnded),
                    Type.EmptyTypes),
            ];
        }

        private static Type[] ResolveSignalPlayerChoiceBegunParameters()
        {
            Type[] currentParameters = [typeof(Player), typeof(PlayerChoiceOptions)];
            return AccessTools.DeclaredMethod(
                typeof(HookPlayerChoiceContext),
                nameof(HookPlayerChoiceContext.SignalPlayerChoiceBegun),
                currentParameters) != null
                ? currentParameters
                : [typeof(PlayerChoiceOptions)];
        }

        public static void Prefix(HookPlayerChoiceContext __instance, MethodBase __originalMethod)
        {
            if (!LocalContext.IsMe(__instance.Owner))
                return;

            if (__originalMethod.Name == nameof(HookPlayerChoiceContext.SignalPlayerChoiceBegun))
                SpeedManager.BeginLocalPlayerChoice();
            else
                SpeedManager.EndLocalPlayerChoice();
        }
    }
}
