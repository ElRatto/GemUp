using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;
using ExileCore.Shared.Helpers;

namespace GemUp
{
    public class Misc
    {
        public static float EntityDistance(Entity entity, Entity player)
        {
            var component = entity?.GetComponent<Render>();

            if (component == null)
                return 9999999f;

            var objectPosition = component.Pos;

            return Vector3.Distance(objectPosition, player.GetComponent<Render>().Pos);
        }

        public static Vector2 GetClickPos(RectangleF viereck)
        {
            var paddingPixels = 3;
            var x = MathHepler.Randomizer.Next((int)viereck.TopLeft.X + paddingPixels, (int)viereck.TopRight.X - paddingPixels);
            var y = MathHepler.Randomizer.Next((int)viereck.TopLeft.Y + paddingPixels, (int)viereck.BottomLeft.Y - paddingPixels);
            return new Vector2(x, y);
        }
    }
}
