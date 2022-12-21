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

        private void Awake()
        {
            var toggle = GetComponent<Toggle>();
            foreach (Graphic graphic in graphics)
                graphic.canvasRenderer.SetColor(toggle.isOn ? onColor : offColor);
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        private void OnToggleValueChanged(bool isOn)
        {
            foreach (Graphic graphic in graphics)
                graphic.CrossFadeColor(isOn ? onColor : offColor, duration, true, true);
        }
    }
}
