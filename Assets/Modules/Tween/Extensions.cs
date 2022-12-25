using System.Threading.Tasks;
using Klrohias.NFast.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.Tween
{
    public static class Extensions
    {
        public static Task NTweenAlpha(this Graphic graphic, float lastTime, EasingFunction easingFunction, float from, float to)
        {
            return Tweener.Get().RunTween(lastTime, (value) =>
            {
                var color = graphic.color;
                color.a = value;
                graphic.color = color;
            }, easingFunction, from, to);
        }

        public static Task NTweenColor(this Graphic graphic, float lastTime, EasingFunction easingFunction, Color from,
            Color to)
        {
            var rStep = to.r - from.r;
            var gStep = to.g - from.g;
            var bStep = to.b - from.b;
            return Tweener.Get().RunTween(lastTime, (value) =>
            {
                graphic.color = new Color(from.r + rStep * value, from.g + gStep * value, from.b + bStep * value,
                    graphic.color.a);
            }, easingFunction);
        }
    }
}