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
            ExtraDelay = new RangeNode<int>(110, 0, 200);
            MouseSpeed = new RangeNode<float>(1, 0, 30);
            IdleGemUp = new ToggleNode(false);
            CheckEveryXTick = new RangeNode<int>(10, 0, 60);
        }

        public ToggleNode Enable { get; set; }
        public HotkeyNode PickUpKey { get; set; }
        public RangeNode<int> ExtraDelay { get; set; }
        public RangeNode<float> MouseSpeed { get; set; }

        public ToggleNode ReturnMouseToBeforeClickPosition { get; set; } =
            new ToggleNode(true);

        public RangeNode<int> TimeBeforeNewClick { get; set; } =
            new RangeNode<int>(500, 0, 1500);

        public ToggleNode IdleGemUp { get; set; }
        public RangeNode<int> CheckEveryXTick { get; set; }
    }
}