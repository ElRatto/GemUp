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
            GemUpKey = Keys.F1;
            MouseSpeed = new RangeNode<float>(1, 0, 30);
            ExtraDelay = new RangeNode<int>(0, 0, 200);


            
            
        }

        public ToggleNode Enable { get; set; }
        public HotkeyNode GemUpKey { get; set; }
        public RangeNode<float> MouseSpeed { get; set; }
        public RangeNode<int> ExtraDelay { get; set; }

    }
}
