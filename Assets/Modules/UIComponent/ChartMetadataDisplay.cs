using System;
using System.Threading.Tasks;
using Klrohias.NFast.Tween;
using Klrohias.NFast.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIComponent
{
    public class ChartMetadataDisplay : MonoBehaviour
    {
        public Image LoadingMask;
        public TMP_Text MetadataText;
        public float AnimationLast = 300f;
        public float DisplayLast = 1500;
        public async Task Display(string text)
        {
            MetadataText.text = text;

            await MetadataText.NTweenAlpha(AnimationLast, EasingFunction.SineIn, 0f, 1f);
            await Task.Delay(Convert.ToInt32(DisplayLast));
            await Task.WhenAll(MetadataText.NTweenAlpha(AnimationLast, EasingFunction.SineOut, 1f, 0f),
                LoadingMask.NTweenAlpha(AnimationLast, EasingFunction.SineOut, 1f, 0f));
            LoadingMask.gameObject.SetActive(false);
        }
    }
}