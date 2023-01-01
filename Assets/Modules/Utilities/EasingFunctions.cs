using System;
using Klrohias.NFast.PhiChartLoader;
using UnityEditor;

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
        QuintInOut,
        ExpoIn,
        ExpoOut,
        ExpoInOut,
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
        public static float QuadInOut(float x) => x < 0.5f ? 0.5f * QuadIn(x / 0.5f) : 0.5f + QuadOut((x - 0.5f) / 0.5f) * 0.5f;
        public static float CubicIn(float x) => x * x * x;
        public static float CubicOut(float x) => 1f - CubicIn(1f - x);
        public static float CubicInOut(float x) =>
            x < 0.5f ? 0.5f * CubicIn(x / 0.5f) : 0.5f + CubicOut((x - 0.5f) / 0.5f) * 0.5f;
        public static float QuartIn(float x) => x * x * x * x;
        public static float QuartOut(float x) => 1f - QuartIn(1f - x);
        public static float QuartInOut(float x) =>
            x < 0.5f ? QuartIn(x / 0.5f) * 0.5f : 0.5f + QuartOut((x - 0.5f) / 0.5f) * 0.5f;
        public static float QuintIn(float x) => x * x * x * x * x;
        public static float QuintOut(float x) => 1f - QuintIn(1f - x);
        public static float QuintInOut(float x) =>
            x < 0.5f ? QuintIn(x / 0.5f) * 0.5f : QuintOut((x - 0.5f) / 0.5f) * 0.5f + 0.5f;
        public static float ExpoIn(float x) => x == 0f ? 0f : MathF.Pow(2f, 10 * (x - 1f));
        public static float ExpoOut(float x) => 1f - ExpoIn(1f - x);

        public static float ExpoInOut(float x) =>
            x < 0.5f ? ExpoIn(x / 0.5f) * 0.5f : 0.5f + 0.5f * ExpoOut((x - 0.5f) / 0.5f);
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
                case EasingFunction.CubicInOut: return CubicInOut(val);
                case EasingFunction.QuartIn: return QuartIn(val);
                case EasingFunction.QuartOut: return QuartOut(val);
                case EasingFunction.QuartInOut: return QuartInOut(val);
                case EasingFunction.QuintIn: return QuintIn(val);
                case EasingFunction.QuintOut: return QuintOut(val);
                case EasingFunction.QuintInOut: return QuintInOut(val);
                case EasingFunction.ExpoIn: return ExpoIn(val);
                case EasingFunction.ExpoOut: return ExpoOut(val);
                case EasingFunction.ExpoInOut: return ExpoInOut(val);
            }
            return Linear(val);
        }
    }
}