using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2QuickAnimationMode.Utils;

namespace STS2QuickAnimationMode.Patches
{
    public partial class NTransitionDurationPaginator : NPaginator
    {
        public override void _Ready()
        {
            ConnectSignals();

            foreach (var label in SpeedManager.TransitionDurationLabels)
                _options.Add(label);

            SetFromSettings();
        }

        public void SetFromSettings()
        {
            _currentIndex = SpeedManager.TransitionDurationIndex;
            _label.SetTextAutoSize(_options[_currentIndex]);
        }

        protected override void OnIndexChanged(int index)
        {
            _currentIndex = index;
            _label.SetTextAutoSize(_options[index]);
            SpeedManager.SetTransitionDurationIndex(index);
        }
    }
}
