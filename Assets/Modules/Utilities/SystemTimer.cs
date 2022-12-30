using System;
using System.Runtime.InteropServices;
using UnityEngine;


namespace Klrohias.NFast.Utilities
{
    public class SystemTimer
    {
        private float offset = 0f;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        // It is not recommend to enable system clock, because
        // there is some problem with it.
        // In some of frames, it give out same value, it make the FPS of notes low.
#if ENABLE_SYSTEM_CLOCK
        private readonly long freq = 0;
        // p/invokes
        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long perf);
        [DllImport("kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long freq);
        public SystemTimer()
        {
            if (!QueryPerformanceFrequency(out freq)) throw new Exception("failed to query freq");
        }

        private long queryCounter()
        {
            QueryPerformanceCounter(out var counter);
            return counter;
        }

        private float rawTime => (float)(queryCounter()) / freq * 1000f;
#else
        private float rawTime => UnityEngine.Time.fixedTime * 1000f;
#endif

#else
        private float rawTime => Convert.ToSingle(AudioSettings.dspTime);
#endif
        private float startTime = 0f;
        private bool paused = false;
        private float pauseStart = 0;
#if UNITY_EDITOR
        public float Time => (rawTime - startTime - offset);
#else
        public float Time => rawTime - startTime - offset;
#endif
        public void Reset()
        {
            startTime = rawTime;
        }
        public void Pause()
        {
            if (paused) throw new InvalidOperationException("Timer is already paused");
            paused = true;
            pauseStart = rawTime;
        }

        public void Resume()
        {
            if (!paused) throw new InvalidOperationException("Timer is not paused");
            paused = false;
            var pauseEnd = rawTime;
            offset += pauseEnd - pauseStart;
        }
    }
}