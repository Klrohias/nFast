using System;
using Klrohias.NFast.Utilities;
using UnityEngine;

namespace Klrohias.NFast.Time
{
    public class DspClock : IClock
    {
        public float Time
        {
            get
            {
                var result = _source.Time - _startTime - _offsetTime;
                return Convert.ToSingle(result) * 1000f;
            }
        }
        private double _startTime = 0;
        private double _offsetTime = 0;
        private readonly DspTimeSource _source;

        private bool _paused = false;
        private double _pauseStart = 0;
        public void Reset()
        {
            _startTime = _source.Time;
        }

        public void Pause()
        {
            if (_paused) throw new InvalidOperationException("Clock is already paused");
            _paused = true;
            _pauseStart = _source.Time;
        }

        public void Resume()
        {
            if (!_paused) throw new InvalidOperationException("Clock is not paused");
            _paused = false;
            _offsetTime += _source.Time - _pauseStart;
        }
        
        public DspClock()
        {
            DspTimeSource.TryCreate();
            _source = DspTimeSource.Instance;
        }

        private class DspTimeSource : MonoBehaviour
        {
            public static DspTimeSource Instance = null;
            public static void TryCreate()
            {
                if (Instance != null) return;
                var gameObject = new GameObject("DspTimeSource");
                gameObject.AddComponent<DspTimeSource>();
            }

            private double _lastDspTime;
            public double Time;
            private void Awake()
            {
                UpdateTime();
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }

            private void UpdateTime()
            {
                var currentDspTime = AudioSettings.dspTime;
                if (currentDspTime != _lastDspTime)
                {
                    _lastDspTime = currentDspTime;
                    Time = currentDspTime;
                    return;
                }

                // Avoid the same DspTime
                Time += UnityEngine.Time.deltaTime;
            }

            private void Update() => UpdateTime();
        }
    }
}