using UnityEngine;

namespace Klrohias.NFast.UIComponent
{
    public class ScreenAdapter : MonoBehaviour
    {
        private const float ASPECT_RATIO = 16f / 9f;
        private float _scaleFactor = 1f;
        public float GameVirtualResolutionWidth = 625f;
        public float GameVirtualResolutionHeight = 440f;
        public float ViewportSize = 10f;

        private void Awake()
        {
            SetupScreenScale();
        }
        public Vector2 ScaleVector2(Vector2 inputVector2) => inputVector2 * _scaleFactor;

        public Vector3 ScaleVector3(Vector3 inputVector3) =>
            new(inputVector3.x * _scaleFactor, inputVector3.y * _scaleFactor, inputVector3.z);

        public float ToGameXPos(float x) =>
            (x / GameVirtualResolutionWidth) * (ViewportSize / 2) * _scaleFactor * ASPECT_RATIO;

        public float ToGameYPos(float x) => (x / GameVirtualResolutionHeight) * (ViewportSize / 2) * _scaleFactor;
        private void SetupScreenScale()
        {
            var safeArea = Screen.safeArea;
            var aspectRatio = safeArea.width / safeArea.height;
            if (aspectRatio < ASPECT_RATIO)
            {
                _scaleFactor = aspectRatio / ASPECT_RATIO;
            }
        }
    }
}