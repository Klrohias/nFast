using UnityEngine;

namespace Klrohias.NFast.Utilities
{
    public static class Extensions
    {
        public static void SetDisplay(this CanvasGroup group, bool display)
        {
            group.alpha = display ? 1f : 0f;
            group.blocksRaycasts = display;
            group.interactable = display;
        }


        private static void LogInternal(object content)
        {
            Debug.Log(content);
        }
        public static void Log(this object self)
        {
            LogInternal(self);
        }
    }
}