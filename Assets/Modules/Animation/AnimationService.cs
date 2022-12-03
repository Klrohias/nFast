using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationService : Service<AnimationService>
{
    private List<AnimationItem> animations = new();
    private List<AnimationItem.PropertyTransition> propertyTransitions = new(128);
    public Func<double> TimeFunc = () => AudioSettings.dspTime;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        foreach (var propertyTransition in propertyTransitions)
        {
            
        }
    }

    public class AnimationItemBuilder
    {
        private AnimationItem item;
        public AnimationItem Build() => item;

        public void Begin()
        {
            if (item.TargetObjects.Count == 0)
            {
                Debug.LogWarning("Try to start a animation with no objects");
                return;
            }
            AnimationService.Get().AddAnimation(item);
        }

        public AnimationItemBuilder WithObject(object obj)
        {
            item.TargetObjects.Add(obj);
            return this;
        }

        public AnimationItemBuilder WithObjects(IEnumerable<object> objs)
        {
            item.TargetObjects.AddRange(objs);
            return this;
        }

        private void trySetMaxAnimationTime(float time)
            => item.MaxAnimationTime = MathF.Max(time, item.MaxAnimationTime);

        public AnimationItemBuilder WithProperty(string propertyName, object beginValue, object endValue, float time,
            Func<float, float> easingFunc)
        {
            trySetMaxAnimationTime(time);
            item.PropertyTransitions.Add(new()
            {
                Name = propertyName,
                BeginValue = beginValue,
                EndValue = endValue,
                AnimationTime = Convert.ToDouble(time),
                EasingFunc = easingFunc
            });
            return this;
        }

        public AnimationItemBuilder WithProperty(string propertyName, object endValue, float time,
            Func<float, float> easingFunc)
        {
            trySetMaxAnimationTime(time);
            item.PropertyTransitions.Add(new()
            {
                Name = propertyName,
                EndValue = endValue,
                AnimationTime = Convert.ToDouble(time),
                EasingFunc = easingFunc
            });
            return this;
        }
    }

    public class AnimationItem
    {
        public class PropertyTransition
        {
            public string Name;
            public object BeginValue;
            public object EndValue;
            internal double BeginTime;
            public double AnimationTime;
            public Func<float, float> EasingFunc;
            public PropertyTransition Clone() => (PropertyTransition) MemberwiseClone();
            internal AnimationItem AnimationItem;
        }
        public List<object> TargetObjects = new();
        public List<PropertyTransition> PropertyTransitions = new();
        public float MaxAnimationTime = 0f;
    }

    public AnimationItemBuilder Prepare() => new();

    public void AddAnimation(AnimationItem item)
    {
        this.animations.Add(item);
        foreach (var propertyTransition in item.PropertyTransitions)
        {
            var obj = propertyTransition.Clone();
            obj.AnimationItem = item;
            propertyTransitions.Add(obj);
        }
    }
}
