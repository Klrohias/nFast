using System;
using Klrohias.NFast.Time;
using Klrohias.NFast.Tween;
using Klrohias.NFast.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.UIControllers
{
    public class ToastService : Service<ToastService>
    {
        public CanvasGroup ToastCanvas;
        public Sprite SuccessSprite;
        public Sprite FailureSprite;
        public Color SuccessColor;
        public Color FailureColor;
        public Image SignImage;
        public TMP_Text ContentText;
        private bool _isOpened = false;
        private IClock _timer = new SystemClock();
        private float _hideTime = float.PositiveInfinity;
        public enum ToastType
        {
            Success,
            Failure,
        }

        private void Start()
        {
            ToastCanvas.SetDisplay(false);
        }

        private void SetupToastType(ToastType type)
        {
            Color color = Color.black;
            Sprite sprite = null;
            switch (type)
            {
                case ToastType.Success:
                    color = SuccessColor;
                    sprite = SuccessSprite;
                    break;
                case ToastType.Failure:
                    color = FailureColor;
                    sprite = FailureSprite;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }

            SignImage.sprite = sprite;
            SignImage.color = color;
        }
        public async void Show(ToastType type, string content)
        {
            SetupToastType(type);
            ContentText.text = content;

            if (_isOpened) return;
            _isOpened = true;

            ToastCanvas.SetDisplay(true);
            await ToastCanvas.NTweenAlpha(200f, EasingFunction.SineOut, 0f, 1f);
            SchedulerHide();
        }

        private async void Hide()
        {
            if (!_isOpened) return;
            _isOpened = false;

            await ToastCanvas.NTweenAlpha(200f, EasingFunction.SineIn, 1f, 0f);
            ToastCanvas.SetDisplay(false);
        }
        private void SchedulerHide()
        {
            _hideTime = _timer.Time + 3000f;
        }

        private void Update()
        {
            if (_timer.Time > _hideTime)
            {
                _hideTime = float.PositiveInfinity;
                Hide();
            }
        }
    }
}
