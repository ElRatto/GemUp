using System.Windows.Forms;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace GemUp
{
    public class GemUpSettings : ISettings
    {
        public GemUpSettings()
        {
            Enable = new ToggleNode(false);
            PickUpKey = Keys.F1;
            ExtraDelay = new RangeNode<int>(0, 0, 200);
            MouseSpeed = new RangeNode<float>(1, 0, 30);
        }

        public ToggleNode Enable { get; set; }
        public HotkeyNode PickUpKey { get; set; }
        public RangeNode<int> ExtraDelay { get; set; }
        public RangeNode<float> MouseSpeed { get; set; }
        public ToggleNode ReturnMouseToBeforeClickPosition { get; set; } = new ToggleNode(true);
        public RangeNode<int> TimeBeforeNewClick { get; set; } = new RangeNode<int>(500, 0, 1500);
    }
}
