using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using STS2QuickAnimationMode.Data;

namespace STS2QuickAnimationMode.Utils
{
    public enum SafeSpeedReason
    {
        CardPileSequence,
        CardPlayResolution,
        TurnTransition,
        EnemyAction,
        TimelineAnimation,
        GameOverSummary,
        LoadingScreen,
    }

    public static class SpeedManager
    {
        private const float NormalMultiplier = 1.0f;
        public const float MinSelectableMultiplier = 0.1f;
        public const float MaxSelectableMultiplier = 10.0f;
        public const float SpeedSliderStep = 0.1f;
        public const float MinTimeThreshold = 0.0f;
        public const float MaxTimeThreshold = 10.0f;
        public const float MinTransitionDuration = 0.0f;
        public const float MaxTransitionDuration = 20.0f;
        private const float DefaultTimelineScopeSeconds = 2.0f;
        private const float IdleBufferDuration = 0.1f;
        private const float TimeScaleEpsilon = 0.001f;

        private static readonly Dictionary<SafeSpeedReason, int> ActiveScopes = new();
        private static readonly Dictionary<SafeSpeedReason, double> TimedScopes = new();

        private static float _targetMultiplier = NormalMultiplier;
        private static float _transitionStartMultiplier = NormalMultiplier;
        private static double? _transitionStartTime;
        private static double? _safeStateStartTime;
        private static double? _idleStartTime;
        private static int _localPlayerChoiceDepth;
        private static float _baseTimeScale = NormalMultiplier;
        private static float _lastAppliedTimeScale = NormalMultiplier;
        private static float _lastAppliedMultiplier = NormalMultiplier;
        private static bool _hasAppliedTimeScale;

        private static SpeedSettings Settings => ModDataStore.Get<SpeedSettings>(ModDataStore.SettingsKey);

        public static float CurrentMultiplier => ClampMultiplier(Settings.SpeedMultiplier);
        public static float CardAnimationMultiplier => ClampMultiplier(Settings.CardAnimationMultiplier);
        public static float CardResolutionMultiplier => ClampMultiplier(Settings.CardResolutionMultiplier);
        public static float CombatPresentationMultiplier => ClampMultiplier(Settings.CombatPresentationMultiplier);
        public static float TransitionDuration => Math.Max(0.0f, Settings.TransitionDuration);
        public static float TimeThreshold => Math.Max(0.0f, Settings.TimeThreshold);
        public static float EffectiveMultiplier { get; private set; } = NormalMultiplier;

        public static bool IsAccelerationEnabled =>
            IsGlobalAccelerationEnabled
            || IsCardAnimationAccelerationEnabled
            || IsCardResolutionAccelerationEnabled
            || IsCombatPresentationAccelerationEnabled;

        public static bool IsGlobalAccelerationEnabled =>
            Main.IsModActive
            && Settings.AccelerationMode is SpeedAccelerationMode.SafeState or SpeedAccelerationMode.AlwaysOn;

        public static bool IsCardAnimationAccelerationEnabled =>
            Main.IsModActive && Settings.CardAnimationAccelerationEnabled;

        public static bool IsCardResolutionAccelerationEnabled =>
            Main.IsModActive && Settings.CardResolutionAccelerationEnabled;

        public static bool IsCombatPresentationAccelerationEnabled =>
            Main.IsModActive && Settings.CombatPresentationAccelerationEnabled;

        public static float EffectiveCardAnimationMultiplier =>
            IsCardAnimationAccelerationEnabled ? CardAnimationMultiplier : NormalMultiplier;

        public static float EffectiveCardResolutionMultiplier =>
            IsCardResolutionAccelerationEnabled ? CardResolutionMultiplier : NormalMultiplier;

        public static float EffectiveCombatPresentationMultiplier =>
            IsCombatPresentationAccelerationEnabled ? CombatPresentationMultiplier : NormalMultiplier;

        public static bool AreCardBehaviorPatchesEnabled =>
            IsGlobalAccelerationEnabled
            || IsCardAnimationAccelerationEnabled
            || IsCardResolutionAccelerationEnabled;

        public static bool AreGlobalBehaviorPatchesEnabled => IsGlobalAccelerationEnabled;

        private static double Now => Time.GetTicksMsec() / 1000.0;

        public static void Initialize()
        {
            NormalizeSettings();
            ResetSpeed();
            EnsureProcessPump();
            Main.Logger.Info(
                $"SpeedManager initialized, game mode: {Settings.AccelerationMode}, game: {CurrentMultiplier}x, " +
                $"card animations: {Settings.CardAnimationAccelerationEnabled} ({CardAnimationMultiplier}x), " +
                $"card resolution: {Settings.CardResolutionAccelerationEnabled} ({CardResolutionMultiplier}x), " +
                $"combat presentation: {Settings.CombatPresentationAccelerationEnabled} " +
                $"({CombatPresentationMultiplier}x)");
        }

        public static IDisposable BeginScope(SafeSpeedReason reason)
        {
            if (!ShouldTrackReason(reason))
                return NullScope.Instance;

            ActiveScopes.TryGetValue(reason, out var count);
            ActiveScopes[reason] = count + 1;
            ProcessFrame(0);
            return new SpeedScope(reason);
        }

        public static Task TrackAsync(Task task, SafeSpeedReason reason)
        {
            return ShouldTrackReason(reason) ? TrackAsyncCore(task, reason) : task;
        }

        public static Task<T> TrackAsync<T>(Task<T> task, SafeSpeedReason reason)
        {
            return ShouldTrackReason(reason) ? TrackAsyncCore(task, reason) : task;
        }

        public static void TrackCompletion(Task task, SafeSpeedReason reason)
        {
            if (ShouldTrackReason(reason))
                _ = TrackCompletionCore(task, reason);
        }

        public static void ActivateTimed(SafeSpeedReason reason, float seconds = DefaultTimelineScopeSeconds)
        {
            if (!ShouldTrackReason(reason) || seconds <= 0)
                return;

            var until = Now + seconds;
            if (!TimedScopes.TryGetValue(reason, out var currentUntil) || currentUntil < until)
                TimedScopes[reason] = until;

            ProcessFrame(0);
        }

        public static void ClearReason(SafeSpeedReason reason)
        {
            ActiveScopes.Remove(reason);
            TimedScopes.Remove(reason);
            ProcessFrame(0);
        }

        public static void EnsureProcessPump()
        {
            var game = NGame.Instance;
            if (game == null || game.GetNodeOrNull<SpeedProcessPump>(SpeedProcessPump.NodeName) != null)
                return;

            game.AddChild(new SpeedProcessPump());
        }

        public static void BeginLocalPlayerChoice()
        {
            if (Settings.AccelerationMode != SpeedAccelerationMode.SafeState)
                return;

            _localPlayerChoiceDepth++;
            ForceNormalSpeed();
        }

        public static void EndLocalPlayerChoice()
        {
            if (Settings.AccelerationMode != SpeedAccelerationMode.SafeState)
                return;

            _localPlayerChoiceDepth = Math.Max(0, _localPlayerChoiceDepth - 1);
            ProcessFrame(0);
        }

        public static void ProcessFrame(double delta)
        {
            if (!Main.IsModActive)
            {
                ForceNormalSpeed();
                return;
            }

            CleanupExpiredTimedScopes();

            var mode = Settings.AccelerationMode;
            switch (mode)
            {
                case SpeedAccelerationMode.Off:
                    SetTarget(NormalMultiplier, true);
                    ReleaseSpeedLayerIfNeeded();
                    return;
                case SpeedAccelerationMode.AlwaysOn:
                    SetTarget(CurrentMultiplier, true);
                    break;
                case SpeedAccelerationMode.SafeState:
                    UpdateSafeStateTarget();
                    break;
                default:
                    SetTarget(NormalMultiplier, true);
                    ReleaseSpeedLayerIfNeeded();
                    return;
            }

            ApplySpeedTransition();
        }

        public static void ResetSpeed()
        {
            ActiveScopes.Clear();
            TimedScopes.Clear();
            _localPlayerChoiceDepth = 0;
            EffectiveMultiplier = Settings.AccelerationMode == SpeedAccelerationMode.AlwaysOn
                ? CurrentMultiplier
                : NormalMultiplier;
            _targetMultiplier = EffectiveMultiplier;
            _transitionStartMultiplier = EffectiveMultiplier;
            _transitionStartTime = null;
            _safeStateStartTime = null;
            _idleStartTime = null;
            ApplySpeed();
        }

        public static void OnSettingsChanged()
        {
            NormalizeSettings();
            ResetSpeed();
        }

        public static void ApplySpeed()
        {
            if (!IsGlobalAccelerationEnabled)
            {
                ReleaseSpeedLayerIfNeeded();
                return;
            }

            ObserveExternalTimeScale();
            var multiplier = Mathf.Clamp(EffectiveMultiplier, MinSelectableMultiplier, MaxSelectableMultiplier);
            WriteTimeScale(_baseTimeScale * multiplier, multiplier);
        }

        public static void ScaleExternalTimeScale(ref float timeScale)
        {
            if (!IsGlobalAccelerationEnabled)
                return;

            _baseTimeScale = NormalizeBaseTimeScale(timeScale);
            var multiplier = Mathf.Clamp(EffectiveMultiplier, MinSelectableMultiplier, MaxSelectableMultiplier);
            timeScale = _baseTimeScale * multiplier;
            _lastAppliedMultiplier = multiplier;
            _lastAppliedTimeScale = timeScale;
            _hasAppliedTimeScale = true;
        }

        private static async Task TrackAsyncCore(Task task, SafeSpeedReason reason)
        {
            using var scope = BeginScope(reason);
            try
            {
                await task;
            }
            finally
            {
                RequestHandRepairForReason(reason);
            }
        }

        private static async Task<T> TrackAsyncCore<T>(Task<T> task, SafeSpeedReason reason)
        {
            using var scope = BeginScope(reason);
            try
            {
                return await task;
            }
            finally
            {
                RequestHandRepairForReason(reason);
            }
        }

        private static async Task TrackCompletionCore(Task task, SafeSpeedReason reason)
        {
            using var scope = BeginScope(reason);
            try
            {
                await task;
            }
            catch
            {
                // The original caller remains responsible for observing the original task failure.
            }
            finally
            {
                RequestHandRepairForReason(reason);
            }
        }

        private static void UpdateSafeStateTarget()
        {
            if (IsUserInteractionBlockingAcceleration())
            {
                ForceNormalSpeed();
                return;
            }

            if (!HasAllowedSafeScope())
            {
                _idleStartTime ??= Now;
                if (Now - _idleStartTime.Value >= IdleBufferDuration)
                    ForceNormalSpeed();
                return;
            }

            _idleStartTime = null;
            if (!Settings.ProgressiveAccelerationEnabled)
            {
                _safeStateStartTime = null;
                SetTarget(CurrentMultiplier, true);
                return;
            }

            _safeStateStartTime ??= Now;
            SetTarget(Now - _safeStateStartTime.Value >= TimeThreshold ? CurrentMultiplier : NormalMultiplier,
                false);
        }

        private static bool HasAllowedSafeScope()
        {
            return Enum.GetValues<SafeSpeedReason>()
                .Any(reason => IsReasonActive(reason) && IsReasonConfigured(reason));
        }

        private static void RequestHandRepairForReason(SafeSpeedReason reason)
        {
            if (AreCardBehaviorPatchesEnabled
                && reason is SafeSpeedReason.CardPileSequence or SafeSpeedReason.CardPlayResolution)
                HandStateRepair.RequestFullRepair();
        }

        private static bool IsReasonActive(SafeSpeedReason reason)
        {
            return (ActiveScopes.TryGetValue(reason, out var count) && count > 0)
                   || (TimedScopes.TryGetValue(reason, out var until) && until > Now);
        }

        private static bool ShouldTrackReason(SafeSpeedReason reason)
        {
            if (!IsGlobalAccelerationEnabled)
                return false;

            return Settings.AccelerationMode == SpeedAccelerationMode.AlwaysOn || IsReasonConfigured(reason);
        }

        private static bool IsReasonConfigured(SafeSpeedReason reason)
        {
            return reason switch
            {
                SafeSpeedReason.CardPileSequence => Settings.AccelerateCardPileSequences,
                SafeSpeedReason.CardPlayResolution => Settings.AccelerateCardPlayResolution,
                SafeSpeedReason.TurnTransition => Settings.AccelerateTurnTransitions,
                SafeSpeedReason.EnemyAction => Settings.AccelerateEnemyActions,
                SafeSpeedReason.TimelineAnimation => Settings.AccelerateTimelineAnimations,
                SafeSpeedReason.GameOverSummary => Settings.AccelerateGameOverSummary,
                SafeSpeedReason.LoadingScreen => Settings.AccelerateLoadingScreens,
                _ => false,
            };
        }

        private static bool IsUserInteractionBlockingAcceleration()
        {
            if (_localPlayerChoiceDepth > 0)
                return true;

            return NPlayerHand.Instance?.IsInCardSelection == true
                   || NOverlayStack.Instance?.Peek() is ICardSelector
                   || NRun.Instance?.GlobalUi?.TargetManager?.IsInSelection == true;
        }

        private static void SetTarget(float target, bool immediate)
        {
            var lowerBound = Math.Min(NormalMultiplier, CurrentMultiplier);
            var upperBound = Math.Max(NormalMultiplier, CurrentMultiplier);
            target = Mathf.Clamp(target, lowerBound, upperBound);
            if (!Mathf.IsEqualApprox(_targetMultiplier, target, 0.001f))
            {
                _targetMultiplier = target;
                _transitionStartTime = null;
                _transitionStartMultiplier = EffectiveMultiplier;
            }

            if (!immediate)
                return;

            EffectiveMultiplier = target;
            _transitionStartMultiplier = target;
            _transitionStartTime = null;
            if (Mathf.IsEqualApprox(target, NormalMultiplier, 0.001f))
                _safeStateStartTime = null;
        }

        private static void ForceNormalSpeed()
        {
            _safeStateStartTime = null;
            _idleStartTime = null;
            SetTarget(NormalMultiplier, true);
            ApplySpeed();
        }

        private static void ApplySpeedTransition()
        {
            if (!IsGlobalAccelerationEnabled)
            {
                ReleaseSpeedLayerIfNeeded();
                return;
            }

            if (Mathf.IsEqualApprox(EffectiveMultiplier, _targetMultiplier, 0.01f))
            {
                EffectiveMultiplier = _targetMultiplier;
                ApplySpeed();
                return;
            }

            if (TransitionDuration <= 0)
            {
                EffectiveMultiplier = _targetMultiplier;
                _transitionStartTime = null;
                ApplySpeed();
                return;
            }

            _transitionStartTime ??= Now;
            var progress = Mathf.Clamp((float)((Now - _transitionStartTime.Value) / TransitionDuration), 0.0f, 1.0f);
            var smoothProgress = progress < 0.5f
                ? 2f * progress * progress
                : 1f - Mathf.Pow(-2f * progress + 2f, 2f) / 2f;

            EffectiveMultiplier = Mathf.Lerp(_transitionStartMultiplier, _targetMultiplier, smoothProgress);
            EffectiveMultiplier = Mathf.Clamp(EffectiveMultiplier, MinSelectableMultiplier, MaxSelectableMultiplier);
            ApplySpeed();
        }

        private static void ObserveExternalTimeScale()
        {
            if (!_hasAppliedTimeScale)
            {
                _baseTimeScale = NormalizeBaseTimeScale((float)Engine.TimeScale);
                return;
            }

            var current = (float)Engine.TimeScale;
            if (Mathf.IsEqualApprox(current, _lastAppliedTimeScale, TimeScaleEpsilon))
                return;

            var divisor = Math.Max(_lastAppliedMultiplier, TimeScaleEpsilon);
            _baseTimeScale = NormalizeBaseTimeScale(current / divisor);
        }

        private static void ReleaseSpeedLayer()
        {
            EffectiveMultiplier = NormalMultiplier;
            _targetMultiplier = NormalMultiplier;
            _transitionStartMultiplier = NormalMultiplier;
            _transitionStartTime = null;
            _safeStateStartTime = null;
            _idleStartTime = null;
            _baseTimeScale = NormalMultiplier;
            WriteTimeScale(NormalMultiplier, NormalMultiplier);
            _hasAppliedTimeScale = false;
        }

        private static void ReleaseSpeedLayerIfNeeded()
        {
            if (_hasAppliedTimeScale
                || !Mathf.IsEqualApprox(_lastAppliedMultiplier, NormalMultiplier, TimeScaleEpsilon)
                || !Mathf.IsEqualApprox(EffectiveMultiplier, NormalMultiplier, TimeScaleEpsilon)
                || !Mathf.IsEqualApprox(_targetMultiplier, NormalMultiplier, TimeScaleEpsilon))
                ReleaseSpeedLayer();
        }

        private static void WriteTimeScale(float timeScale, float multiplier)
        {
            timeScale = Math.Max(0f, timeScale);
            if (!Mathf.IsEqualApprox((float)Engine.TimeScale, timeScale, TimeScaleEpsilon))
                Engine.TimeScale = timeScale;

            _lastAppliedTimeScale = timeScale;
            _lastAppliedMultiplier = multiplier;
            _hasAppliedTimeScale = true;
        }

        private static float NormalizeBaseTimeScale(float timeScale)
        {
            return Mathf.Clamp(timeScale, 0f, NormalMultiplier);
        }

        private static void CleanupExpiredTimedScopes()
        {
            if (TimedScopes.Count == 0)
                return;

            foreach (var reason in TimedScopes.Where(pair => pair.Value <= Now).Select(pair => pair.Key).ToArray())
                TimedScopes.Remove(reason);
        }

        private static void EndScope(SafeSpeedReason reason)
        {
            if (!ActiveScopes.TryGetValue(reason, out var count))
                return;

            if (count <= 1)
                ActiveScopes.Remove(reason);
            else
                ActiveScopes[reason] = count - 1;

            ProcessFrame(0);
        }

        private static void NormalizeSettings()
        {
            ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey, settings =>
            {
                settings.SpeedMultiplier = ClampMultiplier(settings.SpeedMultiplier);
                settings.CardAnimationMultiplier = ClampMultiplier(settings.CardAnimationMultiplier);
                settings.CardResolutionMultiplier = ClampMultiplier(settings.CardResolutionMultiplier);
                settings.CombatPresentationMultiplier = ClampMultiplier(settings.CombatPresentationMultiplier);
                settings.SchemaVersion = SpeedSettings.CurrentSchemaVersion;
                settings.TransitionDuration = Mathf.Clamp(
                    settings.TransitionDuration,
                    MinTransitionDuration,
                    MaxTransitionDuration);
                settings.TimeThreshold = Mathf.Clamp(settings.TimeThreshold, MinTimeThreshold, MaxTimeThreshold);
            });
        }

        private static float ClampMultiplier(float multiplier)
        {
            return Mathf.Clamp(multiplier, MinSelectableMultiplier, MaxSelectableMultiplier);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }

        private sealed class SpeedScope(SafeSpeedReason reason) : IDisposable
        {
            private bool _disposed;

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                EndScope(reason);
            }
        }
    }
}
