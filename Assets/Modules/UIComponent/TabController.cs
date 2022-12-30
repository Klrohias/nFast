using System;
using Klrohias.NFast.Tween;
using Klrohias.NFast.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIComponent
{
    public class TabController  : MonoBehaviour
    {
        public TabGroup[] Groups;
        public int Current = 0;

        public float Duration = 300f;
        public EasingFunction EasingFunction = EasingFunction.SineIn;
        [Serializable]
        public class TabGroup
        {
            public Toggle TabButton;
            public CanvasGroup View;
        }
        
        private void Start()
        {
            UpdateTab(true);
            for (int i = 0; i < Groups.Length; i++)
            {
                var index = i;
                var item = Groups[i];
                item.TabButton.onValueChanged.AddListener(isOn =>
                {
                    Current = index;
                    UpdateTab();
                });
            }
        }

        private async void UpdateTab(bool noAnimation = false)
        {
            for (var i = 0; i < Groups.Length; i++)
            {
                var item = Groups[i];

                if (item.View == null) continue;
                var isCurrent = i == Current;
                if (Duration != 0f && !noAnimation)
                {
                    var beginAlpha = item.View.alpha;
                    if (beginAlpha == 0f && !isCurrent) continue;
                    await item.View.NTweenAlpha(Duration, EasingFunction
                        , beginAlpha, isCurrent ? 1f : 0f);
                }

                item.View.SetDisplay(isCurrent);
            }
        }
    }
}