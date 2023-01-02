using UnityEngine;

namespace Klrohias.NFast.UIComponent
{
    public class ScreenAdapter : MonoBehaviour
    {
        public const float TARGET_ASPECT_RATIO = 16f / 9f;
        private float _scaleFactor = 1f;
        public float ScaleFactor => _scaleFactor;
        private Resolution _resolution;
        public float GameVirtualResolutionWidth = 625f;
        public float GameVirtualResolutionHeight = 440f;
        public float ViewportSize = 10f;
        public Transform[] AutoScaleTransforms = new Transform[0];
        public Camera[] ViewportCameras = new Camera[0];
        private void Awake()
        {
            _resolution = Screen.currentResolution;
            SetupScreenScale();
            foreach (var objTransform in AutoScaleTransforms)
            {
                objTransform.localScale = ScaleVector3(objTransform.localScale);
            }

            foreach (var viewportCamera in ViewportCameras)
            {
                SetupCamera(viewportCamera);
            }
        }

        private void SetupCamera(Camera camera)
        {
            var viewportRect = camera.rect;
            viewportRect.height *= _scaleFactor;
            camera.orthographicSize *= _scaleFactor;
            camera.rect = viewportRect;
        }

        public Vector2 ScaleVector2(Vector2 inputVector2) => inputVector2 * _scaleFactor;

        public Vector3 ScaleVector3(Vector3 inputVector3) =>
            new(inputVector3.x * _scaleFactor, inputVector3.y * _scaleFactor, inputVector3.z);

        public float ToGameXPos(float x) =>
            (x / GameVirtualResolutionWidth) * (ViewportSize / 2) * _scaleFactor * TARGET_ASPECT_RATIO;

        public float ToGameYPos(float x) => (x / GameVirtualResolutionHeight) * (ViewportSize / 2) * _scaleFactor;
        private void SetupScreenScale()
        {
            var safeArea = Screen.safeArea;
            var aspectRatio = safeArea.width / safeArea.height;
            if (aspectRatio < TARGET_ASPECT_RATIO)
            {
                _scaleFactor = aspectRatio / TARGET_ASPECT_RATIO;
            }
        }
    }
}