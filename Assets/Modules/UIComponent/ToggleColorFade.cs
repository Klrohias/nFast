using Klrohias.NFast.Tween;
using UnityEngine;
using UnityEngine.UI;

namespace Klrohias.NFast.Utilities
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleColorFade : MonoBehaviour
    {
        [SerializeField] private Graphic[] graphics;
        [SerializeField] private Color offColor;
        [SerializeField] private Color onColor;
        [SerializeField] private float duration;

        private void Start()
        {
            var toggle = GetComponent<Toggle>();
            OnToggleValueChanged(toggle.isOn);
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        private void OnToggleValueChanged(bool isOn)
        {
            foreach (Graphic graphic in graphics)
                graphic.NTweenColor(duration * 1000f, EasingFunction.SineIn
                    , graphic.color, isOn ? onColor : offColor);
        }
    }
}
