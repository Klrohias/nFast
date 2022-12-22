using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Klrohias.NFast.Utilities;
using UnityEngine;

namespace Klrohias.NFast.Tween
{
    public class Tweener : Service<Tweener>
    {
        public delegate void TweenAction(float value);
        public class Tween
        {
            public event TweenAction OnUpdate;
            public event Action OnFinish;

            public float BeginTime;
            public float EndTime;
            public float BeginValue;
            public float EndValue;
            public EasingFunction EasingFunction;

            internal void Update(float value) => OnUpdate?.Invoke(value);
            internal void Finish() => OnFinish?.Invoke();
        }

        private UnorderedList<Tween> _tweens = new();
        private SystemTimer _timer = new SystemTimer();
        private void Awake()
        {
            _timer.Reset();
        }

        private void Update()
        {
            var time = _timer.Time;
            for (int i = 0; i < _tweens.Length; i++)
            {
                var tween = _tweens[i];
                if(tween.BeginTime> time) continue;
                if (time >= tween.EndTime)
                {
                    tween.Update(tween.EndValue);
                    tween.Finish();
                    _tweens.Remove(tween);
                    continue;
                }

                var easingX = (time - tween.BeginTime) / (tween.EndTime - tween.BeginTime);
                var value = tween.BeginValue + EasingFunctions.Invoke(tween.EasingFunction,
                    easingX) * (tween.EndValue - tween.BeginValue);
                tween.Update(value);
            }
        }

        public void AddTween(Tween tween) => _tweens.Add(tween);
        public Task RunTween(Tween tween)
        {
            var task = new TaskCompletionSource<bool>();
            tween.OnFinish += () => task.TrySetResult(true);
            AddTween(tween);
            return task.Task;
        }

        public Task RunTween(float lastTime, TweenAction action, 
            EasingFunction easingFunction = EasingFunction.Linear,
            float beginValue = 0f,
            float endValue = 1f)
        {
            var time = _timer.Time;
            var tween = new Tween()
            {
                BeginTime = time,
                EndTime = time + lastTime,
                BeginValue = beginValue,
                EndValue = endValue,
                EasingFunction = easingFunction
            };
            tween.OnUpdate += action;
            return RunTween(tween);
        }

        public float GetCurrentTime() => _timer.Time;
    }
}
