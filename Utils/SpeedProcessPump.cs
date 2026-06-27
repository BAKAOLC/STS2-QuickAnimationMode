using Godot;

namespace STS2QuickAnimationMode.Utils
{
    public sealed partial class SpeedProcessPump : Node
    {
        public const string NodeName = "STS2QuickAnimationModeSpeedProcessPump";

        public SpeedProcessPump()
        {
            Name = NodeName;
            ProcessMode = ProcessModeEnum.Always;
        }

        public override void _Process(double delta)
        {
            SpeedManager.ProcessFrame(delta);
        }
    }
}
