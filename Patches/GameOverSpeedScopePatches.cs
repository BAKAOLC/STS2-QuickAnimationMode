using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    internal static class GameOverScoreTweenTracker
    {
        private static IDisposable? _scope;
        private static int _generation;

        public static void MarkActive()
        {
            _scope ??= SpeedManager.BeginScope(SafeSpeedReason.GameOverSummary);
            var generation = ++_generation;
            TaskHelper.RunSafely(ReleaseWhenIdle(generation));
        }

        private static async Task ReleaseWhenIdle(int generation)
        {
            if (NGame.Instance != null)
            {
                await NGame.Instance.AwaitProcessFrame();
                await NGame.Instance.AwaitProcessFrame();
            }
            else
            {
                await Task.Yield();
            }

            if (generation != _generation)
                return;

            _scope?.Dispose();
            _scope = null;
        }
    }

    public class GameOverScreenIntroSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_game_over_intro";
        public static string Description => "Accelerate game over screen entrance animation";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NGameOverScreen), "AnimateIn", Type.EmptyTypes)];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.GameOverSummary);
        }
    }

    public class GameOverQuoteSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_game_over_quote";
        public static string Description => "Accelerate game over quote animation";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NGameOverScreen), "AnimateInQuote", Type.EmptyTypes)];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.GameOverSummary);
        }
    }

    public class GameOverRunSummarySpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_game_over_run_summary";
        public static string Description => "Accelerate game over score and run summary animation";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NGameOverScreen), "AnimateRunSummary", Type.EmptyTypes)];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.GameOverSummary);
        }
    }

    public class GameOverScoreLineSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_game_over_score_line";
        public static string Description => "Accelerate individual game over score line animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NScoreLine), nameof(NScoreLine.AnimateIn), Type.EmptyTypes)];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.GameOverSummary);
        }
    }

    public class GameOverBadgeSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_game_over_badge";
        public static string Description => "Accelerate game over badge animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NBadge), nameof(NBadge.AnimateIn), Type.EmptyTypes)];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.GameOverSummary);
        }
    }

    public class GameOverDiscoverySpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_game_over_discovery";
        public static string Description => "Accelerate game over discovery summary animations";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return
            [
                new(typeof(NRunSummary), nameof(NRunSummary.AnimateInDiscoveries),
                    [typeof(RunState), typeof(CancellationToken)]),
            ];
        }

        public static void Postfix(ref Task __result)
        {
            __result = SpeedManager.TrackAsync(__result, SafeSpeedReason.GameOverSummary);
        }
    }

    public class GameOverScoreTweenSpeedScopePatch : IPatchMethod
    {
        public static string PatchId => "safe_speed_game_over_score_tween";
        public static string Description => "Keep acceleration active while the game over score value is tweening";
        public static bool IsCritical => false;

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NGameOverScreen), "TweenScore", [typeof(int)])];
        }

        public static void Prefix()
        {
            GameOverScoreTweenTracker.MarkActive();
        }
    }
}
