namespace Klrohias.NFast.Utilities
{
    public class BeatsTimer
    {
        private SystemTimer _timerSource;
        private float _offsetTime = 0f; // unit: ms
        
        private float _offsetBeats = 0f;
        private float _beatLasts = 0f; // unit: ms
        public float CurrentBpm { get; private set; }
        private float _lastBeginBeats = 0f;

        private float _lastBeats = 0f;
        public float Beats
        {
            get
            {
                var result = _offsetBeats + (_timerSource.Time - _offsetTime) / _beatLasts;
                if (result < _lastBeats)
                {
                    $"beats cannot keep up, difference {_lastBeats - result}".LogWarning();
                    return _lastBeats;
                }
                _lastBeats = result;
                return result;
            }
        }

        public BeatsTimer(SystemTimer timerSource)
        {
            _timerSource = timerSource;
        }

        public void Reset()
        {
            _offsetTime = _timerSource.Time;
        }

        public void ApplyNewBpm(float bpm, float beginBeats)
        {
            var appendBeatsOffset = beginBeats - _lastBeginBeats;
            
            _offsetBeats += appendBeatsOffset;
            _offsetTime += appendBeatsOffset * _beatLasts;

            _lastBeginBeats = beginBeats;
            CurrentBpm = bpm;
            _beatLasts = 60f / CurrentBpm * 1000f;
        }
    }
}