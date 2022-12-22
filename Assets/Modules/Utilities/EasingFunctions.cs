using System;
using Klrohias.NFast.PhiChartLoader.NFast;

namespace Klrohias.NFast.Utilities
{
    public enum EasingFunction
    {
        Linear,
        SineIn,
        SineOut,
        SineInOut,
        QuadIn,
        QuadOut,
        QuadInOut,
        CubicIn,
        CubicOut,
        CubicInOut,
        QuartIn,
        QuartOut,
        QuartInOut,
        QuintIn,
        QuintOut,
        ExpoIn,
        ExpoOut,
        CircIn,
        CircOut,
        CircInOut,
        BackIn,
        BackOut,
        BackInOut,
        ElasticIn,
        ElasticOut,
        BounceIn,
        BounceOut,
        BounceInOut,
    }
    public static class EasingFunctions
    {
        private static float Lerp(float begin, float end, float val)
            => begin + (end - begin) * val;
        public static float Linear(float x) => x;
        public static float SineIn(float x) => MathF.Sin(Lerp(0, MathF.PI / 2, x));
        public static float SineOut(float x) => MathF.Sin(Lerp(-MathF.PI / 2, 0, x)) + 1f;
        public static float SineInOut(float x) => MathF.Sin(Lerp(-MathF.PI / 2, MathF.PI / 2, x)) * 0.5f + 0.5f;
        public static float QuadIn(float x) => x * x;
        public static float QuadOut(float x) => 1f - MathF.Pow(1f - x, 2);
        public static float QuadInOut(float x) => x < 0.5f ? 2f * QuadIn(x) : 1f - MathF.Pow(-2f * (x - 1f), 2) / 2f;
        public static float CubicIn(float x) => x * x * x;
        public static float CubicOut(float x) => 1 - CubicIn(1 - x);
        public static float Invoke(EasingFunction type, float x, float low = 0f, float high = 1f)
        {
            var val = Lerp(low, high, x);
            switch (type)
            {
                case EasingFunction.Linear: return Linear(val);
                case EasingFunction.SineIn: return SineIn(val);
                case EasingFunction.SineOut: return SineOut(val);
                case EasingFunction.SineInOut: return SineInOut(val);
                case EasingFunction.QuadIn: return QuadIn(val);
                case EasingFunction.QuadOut: return QuadOut(val);
                case EasingFunction.QuadInOut: return QuadInOut(val);
                case EasingFunction.CubicIn: return CubicIn(val);
                case EasingFunction.CubicOut: return CubicOut(val);
            }
            return Linear(val);
        }
    }
}