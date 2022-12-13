using System;
using System.Runtime.InteropServices;

namespace Klrohias.NFast.GamePlay
{
    /// <summary>
    /// High precision relative time timer
    /// </summary>
    public class SystemTimer
    {
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
            QueryPerformanceCounter(out start);
        }

        public float Time
        {
            get
            {
                QueryPerformanceCounter(out var end);
                return (float) (end - start) / freq * 1000f;
            }
        }
#elif UNITY_ANDROID

#endif
    }
}