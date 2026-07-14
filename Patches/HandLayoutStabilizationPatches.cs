using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using STS2QuickAnimationMode.Utils;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches;

internal static class HandLayoutStabilizer
{
    private const float RotateSpeed = 10f;
    private const float AngleSnapThreshold = 0.1f;
    private const float ScaleSpeed = 8f;
    private const float ScaleSnapThreshold = 0.002f;
    private const float MoveSpeed = 7f;
    private const float PositionSnapDistanceSquared = 1f;
    private const float ReenableHitboxThreshold = 200f;

    private static readonly FieldInfo? TargetPositionField =
        typeof(NHandCardHolder).GetField("_targetPosition", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? TargetAngleField =
        typeof(NHandCardHolder).GetField("_targetAngle", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? TargetScaleField =
        typeof(NHandCardHolder).GetField("_targetScale", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? AngleCancelTokenField =
        typeof(NHandCardHolder).GetField("_angleCancelToken", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? PositionCancelTokenField =
        typeof(NHandCardHolder).GetField("_positionCancelToken", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? ScaleCancelTokenField =
        typeof(NHandCardHolder).GetField("_scaleCancelToken", BindingFlags.Instance | BindingFlags.NonPublic);

    public static bool TryStartAngleAnimation(NHandCardHolder holder, float angle)
    {
        if (!SpeedManager.AreBehaviorPatchesEnabled || TargetAngleField == null || AngleCancelTokenField == null)
            return false;

        TargetAngleField.SetValue(holder, angle);
        var token = ReplaceToken(holder, AngleCancelTokenField);
        TaskHelper.RunSafely(AnimateAngle(holder, angle, token));
        return true;
    }

    public static bool TryStartPositionAnimation(NHandCardHolder holder, Vector2 position)
    {
        if (!SpeedManager.AreBehaviorPatchesEnabled || TargetPositionField == null || PositionCancelTokenField == null)
            return false;

        TargetPositionField.SetValue(holder, position);
        var token = ReplaceToken(holder, PositionCancelTokenField);
        TaskHelper.RunSafely(AnimatePosition(holder, position, token));
        return true;
    }

    public static bool TryStartScaleAnimation(NHandCardHolder holder, Vector2 scale)
    {
        if (!SpeedManager.AreBehaviorPatchesEnabled || TargetScaleField == null || ScaleCancelTokenField == null)
            return false;

        TargetScaleField.SetValue(holder, scale);
        var token = ReplaceToken(holder, ScaleCancelTokenField);
        TaskHelper.RunSafely(AnimateScale(holder, scale, token));
        return true;
    }

    private static CancellationTokenSource ReplaceToken(NHandCardHolder holder, FieldInfo field)
    {
        if (field.GetValue(holder) is CancellationTokenSource oldToken)
            oldToken.Cancel();

        var token = new CancellationTokenSource();
        field.SetValue(holder, token);
        return token;
    }

    private static async Task AnimateAngle(NHandCardHolder holder, float targetAngle, CancellationTokenSource token)
    {
        while (!token.IsCancellationRequested && holder.IsValid())
        {
            holder.RotationDegrees = Mathf.Lerp(holder.RotationDegrees, targetAngle, Step(holder, RotateSpeed));
            if (Mathf.Abs(holder.RotationDegrees - targetAngle) < AngleSnapThreshold)
            {
                holder.RotationDegrees = targetAngle;
                break;
            }

            await AwaitProcessFrameNonThrowing(holder, token);
        }
    }

    private static async Task AnimateScale(NHandCardHolder holder, Vector2 targetScale, CancellationTokenSource token)
    {
        while (!token.IsCancellationRequested && holder.IsValid())
        {
            holder.Scale = holder.Scale.Lerp(targetScale, Step(holder, ScaleSpeed));
            if (Mathf.Abs(targetScale.X - holder.Scale.X) < ScaleSnapThreshold)
            {
                holder.Scale = targetScale;
                break;
            }

            await AwaitProcessFrameNonThrowing(holder, token);
        }
    }

    private static async Task AnimatePosition(NHandCardHolder holder, Vector2 targetPosition,
        CancellationTokenSource token)
    {
        while (!token.IsCancellationRequested && holder.IsValid())
        {
            holder.Position = holder.Position.Lerp(targetPosition, Step(holder, MoveSpeed));
            var xDistance = Mathf.Abs(holder.Position.X - targetPosition.X);
            if (!holder.Hitbox.IsEnabled && xDistance < ReenableHitboxThreshold)
                holder.Hitbox.SetEnabled(true);

            if (holder.Position.DistanceSquaredTo(targetPosition) < PositionSnapDistanceSquared)
            {
                holder.Position = targetPosition;
                return;
            }

            await AwaitProcessFrameNonThrowing(holder, token);
        }

        if (holder.IsValid()
            && !holder.Hitbox.IsEnabled
            && holder.Position.DistanceSquaredTo(targetPosition) < ReenableHitboxThreshold * ReenableHitboxThreshold)
            holder.Hitbox.SetEnabled(true);
    }

    private static float Step(Node holder, float speed)
    {
        return Mathf.Clamp((float)holder.GetProcessDeltaTime() * speed, 0f, 1f);
    }

    private static async Task AwaitProcessFrameNonThrowing(Node node, CancellationTokenSource token)
    {
        if (token.IsCancellationRequested)
            return;

        var tree = node.IsInsideTree() ? node.GetTree() : null;
        if (tree == null)
        {
            token.Cancel();
            return;
        }

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        if (!token.IsCancellationRequested && (!node.IsValid() || !node.IsInsideTree()))
            token.Cancel();
    }
}

public class HandTargetPositionStabilizationPatch : IPatchMethod
{
    public static string PatchId => "hand_target_position_stabilization";
    public static string Description => "Clamp hand position animation interpolation";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(NHandCardHolder), nameof(NHandCardHolder.SetTargetPosition), [typeof(Vector2)])
        ];
    }

    public static bool Prefix(NHandCardHolder __instance, Vector2 position)
    {
        return !HandLayoutStabilizer.TryStartPositionAnimation(__instance, position);
    }
}

public class HandTargetAngleStabilizationPatch : IPatchMethod
{
    public static string PatchId => "hand_target_angle_stabilization";
    public static string Description => "Clamp hand angle animation interpolation";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(NHandCardHolder), nameof(NHandCardHolder.SetTargetAngle), [typeof(float)])];
    }

    public static bool Prefix(NHandCardHolder __instance, float angle)
    {
        return !HandLayoutStabilizer.TryStartAngleAnimation(__instance, angle);
    }
}

public class HandTargetScaleStabilizationPatch : IPatchMethod
{
    public static string PatchId => "hand_target_scale_stabilization";
    public static string Description => "Clamp hand scale animation interpolation";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(NHandCardHolder), nameof(NHandCardHolder.SetTargetScale), [typeof(Vector2)])];
    }

    public static bool Prefix(NHandCardHolder __instance, Vector2 scale)
    {
        return !HandLayoutStabilizer.TryStartScaleAnimation(__instance, scale);
    }
}