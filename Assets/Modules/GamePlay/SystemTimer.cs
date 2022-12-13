using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Klrohias.NFast.GamePlay
{
    /// <summary>
    /// High precision relative time timer
    /// </summary>
    public class SystemTimer
    {
        private float offset = 0f;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        private long freq = 0, start = 0;
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
            start = queryCounter();
        }

        private long queryCounter()
        {
            QueryPerformanceCounter(out var counter);
            return counter;
        }

        private float rawTime
        {
            get
            {
                var end = queryCounter();
                return (float)(end - start) / freq * 1000f;
            }
        }

#else
        private float rawTime => Convert.ToSingle(AudioSettings.dspTime);
#endif
        private bool paused = false;
        private float pauseStart = 0;
        public float Time => rawTime - offset;
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