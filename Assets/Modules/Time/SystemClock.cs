using System;
using System.Runtime.InteropServices;
using UnityEngine;


namespace Klrohias.NFast.Time
{
    public class SystemClock : IClock
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

        private float RawTime => (float)(queryCounter()) / freq * 1000f;
#else
        private float RawTime => UnityEngine.Time.fixedTime * 1000f;
#endif

#else
        private float RawTime => Convert.ToSingle(AudioSettings.dspTime);
#endif
        private float _startTime = 0f;
        private bool _paused = false;
        private float _pauseStart = 0;
        
        public float Time => RawTime - _startTime - offset;
        public void Reset()
        {
            _startTime = RawTime;
        }
        public void Pause()
        {
            if (_paused) throw new InvalidOperationException("Clock is already paused");
            _paused = true;
            _pauseStart = RawTime;
        }

        public void Resume()
        {
            if (!_paused) throw new InvalidOperationException("Clock is not paused");
            _paused = false;
            var pauseEnd = RawTime;
            offset += pauseEnd - _pauseStart;
        }
    }
}