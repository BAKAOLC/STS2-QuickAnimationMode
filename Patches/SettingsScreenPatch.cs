using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Settings;

namespace STS2QuickAnimationMode.Patches
{
    public class SettingsScreenPatch : IPatchMethod
    {
        public static string PatchId => "settings_speed_control_link";
        public static string Description => "Add a vanilla settings entry that opens the RitsuLib Speed Control page";

        public static ModPatchTarget[] GetTargets()
        {
            return [new(typeof(NSettingsScreen), nameof(NSettingsScreen.OnSubmenuOpened))];
        }

        public static void Postfix(NSettingsScreen __instance)
        {
            try
            {
                InjectSettingsLink(__instance);
            }
            catch (Exception ex)
            {
                Main.Logger.Error($"Failed to inject speed control settings link: {ex.Message}");
                Main.Logger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        private static void InjectSettingsLink(NSettingsScreen settingsScreen)
        {
            var generalPanel = settingsScreen.GetNode<NSettingsPanel>("%GeneralSettings");
            var content = generalPanel.Content;

            if (content.GetNodeOrNull("SpeedControlSettingsEntry") != null)
                return;

            var fastModeNode = content.GetNodeOrNull<Control>("FastMode");
            var insertIndex = fastModeNode != null
                ? fastModeNode.GetIndex() + 1
                : content.GetChildCount();

            var divider = CreateDivider("SpeedControlSettingsDivider");
            content.AddChild(divider);
            content.MoveChild(divider, insertIndex++);

            var entry = CreateEntryLine();
            content.AddChild(entry);
            content.MoveChild(entry, insertIndex);
        }

        private static MarginContainer CreateEntryLine()
        {
            var title = Main.I18N.Get("SETTINGS_PAGE_TITLE", "Speed Control");
            var line = ModSettingsGameSettingsEntryLine.Create(OpenSpeedControlSettings);
            line.Name = "SpeedControlSettingsEntry";

            if (line.GetNodeOrNull<MegaRichTextLabel>("ContentRow/Label") is { } label)
                label.SetTextAutoSize(title);
            if (line.GetNodeOrNull<MegaLabel>("ContentRow/RitsuLibModSettingsButton/Label") is { } buttonLabel)
                buttonLabel.SetTextAutoSize(Main.I18N.Get("OPEN_RITSULIB_SETTINGS_BUTTON", "Open Settings"));

            return line;
        }

        private static void OpenSpeedControlSettings()
        {
            var result = ModSettingsNavigator.RequestOpenByIds(
                Const.ModId,
                Const.SettingsPageId,
                Const.SettingsSectionId,
                null);
            if (!result.Success)
                Main.Logger.Warn($"Could not open Speed Control settings page: {result.Message}");
        }

        private static ColorRect CreateDivider(string name)
        {
            return new()
            {
                Name = name,
                CustomMinimumSize = new(0, 2),
                SizeFlagsHorizontal = Control.SizeFlags.Fill | Control.SizeFlags.Expand,
                Color = new(0.909804f, 0.862745f, 0.745098f, 0.25098f),
            };
        }
    }
}
