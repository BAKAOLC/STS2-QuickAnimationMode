using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2QuickAnimationMode.Patching.Models;

namespace STS2QuickAnimationMode.Patches
{
    /// <summary>
    ///     Injects a speed multiplier paginator into the General settings tab,
    ///     right after the FastMode setting.
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

            var speedLine = new MarginContainer();
            speedLine.Name = "SpeedMultiplier";
            speedLine.CustomMinimumSize = new(0, 64);
            speedLine.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
            speedLine.AddThemeConstantOverride("margin_left", 12);
            speedLine.AddThemeConstantOverride("margin_top", 0);
            speedLine.AddThemeConstantOverride("margin_right", 12);
            speedLine.AddThemeConstantOverride("margin_bottom", 0);

            var existingLabel = fastModeNode?.GetNodeOrNull<Control>("Label");
            RichTextLabel label;
            if (existingLabel != null)
                label = (RichTextLabel)existingLabel.Duplicate();
            else
                label = new();
            label.Text = Main.I18N.Get("SPEED_MULTIPLIER", "Speed Multiplier");
            label.SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand;
            label.MouseFilter = Control.MouseFilterEnum.Ignore;
            speedLine.AddChild(label);

            var existingPaginator = screenshakeNode.GetNodeOrNull<NPaginator>("Paginator");
            if (existingPaginator == null)
            {
                Main.Logger.Error("Could not find Paginator node in Screenshake");
                return;
            }

            var speedPaginator = new NSpeedPaginator();
            speedPaginator.Name = "Paginator";
            speedPaginator.CustomMinimumSize = new(324, 64);
            speedPaginator.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
            speedPaginator.FocusMode = Control.FocusModeEnum.All;
            speedPaginator.MouseFilter = Control.MouseFilterEnum.Ignore;

            foreach (var child in existingPaginator.GetChildren())
            {
                var copy = child.Duplicate();
                speedPaginator.AddChild(copy);
                SetOwnerRecursive(copy, speedPaginator);
            }

            speedLine.AddChild(speedPaginator);

            var insertIndex = fastModeNode != null
                ? fastModeNode.GetIndex() + 1
                : content.GetChildCount();

            var divider = CreateDivider("SpeedMultiplierDivider");
            content.AddChild(divider);
            content.MoveChild(divider, insertIndex);
            insertIndex++;

            content.AddChild(speedLine);
            content.MoveChild(speedLine, insertIndex);

            Main.Logger.Info("Speed multiplier control injected into settings");
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
