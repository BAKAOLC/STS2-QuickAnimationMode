using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2QuickAnimationMode.Utils;

namespace STS2QuickAnimationMode.Patches
{
    /// <summary>
    ///     A paginator for selecting game speed multiplier.
    ///     Follows the same pattern as NScreenshakePaginator.
    /// </summary>
    public partial class NSpeedPaginator : NPaginator
    {
        public override void _Ready()
        {
            ConnectSignals();

            for (var index = 0; index < SpeedManager.SpeedOptionCount; index++)
                _options.Add(SpeedManager.GetSpeedLabel(index));

            SetFromSettings();
        }

        public void SetFromSettings()
        {
            _currentIndex = SpeedManager.CurrentIndex;
            _label.SetTextAutoSize(_options[_currentIndex]);
        }

        protected override void OnIndexChanged(int index)
        {
            _currentIndex = index;
            _label.SetTextAutoSize(_options[index]);
            SpeedManager.SetSpeedIndex(index);
        }
    }
}
