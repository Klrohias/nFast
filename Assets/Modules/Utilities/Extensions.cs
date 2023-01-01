using System;
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

        private enum LogLevel
        {
            Normal,
            Warning,
            Error
        }
        private static void LogInternal(LogLevel level, object content)
        {
            switch (level)
            {
                case LogLevel.Normal:
                    Debug.Log(content);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(content);
                    break;
                case LogLevel.Error:
                    Debug.LogError(content);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
        public static void Log(this object self)
        {
            LogInternal(LogLevel.Normal, self);
        }

        public static void LogWarning(this object self)
        {
            LogInternal(LogLevel.Warning, self);
        }

        public static void LogError(this object self)
        {
            LogInternal(LogLevel.Error, self);
        }
    }
}