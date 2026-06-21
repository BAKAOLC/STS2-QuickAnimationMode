using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2RitsuLib.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    /// <summary>
    ///     Injects speed control settings into the General settings tab,
    ///     including speed multiplier and progressive acceleration options.
    /// </summary>
    public class SettingsScreenPatch : IPatchMethod
    {
        public static string PatchId => "settings_speed_control";
        public static string Description => "Add speed multiplier control to settings screen";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NSettingsScreen), nameof(NSettingsScreen.OnSubmenuOpened))];
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(NSettingsScreen __instance)
        {
            try
            {
                InjectSpeedControl(__instance);
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Failed to inject speed control: {ex.Message}");
                Main.Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        private static void InjectSpeedControl(NSettingsScreen settingsScreen)
        {
            var generalPanel = settingsScreen.GetNode<NSettingsPanel>("%GeneralSettings");
            var content = generalPanel.Content;

            if (content.GetNodeOrNull("SpeedMultiplier") != null)
                return;

            var fastModeNode = content.GetNodeOrNull<Control>("FastMode");
            var screenshakeNode = content.GetNodeOrNull<Control>("Screenshake");
            if (screenshakeNode == null)
            {
                Main.Logger.Error("Could not find Screenshake node as template");
                return;
            }

            var existingPaginator = screenshakeNode.GetNodeOrNull<NPaginator>("Paginator");
            if (existingPaginator == null)
            {
                Main.Logger.Error("Could not find Paginator node in Screenshake");
                return;
            }

            var insertIndex = fastModeNode != null
                ? fastModeNode.GetIndex() + 1
                : content.GetChildCount();

            var speedDivider = CreateDivider("SpeedMultiplierDivider");
            content.AddChild(speedDivider);
            content.MoveChild(speedDivider, insertIndex++);

            var speedLine = CreateSettingLine(
                "SpeedMultiplier",
                Main.I18N.Get("SPEED_MULTIPLIER", "Speed Multiplier"),
                "SPEED_MULTIPLIER",
                "SPEED_MULTIPLIER_DESCRIPTION",
                "Speed Multiplier",
                "Target game speed. With Progressive Acceleration off, this applies all the time. With it on, this is the maximum speed reached during long automatic sequences.",
                fastModeNode,
                existingPaginator,
                () => new NSpeedPaginator()
            );
            content.AddChild(speedLine);
            content.MoveChild(speedLine, insertIndex++);

            var progressiveDivider = CreateDivider("ProgressiveToggleDivider");
            content.AddChild(progressiveDivider);
            content.MoveChild(progressiveDivider, insertIndex++);

            var progressiveLine = CreateSettingLine(
                "ProgressiveToggle",
                Main.I18N.Get("PROGRESSIVE_TOGGLE", "Progressive Acceleration"),
                "PROGRESSIVE_TOGGLE",
                "PROGRESSIVE_TOGGLE_DESCRIPTION",
                "Progressive Acceleration",
                "Starts at 1x while waiting for input, then accelerates only while actions, animations, and automatic effects keep resolving. Card selection does not count toward the timer.",
                fastModeNode,
                existingPaginator,
                () => new NProgressiveTogglePaginator()
            );
            content.AddChild(progressiveLine);
            content.MoveChild(progressiveLine, insertIndex++);

            var timeThresholdDivider = CreateDivider("TimeThresholdDivider");
            content.AddChild(timeThresholdDivider);
            content.MoveChild(timeThresholdDivider, insertIndex++);

            var timeThresholdLine = CreateSettingLine(
                "TimeThreshold",
                Main.I18N.Get("TIME_THRESHOLD", "Time Threshold"),
                "TIME_THRESHOLD",
                "TIME_THRESHOLD_DESCRIPTION",
                "Time Threshold",
                "How long continuous non-interactive action must run before progressive acceleration begins. Lower values speed up sooner; higher values keep short actions closer to normal speed.",
                fastModeNode,
                existingPaginator,
                () => new NTimeThresholdPaginator()
            );
            content.AddChild(timeThresholdLine);
            content.MoveChild(timeThresholdLine, insertIndex++);

            var transitionDivider = CreateDivider("TransitionDurationDivider");
            content.AddChild(transitionDivider);
            content.MoveChild(transitionDivider, insertIndex++);

            var transitionLine = CreateSettingLine(
                "TransitionDuration",
                Main.I18N.Get("TRANSITION_DURATION", "Transition Duration"),
                "TRANSITION_DURATION",
                "TRANSITION_DURATION_DESCRIPTION",
                "Transition Duration",
                "How long the ramp from 1x to the selected Speed Multiplier takes after the threshold. Lower values feel snappier; higher values feel smoother.",
                fastModeNode,
                existingPaginator,
                () => new NTransitionDurationPaginator()
            );
            content.AddChild(transitionLine);
            content.MoveChild(transitionLine, insertIndex);

            Main.Logger.Info("Speed control settings injected into settings");
        }

        private static MarginContainer CreateSettingLine(
            string name,
            string labelText,
            string hoverTipTitleKey,
            string hoverTipDescriptionKey,
            string hoverTipTitleFallback,
            string hoverTipDescriptionFallback,
            Control? templateLabelSource,
            NPaginator templatePaginator,
            Func<NPaginator> createPaginator)
        {
            var line = new NSettingHoverTipLine();
            line.Name = name;
            line.SetHoverTipKeys(
                hoverTipTitleKey,
                hoverTipDescriptionKey,
                hoverTipTitleFallback,
                hoverTipDescriptionFallback);
            line.CustomMinimumSize = new(0, 64);
            line.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
            line.AddThemeConstantOverride("margin_left", 12);
            line.AddThemeConstantOverride("margin_top", 0);
            line.AddThemeConstantOverride("margin_right", 12);
            line.AddThemeConstantOverride("margin_bottom", 0);

            var existingLabel = templateLabelSource?.GetNodeOrNull<Control>("Label");
            RichTextLabel label;
            if (existingLabel != null)
                label = (RichTextLabel)existingLabel.Duplicate();
            else
                label = new();
            label.Text = labelText;
            label.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
            label.MouseFilter = Control.MouseFilterEnum.Ignore;
            line.AddChild(label);

            var paginator = createPaginator();
            paginator.Name = "Paginator";
            paginator.CustomMinimumSize = new(324, 64);
            paginator.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
            paginator.FocusMode = Control.FocusModeEnum.All;
            paginator.MouseFilter = Control.MouseFilterEnum.Ignore;

            foreach (var child in templatePaginator.GetChildren())
            {
                var copy = child.Duplicate();
                paginator.AddChild(copy);
                SetOwnerRecursive(copy, paginator);
            }

            line.AddChild(paginator);
            return line;
        }

        private static void SetOwnerRecursive(Node node, Node owner)
        {
            node.Owner = owner;
            foreach (var child in node.GetChildren())
                SetOwnerRecursive(child, owner);
        }

        private static ColorRect CreateDivider(string name)
        {
            var divider = new ColorRect();
            divider.Name = name;
            divider.CustomMinimumSize = new(0, 2);
            divider.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
            divider.Color = new(0.909804f, 0.862745f, 0.745098f, 0.25098f);
            return divider;
        }
    }
}
