using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2QuickAnimationMode.Utils;

namespace STS2QuickAnimationMode.Patches
{
    /// <summary>
    ///     A paginator for selecting the time threshold for acceleration.
    /// </summary>
    public partial class NTimeThresholdPaginator : NPaginator
    {
        public override void _Ready()
        {
            ConnectSignals();

            foreach (var label in SpeedManager.TimeThresholdLabels)
                _options.Add(label);

            SetFromSettings();
        }

        public void SetFromSettings()
        {
            _currentIndex = SpeedManager.TimeThresholdIndex;
            _label.SetTextAutoSize(_options[_currentIndex]);
        }

        protected override void OnIndexChanged(int index)
        {
            _currentIndex = index;
            _label.SetTextAutoSize(_options[index]);
            SpeedManager.SetTimeThresholdIndex(index);
        }
    }
}
