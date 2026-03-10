using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2QuickAnimationMode.Utils;

namespace STS2QuickAnimationMode.Patches
{
    /// <summary>
    ///     A paginator for toggling progressive acceleration on/off.
    /// </summary>
    public partial class NProgressiveTogglePaginator : NPaginator
    {
        private static readonly string[] ToggleLabels = ["OFF", "ON"];

        public override void _Ready()
        {
            ConnectSignals();

            foreach (var label in ToggleLabels)
                _options.Add(label);

            SetFromSettings();
        }

        public void SetFromSettings()
        {
            _currentIndex = SpeedManager.ProgressiveEnabled ? 1 : 0;
            _label.SetTextAutoSize(_options[_currentIndex]);
        }

        protected override void OnIndexChanged(int index)
        {
            _currentIndex = index;
            _label.SetTextAutoSize(_options[index]);
            SpeedManager.SetProgressiveEnabled(index == 1);
        }
    }
}
