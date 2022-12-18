using System;
using Klrohias.NFast.ChartLoader.NFast;

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

        public static float Invoke(EasingFunction type, float x, float low = 0f, float high = 1f)
        {
            var val = Lerp(low, high, x);
            switch (type)
            {
                case EasingFunction.Linear: return Linear(val);
                case EasingFunction.SineIn: return SineIn(val);
                case EasingFunction.SineOut: return SineOut(val);
                case EasingFunction.SineInOut: return SineInOut(val);
                case EasingFunction.QuadIn:
                    break;
                case EasingFunction.QuadOut:
                    break;
                case EasingFunction.QuadInOut:
                    break;
                case EasingFunction.CubicIn:
                    break;
                case EasingFunction.CubicOut:
                    break;
                case EasingFunction.CubicInOut:
                    break;
                case EasingFunction.QuartIn:
                    break;
                case EasingFunction.QuartOut:
                    break;
                case EasingFunction.QuartInOut:
                    break;
                case EasingFunction.QuintIn:
                    break;
                case EasingFunction.QuintOut:
                    break;
                case EasingFunction.ExpoIn:
                    break;
                case EasingFunction.ExpoOut:
                    break;
                case EasingFunction.CircIn:
                    break;
                case EasingFunction.CircOut:
                    break;
                case EasingFunction.CircInOut:
                    break;
                case EasingFunction.BackIn:
                    break;
                case EasingFunction.BackOut:
                    break;
                case EasingFunction.BackInOut:
                    break;
                case EasingFunction.ElasticIn:
                    break;
                case EasingFunction.ElasticOut:
                    break;
                case EasingFunction.BounceIn:
                    break;
                case EasingFunction.BounceOut:
                    break;
                case EasingFunction.BounceInOut:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            return Linear(val);
        }
    }
}