using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace STS2QuickAnimationMode.Patches
{
    public partial class NSettingHoverTipLine : MarginContainer
    {
        private static readonly MethodInfo? HoverTipTitleSetter =
            typeof(HoverTip).GetProperty(nameof(HoverTip.Title))?.GetSetMethod(true);

        private IHoverTip? _hoverTip;
        private string? _descriptionKey;
        private string? _descriptionFallback;
        private string? _titleFallback;
        private string? _titleKey;

        public override void _Ready()
        {
            Connect(Control.SignalName.MouseEntered, Callable.From(OnHovered));
            Connect(Control.SignalName.MouseExited, Callable.From(OnUnhovered));

            if (_titleKey != null && _descriptionKey != null)
            {
                _hoverTip = CreateHoverTip(
                    Main.I18N.Get(_titleKey, _titleFallback ?? _titleKey),
                    Main.I18N.Get(_descriptionKey, _descriptionFallback ?? _descriptionKey)
                );
            }
        }

        public void SetHoverTipKeys(
            string titleKey,
            string descriptionKey,
            string titleFallback,
            string descriptionFallback)
        {
            _titleKey = titleKey;
            _descriptionKey = descriptionKey;
            _titleFallback = titleFallback;
            _descriptionFallback = descriptionFallback;
        }

        private static HoverTip CreateHoverTip(string title, string description)
        {
            var hoverTip = new HoverTip(new LocString("settings_ui", "FASTMODE"), description)
            {
                Id = $"{Const.ModId}.settings.{title}"
            };

            if (HoverTipTitleSetter == null)
                return hoverTip;

            object boxedHoverTip = hoverTip;
            HoverTipTitleSetter.Invoke(boxedHoverTip, [title]);
            return (HoverTip)boxedHoverTip;
        }

        private void OnHovered()
        {
            if (_hoverTip == null)
                return;

            NHoverTipSet.CreateAndShow(this, _hoverTip)?.SetGlobalPosition(GlobalPosition + NSettingsScreen.settingTipsOffset);
        }

        private void OnUnhovered()
        {
            NHoverTipSet.Remove(this);
        }
    }
}
