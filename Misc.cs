using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using SharpDX;
using ExileCore.Shared.Helpers;

namespace GemUp
{
    public static class Misc
    {
        public static float EntityDistance(Entity entity, Entity player)
        {
            var component = entity?.GetComponent<Render>();

            if (component == null)
                return 9999999f;

            var objectPosition = component.Pos;

            return Vector3.Distance(objectPosition,
                player.GetComponent<Render>().Pos);
        }

        public static Vector2 GetClickPos(RectangleF rectangleF)
        {
            const int paddingPixels = 3;
            var x = MathHepler.Randomizer.Next(
                (int) rectangleF.TopLeft.X + paddingPixels,
                (int) rectangleF.TopRight.X - paddingPixels);
            var y = MathHepler.Randomizer.Next(
                (int) rectangleF.TopLeft.Y + paddingPixels,
                (int) rectangleF.BottomLeft.Y - paddingPixels);
            return new Vector2(x, y);
        }

        public static Vector2 RandomizePos(Vector2 oldPos)
        {
            var x = MathHepler.Randomizer.Next((int) oldPos.X - 50,
                (int) oldPos.X + 50);
            var y = MathHepler.Randomizer.Next((int) oldPos.Y - 50,
                (int) oldPos.Y + 50);
            return new Vector2(x, y);
        }
    }
}