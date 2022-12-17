using System;

namespace Klrohias.NFast.GamePlay
{
    public static class EasingFunctions
    {
        private static float Lerp(float begin, float end, float val)
            => begin + (end - begin) * val;
        public static float Linear(float x) => x;
        public static float SineIn(float x) => MathF.Sin(Lerp(0, MathF.PI / 2, x));
        public static float SineOut(float x) => MathF.Sin(Lerp(-MathF.PI / 2, 0, x));
        public static float SineInOut(float x) => MathF.Sin(Lerp(-MathF.PI / 2, MathF.PI / 2, x));
    }
}