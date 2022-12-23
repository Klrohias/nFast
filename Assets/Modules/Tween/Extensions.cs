using System.Threading.Tasks;
using Klrohias.NFast.Utilities;
using TMPro;
using UnityEngine.UI;

namespace Klrohias.NFast.Tween
{
    public static class Extensions
    {
        public static Task NTweenAlpha(this TMP_Text text, float lastTime, EasingFunction easingFunction, float from, float to)
        {
            return Tweener.Get().RunTween(lastTime, (value) =>
            {
                var color = text.color;
                color.a = value;
                text.color = color;
            }, easingFunction, from, to);
        }

        public static Task NTweenAlpha(this Image image, float lastTime, EasingFunction easingFunction, float from,
            float to)
        {
            return Tweener.Get().RunTween(lastTime, (value) =>
            {
                var color = image.color;
                color.a = value;
                image.color = color;
            }, easingFunction, from, to);
        }
    }
}