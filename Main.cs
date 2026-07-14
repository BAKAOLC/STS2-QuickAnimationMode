using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using STS2QuickAnimationMode.Data;
using STS2QuickAnimationMode.Patches;
using STS2QuickAnimationMode.Settings;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Utils;

namespace STS2QuickAnimationMode;

[ModInitializer("Initialize")]
public static class Main
{
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(Const.ModId);
    public static I18N I18N { get; private set; } = null!;

    public static bool IsModActive { get; private set; }

    public static void Initialize()
    {
        Logger.Info($"Mod ID: {Const.ModId}");
        Logger.Info($"Version: {Const.Version}");
        Logger.Info("Initializing mod...");

        try
        {
            var patcher = RitsuLibFramework.CreatePatcher(Const.ModId, "main");
            RegisterMainPatches(patcher);

            if (!RitsuLibFramework.ApplyRequiredPatcher(patcher, () => IsModActive = false))
            {
                Logger.Error("Mod initialization failed: Critical patch(es) failed to apply");
                return;
            }

            IsModActive = true;

            I18N = RitsuLibFramework.CreateLocalization(
                "SpeedControl.I18N",
                resourceFolders: ["STS2QuickAnimationMode.localization"]
            );

            ModDataStore.Initialize();
            SpeedControlSettingsPage.Register();
            SpeedManager.Initialize();

            Logger.Info("Mod initialization complete - Mod is now ACTIVE");
        }
        catch (Exception ex)
        {
            Logger.Error($"Mod initialization failed with exception: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
            IsModActive = false;
        }
    }

    private static void RegisterMainPatches(ModPatcher patcher)
    {
        patcher.RegisterPatch<SettingsScreenPatch>();
        patcher.RegisterPatch<HitStopPatch>();
        patcher.RegisterPatch<SpeedProcessPumpInstallPatch>();
        patcher.RegisterPatch<HandAddStateRepairPatch>();
        patcher.RegisterPatch<ReturnHolderToHandStateRepairPatch>();
        patcher.RegisterPatch<CardPlayCleanupStateRepairPatch>();
        patcher.RegisterPatch<CardPileAddSingleStateRepairPatch>();
        patcher.RegisterPatch<CardPileAddManyStateRepairPatch>();
        patcher.RegisterPatch<CardTransformStateRepairGuardPatch>();
        patcher.RegisterPatch<HandTargetPositionStabilizationPatch>();
        patcher.RegisterPatch<HandTargetAngleStabilizationPatch>();
        patcher.RegisterPatch<HandTargetScaleStabilizationPatch>();
        patcher.RegisterPatch<CardPileDrawSingleSpeedScopePatch>();
        patcher.RegisterPatch<CardPileDrawManySpeedScopePatch>();
        patcher.RegisterPatch<CardPileShuffleSpeedScopePatch>();
        patcher.RegisterPatch<CardPlayResolutionSpeedScopePatch>();
        patcher.RegisterPatch<CardDiscardSingleSpeedScopePatch>();
        patcher.RegisterPatch<CardDiscardManySpeedScopePatch>();
        patcher.RegisterPatch<CardExhaustSpeedScopePatch>();
        patcher.RegisterPatch<CombatTurnTransitionSpeedScopePatch>();
        patcher.RegisterPatch<EnemyActionSpeedScopePatch>();
        patcher.RegisterPatch<TimelineFirstTimeSpeedScopePatch>();
        patcher.RegisterPatch<TimelineTutorialIntroSpeedScopePatch>();
        patcher.RegisterPatch<TimelineTutorialCloseSpeedScopePatch>();
        patcher.RegisterPatch<TimelineUnlockInfoAnimInSpeedScopePatch>();
        patcher.RegisterPatch<TimelineUnlockInfoPaginatorSpeedScopePatch>();
        patcher.RegisterPatch<TimelineUnlockScreenBaseOpenSpeedScopePatch>();
        patcher.RegisterPatch<TimelineUnlockCardsScreenOpenSpeedScopePatch>();
        patcher.RegisterPatch<TimelineUnlockRelicsScreenOpenSpeedScopePatch>();
        patcher.RegisterPatch<TimelineUnlockPotionsScreenOpenSpeedScopePatch>();
        patcher.RegisterPatch<TimelineUnlockCharacterScreenOpenSpeedScopePatch>();
        patcher.RegisterPatch<TimelineUnlockEpochScreenOpenSpeedScopePatch>();
        patcher.RegisterPatch<TimelineUnlockMiscScreenOpenSpeedScopePatch>();
        patcher.RegisterPatch<TimelineCommonBannerAnimateInSpeedScopePatch>();
        patcher.RegisterPatch<TimelineCommonBannerChangeTextSpeedScopePatch>();
        patcher.RegisterPatch<TimelineAddEpochSlotsSpeedScopePatch>();
        patcher.RegisterPatch<TimelineAutoPanSpeedScopePatch>();
        patcher.RegisterPatch<TimelineUnlockAnimationSpeedScopePatch>();
        patcher.RegisterPatch<TimelineEpochSlotSpawnSpeedScopePatch>();
        patcher.RegisterPatch<TimelineEraIconSpawnSpeedScopePatch>();
        patcher.RegisterPatch<TimelineEraLabelSpawnSpeedScopePatch>();
        patcher.RegisterPatch<TimelineEraColumnMoveSpeedScopePatch>();
        patcher.RegisterPatch<TimelineSlotsContainerProcessStabilizationPatch>();
        patcher.RegisterPatch<GameOverScreenIntroSpeedScopePatch>();
        patcher.RegisterPatch<GameOverQuoteSpeedScopePatch>();
        patcher.RegisterPatch<GameOverRunSummarySpeedScopePatch>();
        patcher.RegisterPatch<GameOverScoreLineSpeedScopePatch>();
        patcher.RegisterPatch<GameOverBadgeSpeedScopePatch>();
        patcher.RegisterPatch<GameOverDiscoverySpeedScopePatch>();
        patcher.RegisterPatch<GameOverScoreTweenSpeedScopePatch>();
        patcher.RegisterPatch<RunLoadingSpeedScopePatch>();
        patcher.RegisterPatch<TransitionSpeedScopePatch>();
        patcher.RegisterPatch<GameActionLocalPlayerChoiceSpeedGuardPatch>();
        patcher.RegisterPatch<HookLocalPlayerChoiceSpeedGuardPatch>();
        patcher.RegisterPatch<RunCleanupPatch>();
    }
}