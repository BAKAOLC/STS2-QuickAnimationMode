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
        LoadingScreen,
    }

    public static class SpeedManager
    {
        private const float NormalMultiplier = 1.0f;
        public const float MinSelectableMultiplier = 0.1f;
        public const float MaxSelectableMultiplier = 10.0f;
        public const float SpeedSliderStep = 0.1f;
        private const float DefaultTimelineScopeSeconds = 2.0f;
        private const float IdleBufferDuration = 0.1f;

        public static readonly float[] TransitionDurationOptions = [0.0f, 0.1f, 0.2f, 0.5f, 1.0f, 2.0f, 3.0f];
        public static readonly string[] TransitionDurationLabels = ["0s", "0.1s", "0.2s", "0.5s", "1s", "2s", "3s"];

        public static readonly float[] TimeThresholdOptions = [0.0f, 0.05f, 0.1f, 0.25f, 0.5f, 1.0f, 2.0f];
        public static readonly string[] TimeThresholdLabels = ["0s", "0.05s", "0.1s", "0.25s", "0.5s", "1s", "2s"];

        private static readonly Dictionary<SafeSpeedReason, int> ActiveScopes = new();
        private static readonly Dictionary<SafeSpeedReason, double> TimedScopes = new();

        private static float _targetMultiplier = NormalMultiplier;
        private static float _transitionStartMultiplier = NormalMultiplier;
        private static double? _transitionStartTime;
        private static double? _safeStateStartTime;
        private static double? _idleStartTime;
        private static int _localPlayerChoiceDepth;

        private static SpeedSettings Settings => ModDataStore.Get<SpeedSettings>(ModDataStore.SettingsKey);

        public static float CurrentMultiplier => ClampMultiplier(Settings.SpeedMultiplier);
        public static bool ProgressiveEnabled => Settings.ProgressiveAccelerationEnabled;
        public static float TransitionDuration => Math.Max(0.0f, Settings.TransitionDuration);
        public static float TimeThreshold => Math.Max(0.0f, Settings.TimeThreshold);
        public static float EffectiveMultiplier { get; private set; } = NormalMultiplier;

        public static int SpeedOptionCount => (int)MathF.Round((MaxSelectableMultiplier - MinSelectableMultiplier) /
                                                               SpeedSliderStep) + 1;

        public static int CurrentIndex => SpeedIndexFromValue(CurrentMultiplier);
        public static int TransitionDurationIndex => IndexOfClosest(TransitionDurationOptions, TransitionDuration);
        public static int TimeThresholdIndex => IndexOfClosest(TimeThresholdOptions, TimeThreshold);

        private static double Now => Time.GetTicksMsec() / 1000.0;

        public static void Initialize()
        {
            NormalizeSettings();
            ResetSpeed();
            EnsureProcessPump();
            Main.Logger.Info(
                $"SpeedManager initialized, mode: {Settings.AccelerationMode}, multiplier: {CurrentMultiplier}x");
        }

        public static IDisposable BeginScope(SafeSpeedReason reason)
        {
            ActiveScopes.TryGetValue(reason, out var count);
            ActiveScopes[reason] = count + 1;
            ProcessFrame(0);
            return new SpeedScope(reason);
        }

        public static Task TrackAsync(Task task, SafeSpeedReason reason)
        {
            return TrackAsyncCore(task, reason);
        }

        public static Task<T> TrackAsync<T>(Task<T> task, SafeSpeedReason reason)
        {
            return TrackAsyncCore(task, reason);
        }

        public static void ActivateTimed(SafeSpeedReason reason, float seconds = DefaultTimelineScopeSeconds)
        {
            if (seconds <= 0)
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
            _localPlayerChoiceDepth++;
            ForceNormalSpeed();
        }

        public static void EndLocalPlayerChoice()
        {
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
                    break;
                case SpeedAccelerationMode.AlwaysOn:
                    SetTarget(CurrentMultiplier, true);
                    break;
                case SpeedAccelerationMode.SafeState:
                    UpdateSafeStateTarget();
                    break;
                default:
                    SetTarget(NormalMultiplier, true);
                    break;
            }

            ApplySpeedTransition();
        }

        public static void ResetSpeed()
        {
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
            var value = Mathf.Clamp(EffectiveMultiplier, MinSelectableMultiplier, MaxSelectableMultiplier);
            if (!Mathf.IsEqualApprox(Engine.TimeScale, value, 0.001f))
                Engine.TimeScale = value;
        }

        public static string GetSpeedLabel(int index)
        {
            return $"{SpeedValueFromIndex(index):0.#}x";
        }

        public static void SetSpeedIndex(int index)
        {
            if (index < 0 || index >= SpeedOptionCount)
                return;

            ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey,
                data => data.SpeedMultiplier = SpeedValueFromIndex(index));
            ModDataStore.Save(ModDataStore.SettingsKey);
            OnSettingsChanged();
        }

        public static void SetProgressiveEnabled(bool enabled)
        {
            ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey,
                data => data.ProgressiveAccelerationEnabled = enabled);
            ModDataStore.Save(ModDataStore.SettingsKey);
            OnSettingsChanged();
        }

        public static void SetTransitionDurationIndex(int index)
        {
            if (index < 0 || index >= TransitionDurationOptions.Length)
                return;

            ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey,
                data => data.TransitionDuration = TransitionDurationOptions[index]);
            ModDataStore.Save(ModDataStore.SettingsKey);
            OnSettingsChanged();
        }

        public static void SetTimeThresholdIndex(int index)
        {
            if (index < 0 || index >= TimeThresholdOptions.Length)
                return;

            ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey,
                data => data.TimeThreshold = TimeThresholdOptions[index]);
            ModDataStore.Save(ModDataStore.SettingsKey);
            OnSettingsChanged();
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
            return (IsReasonActive(SafeSpeedReason.CardPileSequence) && Settings.AccelerateCardPileSequences)
                   || (IsReasonActive(SafeSpeedReason.CardPlayResolution) && Settings.AccelerateCardPlayResolution)
                   || (IsReasonActive(SafeSpeedReason.TurnTransition) && Settings.AccelerateTurnTransitions)
                   || (IsReasonActive(SafeSpeedReason.EnemyAction) && Settings.AccelerateEnemyActions)
                   || (IsReasonActive(SafeSpeedReason.TimelineAnimation) && Settings.AccelerateTimelineAnimations)
                   || (IsReasonActive(SafeSpeedReason.LoadingScreen) && Settings.AccelerateLoadingScreens);
        }

        private static void RequestHandRepairForReason(SafeSpeedReason reason)
        {
            if (reason is SafeSpeedReason.CardPileSequence or SafeSpeedReason.CardPlayResolution)
                HandStateRepair.RequestFullRepair();
        }

        private static bool IsReasonActive(SafeSpeedReason reason)
        {
            return (ActiveScopes.TryGetValue(reason, out var count) && count > 0)
                   || (TimedScopes.TryGetValue(reason, out var until) && until > Now);
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
                settings.SchemaVersion = SpeedSettings.CurrentSchemaVersion;
                settings.TransitionDuration = Mathf.Clamp(settings.TransitionDuration, 0.0f, 3.0f);
                settings.TimeThreshold = Mathf.Clamp(settings.TimeThreshold, 0.0f, 2.0f);
            });
        }

        private static float ClampMultiplier(float multiplier)
        {
            return Mathf.Clamp(multiplier, MinSelectableMultiplier, MaxSelectableMultiplier);
        }

        private static int SpeedIndexFromValue(float value)
        {
            return Math.Clamp((int)MathF.Round((ClampMultiplier(value) - MinSelectableMultiplier) / SpeedSliderStep),
                0,
                SpeedOptionCount - 1);
        }

        private static float SpeedValueFromIndex(int index)
        {
            return Mathf.Clamp(MinSelectableMultiplier + index * SpeedSliderStep,
                MinSelectableMultiplier,
                MaxSelectableMultiplier);
        }

        private static int IndexOfClosest(float[] values, float value)
        {
            var bestIndex = 0;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < values.Length; i++)
            {
                var distance = Math.Abs(values[i] - value);
                if (distance >= bestDistance)
                    continue;

                bestIndex = i;
                bestDistance = distance;
            }

            return bestIndex;
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
