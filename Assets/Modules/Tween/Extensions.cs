using System;
using System.Threading.Tasks;
using Klrohias.NFast.Utilities;
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

        public static Task NTweenAlpha(this CanvasGroup canvasGroup, float lastTime, EasingFunction easingFunction, float from, float to)
        {
            return Tweener.Get().RunTween(lastTime, value => canvasGroup.alpha = value, easingFunction, from, to);
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

        public static Tweener.Tween Last(this Tweener.Tween tween, float lastTime)
        {
            var tweener = Tweener.Get();
            var time = tweener.Timer.Time;
            tween.BeginTime = time;
            tween.EndTime = time + lastTime;
            return tween;
        }

        public static Tweener.Tween From(this Tweener.Tween tween, float val)
        {
            tween.BeginValue = val;
            return tween;
        }

        public static Tweener.Tween To(this Tweener.Tween tween, float val)
        {
            tween.EndValue = val;
            return tween;
        }

        public static Tweener.Tween Easing(this Tweener.Tween tween, EasingFunction val)
        {
            tween.EasingFunction = val;
            return tween;
        }

        public static Tweener.Tween OnUpdate(this Tweener.Tween tween, Tweener.TweenAction action)
        {
            tween.OnUpdate += action;
            return tween;
        }

        public static Tweener.Tween OnFinish(this Tweener.Tween tween, Action action)
        {
            tween.OnFinish += action;
            return tween;
        }

        public static Task Run(this Tweener.Tween tween)
        {
            return Tweener.Get().RunTween(tween);
        }

        public static void Stop(this Tweener.Tween tween)
        {
            Tweener.Get().Remove(tween);
        }

        public static Task NTweenRotate(this Transform transform, Vector3 from, Vector3 to, float lastTime = 300f,
            EasingFunction easing = EasingFunction.Linear)
        {
            return Tweener.Get().RunTween(lastTime, value =>
            {
                transform.rotation = Quaternion.Euler(Vector3.Lerp(from, to, value));
            }, easing, 0f, 1f);
        }
    }
}