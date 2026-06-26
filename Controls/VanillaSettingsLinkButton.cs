using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2RitsuLib.Settings;

namespace STS2QuickAnimationMode.Controls
{
    public sealed partial class VanillaSettingsLinkButton : NSettingsButton
    {
        private const string SelectionReticleScenePath = "res://scenes/ui/selection_reticle.tscn";

        private readonly Action _action;
        private readonly string _text;
        private MegaLabel? _buttonLabel;

        public VanillaSettingsLinkButton(string text, Action action)
        {
            _text = text;
            _action = action;

            CustomMinimumSize = new(320, 64);
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
            SizeFlagsVertical = SizeFlags.ShrinkBegin;
            FocusMode = FocusModeEnum.All;

            AddChild(CreateImage());
            AddChild(CreateLabel());
            AddChild(CreateSelectionReticle());
        }

        public override void _Ready()
        {
            ConnectSignals();
            _buttonLabel = GetNode<MegaLabel>("Label");
            _buttonLabel.SetTextAutoSize(_text);
            Callable.From(SyncPivots).CallDeferred();
        }

        protected override void OnRelease()
        {
            base.OnRelease();
            _action();
        }

        private void SyncPivots()
        {
            PivotOffset = Size * 0.5f;
            if (GetNodeOrNull<TextureRect>("Image") is { } image)
                image.PivotOffset = image.Size * 0.5f;
            if (_buttonLabel != null)
                _buttonLabel.PivotOffset = _buttonLabel.Size * 0.5f;
        }

        private static TextureRect CreateImage()
        {
            return new()
            {
                Name = "Image",
                Material = ModSettingsUiResources.CreateToneMaterial(ModSettingsButtonTone.Accent),
                CustomMinimumSize = new(64, 64),
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                Texture = ModSettingsUiResources.SettingsButtonTexture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
            };
        }

        private static MegaLabel CreateLabel()
        {
            var label = new MegaLabel
            {
                Name = "Label",
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            label.AddThemeColorOverride("font_color", new(0.91f, 0.86359f, 0.7462f));
            label.AddThemeColorOverride("font_shadow_color", new(0f, 0f, 0f, 0.25098f));
            label.AddThemeColorOverride("font_outline_color",
                ModSettingsUiResources.GetToneOutlineColor(ModSettingsButtonTone.Accent));
            label.AddThemeConstantOverride("shadow_offset_x", 4);
            label.AddThemeConstantOverride("shadow_offset_y", 3);
            label.AddThemeConstantOverride("outline_size", 12);
            label.AddThemeConstantOverride("shadow_outline_size", 0);
            label.AddThemeFontOverride("font", ModSettingsUiResources.KreonButton);
            label.AddThemeFontSizeOverride("font_size", 28);
            label.MinFontSize = 16;
            label.MaxFontSize = 28;
            return label;
        }

        private static Control CreateSelectionReticle()
        {
            var reticle = PreloadManager.Cache.GetScene(SelectionReticleScenePath).Instantiate<Control>();
            reticle.Name = "SelectionReticle";
            reticle.AnchorRight = 1f;
            reticle.AnchorBottom = 1f;
            reticle.GrowHorizontal = GrowDirection.Both;
            reticle.GrowVertical = GrowDirection.Both;
            reticle.MouseFilter = MouseFilterEnum.Ignore;
            return reticle;
        }
    }
}
