using Godot;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.Runs;
using STS2QuickAnimationMode.Data;

namespace STS2QuickAnimationMode.Utils
{
    /// <summary>
    ///     Manages the global game speed multiplier via Engine.TimeScale.
    ///     Supports both fixed speed and progressive acceleration modes.
    /// </summary>
    public static class SpeedManager
    {
        private const float IdleBufferDuration = 0.15f;
        public static readonly float[] SpeedOptions = [1.0f, 1.5f, 2.0f, 3.0f, 4.0f, 5.0f, 8.0f, 10.0f];
        public static readonly string[] SpeedLabels = ["1x", "1.5x", "2x", "3x", "4x", "5x", "8x", "10x"];

        public static readonly float[] TransitionDurationOptions = [1.0f, 2.0f, 3.0f, 5.0f, 10.0f, 15.0f, 20.0f];
        public static readonly string[] TransitionDurationLabels = ["1s", "2s", "3s", "5s", "10s", "15s", "20s"];

        // Time thresholds for acceleration (in seconds, real time)
        public static readonly float[] TimeThresholdOptions = [0.5f, 1.0f, 2.0f, 3.0f, 5.0f, 10.0f];
        public static readonly string[] TimeThresholdLabels = ["0.5s", "1s", "2s", "3s", "5s", "10s"];

        // Progressive acceleration state
        private static float _currentDisplayMultiplier = 1.0f;
        private static float _targetMultiplier = 1.0f;

        // Time-based acceleration state
        private static double? _accelerationStartTime;
        private static double? _transitionStartTime;
        private static float _transitionStartMultiplier = 1.0f;

        // Idle buffer state
        private static double? _idleStartTime;

        private static SpeedSettings Settings => ModDataStore.Get<SpeedSettings>(ModDataStore.SettingsKey);

        public static float CurrentMultiplier => Settings.SpeedMultiplier;
        public static bool ProgressiveEnabled => Settings.ProgressiveEnabled;
        public static float TransitionDuration => Settings.TransitionDuration;
        public static float TimeThreshold => Settings.TimeThreshold;

        /// <summary>
        ///     The actual multiplier being applied (may differ from target during transitions)
        /// </summary>
        public static float EffectiveMultiplier => ProgressiveEnabled ? _currentDisplayMultiplier : CurrentMultiplier;

        public static int CurrentIndex
        {
            get
            {
                var multiplier = CurrentMultiplier;
                for (var i = 0; i < SpeedOptions.Length; i++)
                    if (Mathf.IsEqualApprox(SpeedOptions[i], multiplier))
                        return i;
                return 0;
            }
        }

        public static int TransitionDurationIndex
        {
            get
            {
                var duration = TransitionDuration;
                for (var i = 0; i < TransitionDurationOptions.Length; i++)
                    if (Mathf.IsEqualApprox(TransitionDurationOptions[i], duration))
                        return i;
                return 4;
            }
        }

        public static int TimeThresholdIndex
        {
            get
            {
                var threshold = TimeThreshold;
                for (var i = 0; i < TimeThresholdOptions.Length; i++)
                    if (Mathf.IsEqualApprox(TimeThresholdOptions[i], threshold))
                        return i;
                return 3;
            }
        }

        public static void Initialize()
        {
            _currentDisplayMultiplier = ProgressiveEnabled ? 1.0f : CurrentMultiplier;
            _targetMultiplier = _currentDisplayMultiplier;
            ApplySpeed();
            Main.Logger.Info(
                $"SpeedManager initialized, multiplier: {CurrentMultiplier}x, progressive: {ProgressiveEnabled}");
        }

        public static void SetSpeedIndex(int index)
        {
            if (index < 0 || index >= SpeedOptions.Length) return;

            var newSpeed = SpeedOptions[index];
            ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey, data => data.SpeedMultiplier = newSpeed);
            ModDataStore.Save(ModDataStore.SettingsKey);

            if (!ProgressiveEnabled)
                ApplySpeed();

            Main.Logger.Info($"Speed set to {newSpeed}x (index {index})");
        }

        public static void SetProgressiveEnabled(bool enabled)
        {
            ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey, data => data.ProgressiveEnabled = enabled);
            ModDataStore.Save(ModDataStore.SettingsKey);

            if (enabled)
            {
                _currentDisplayMultiplier = 1.0f;
                _targetMultiplier = 1.0f;
                _accelerationStartTime = null;
            }
            else
            {
                _currentDisplayMultiplier = CurrentMultiplier;
                _targetMultiplier = CurrentMultiplier;
            }

            ApplySpeed();
            Main.Logger.Info($"Progressive acceleration {(enabled ? "enabled" : "disabled")}");
        }

        public static void SetTransitionDurationIndex(int index)
        {
            if (index < 0 || index >= TransitionDurationOptions.Length) return;

            var newDuration = TransitionDurationOptions[index];
            ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey, data => data.TransitionDuration = newDuration);
            ModDataStore.Save(ModDataStore.SettingsKey);
            Main.Logger.Info($"Transition duration set to {newDuration}s");
        }

        public static void SetTimeThresholdIndex(int index)
        {
            if (index < 0 || index >= TimeThresholdOptions.Length) return;

            var newThreshold = TimeThresholdOptions[index];
            ModDataStore.Modify<SpeedSettings>(ModDataStore.SettingsKey, data => data.TimeThreshold = newThreshold);
            ModDataStore.Save(ModDataStore.SettingsKey);
            Main.Logger.Info($"Time threshold set to {newThreshold}s");
        }

        /// <summary>
        ///     Called every frame to check game state and smoothly transition speed
        /// </summary>
        public static void ProcessFrame(double delta)
        {
            if (!ProgressiveEnabled) return;

            UpdateTargetSpeedFromGameState();
            ApplySpeedTransition(delta);
        }

        private static void UpdateTargetSpeedFromGameState()
        {
            if (RunManager.Instance == null)
            {
                ResetToNormalSpeed();
                return;
            }

            var actionExecutor = RunManager.Instance.ActionExecutor;
            var actionQueueSet = RunManager.Instance.ActionQueueSet;

            if (actionExecutor == null || actionQueueSet == null)
            {
                ResetToNormalSpeed();
                return;
            }

            var isQueueEmpty = actionQueueSet.IsEmpty;
            var isRunning = actionExecutor.IsRunning;
            var isPaused = actionExecutor.IsPaused;
            var currentAction = actionExecutor.CurrentlyRunningAction;
            var isGatheringPlayerChoice = currentAction?.State == GameActionState.GatheringPlayerChoice;

            if (isGatheringPlayerChoice)
            {
                ResetToNormalSpeed();
                return;
            }

            var hasActiveWork = currentAction != null || (!isQueueEmpty && !isPaused) || isRunning;

            if (!hasActiveWork)
            {
                _idleStartTime ??= Time.GetTicksMsec() / 1000.0;

                var currentTime = Time.GetTicksMsec() / 1000.0;
                var idleElapsed = currentTime - _idleStartTime.Value;

                if (idleElapsed >= IdleBufferDuration) ResetToNormalSpeed();
                return;
            }

            _idleStartTime = null;

            _accelerationStartTime ??= Time.GetTicksMsec() / 1000.0;

            var elapsedTime = Time.GetTicksMsec() / 1000.0 - _accelerationStartTime.Value;

            _targetMultiplier = elapsedTime >= TimeThreshold ? CurrentMultiplier : 1.0f;
        }

        private static void ResetToNormalSpeed()
        {
            _targetMultiplier = 1.0f;
            _currentDisplayMultiplier = 1.0f;
            _accelerationStartTime = null;
            _transitionStartTime = null;
            _idleStartTime = null;
            ApplySpeed();
        }

        public static void ResetSpeed()
        {
            _currentDisplayMultiplier = ProgressiveEnabled ? 1.0f : CurrentMultiplier;
            _targetMultiplier = _currentDisplayMultiplier;
            _accelerationStartTime = null;
            _transitionStartTime = null;
            _idleStartTime = null;
            ApplySpeed();
        }

        private static void ApplySpeedTransition(double delta)
        {
            if (Mathf.IsEqualApprox(_currentDisplayMultiplier, _targetMultiplier, 0.01f))
            {
                _currentDisplayMultiplier = _targetMultiplier;
                ApplySpeed();
                return;
            }

            if (_transitionStartTime == null)
            {
                _transitionStartTime = Time.GetTicksMsec() / 1000.0;
                _transitionStartMultiplier = _currentDisplayMultiplier;
            }

            var currentTime = Time.GetTicksMsec() / 1000.0;
            var elapsedTime = currentTime - _transitionStartTime.Value;
            var progress = (float)Math.Min(elapsedTime / TransitionDuration, 1.0);

            var smoothProgress = progress < 0.5f
                ? 2f * progress * progress
                : 1f - Mathf.Pow(-2f * progress + 2f, 2f) / 2f;

            _currentDisplayMultiplier = Mathf.Lerp(_transitionStartMultiplier, _targetMultiplier, smoothProgress);
            _currentDisplayMultiplier = Mathf.Clamp(_currentDisplayMultiplier, 1.0f, CurrentMultiplier);

            ApplySpeed();
        }

        public static void ApplySpeed()
        {
            Engine.TimeScale = EffectiveMultiplier;
        }
    }
}
