using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.Utilities;
using UnityEngine;
using Klrohias.NFast.Native;
using Klrohias.NFast.UIComponent;
using Klrohias.NFast.Resource;
using Debug = UnityEngine.Debug;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiGamePlayer : MonoBehaviour
    {
        internal bool GameRunning { get; private set; } = false;
        internal SystemTimer Timer { get; private set; } = null;

        // game running
        private int _currentBeatCount = 0;
        private int _lastBeatCount = -1;
        public float CurrentBeats { get; private set; } = 0f;
        private KeyValuePair<float, float> _nextBpmEvent;
        private float _currentBpm = 0f;
        private float _beatLast = 0f;
        private IEnumerator<KeyValuePair<float, float>> _bpmEventGenerator;
        private readonly ThreadDispatcher _dispatcher = new ThreadDispatcher();
        private readonly Queue<PhiNote> _newNotes = new Queue<PhiNote>();

        // audio
        private const float FINISH_LENGTH_OFFSET = 3f; // unit: second
        private float _musicLength = 0f; // unit: second

        // events
        private readonly UnorderedList<LineEvent> _runningEvents = new UnorderedList<LineEvent>();
        private IEnumerator<IList<LineEvent>> _eventsGenerator;

        // unity resources
        public Transform BackgroundTransform;
        public SpriteRenderer[] BackgroundImages;
        public GameObject JudgeLinePrefab;
        public GameObject NotePrefab;
        public GameObject HoldNotePrefab;
        public ChartMetadataDisplay MetadataDisplay;
        public ScreenAdapter ScreenAdapter;
        public AudioSource BgmAudioSource;
        public Sprite TapNoteSprite;
        public Sprite DragNoteSprite;
        public Sprite FlickNoteSprite;
        private Texture2D _coverTexture = null;
        private AudioClip _audioClip = null;

        // object pools
        private ObjectPool _linePool;
        private ObjectPool _notePool;
        private ObjectPool _holdNotePool;

        // chart data
        private IResourceProvider _resourceProvider;
        private PhiChart _chart;
        internal IList<PhiLine> Lines;
        internal readonly List<GameObject> LineObjects = new List<GameObject>();
        private readonly List<PhiLineWrapper> _lineWrappers = new List<PhiLineWrapper>();
        private readonly UnorderedList<PhiNote> _notes = new UnorderedList<PhiNote>();

        // judge
        internal readonly UnorderedList<PhiNote> JudgeNotes = new UnorderedList<PhiNote>();
        private float[] _landDistances = null;
        private int _currentMaxJudgeBeats = 0;
        private const int JUDGE_FUTURE_BEATS = 4;

        public enum JudgeResult
        {
            Miss,
            Bad,
            Good,
            Perfect
        }
        async void Start()
        {
            // load chart file
            var loadInstruction = NavigationService.Get().ExtraData as string;
            if (loadInstruction == null) throw new InvalidOperationException("failed to load: unknown");
            
            BackgroundTransform.localScale = ScreenAdapter.ScaleVector3(BackgroundTransform.localScale);

            await LoadChart(loadInstruction);

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", _coverTexture);
            foreach (var backgroundImage in BackgroundImages)
            {
                backgroundImage.SetPropertyBlock(block);
            }

            GameBegin();
        }
        private void Update()
        {
            if (GameRunning) GameTick();
        }

        private void OnDestroy()
        {
            GameRunning = false;
            _dispatcher.Stop();
        }

        async Task LoadChart(string filePath)
        {
            var cachePath = OSService.Get().CachePath;

            // var path = await ChartLoader.ToNFastChart(filePath);
            var path = filePath;

            Stopwatch stopwatch = Stopwatch.StartNew();
            var loadResult = await ChartLoader.LoadChartAsync(path, cachePath);
            _resourceProvider = loadResult.ResourceProvider;
            _chart = loadResult.Chart;
            
            Debug.Log($"load chart: {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Restart();

            async Task LoadMusic()
            {
                _audioClip = await _resourceProvider.GetAudioResource(_chart.Metadata.MusicFileName);
            }

            async Task LoadCover()
            {
                _coverTexture = 
                    await _resourceProvider.GetTextureResource(_chart.Metadata.BackgroundFileName);
            }

            await Task.WhenAll(LoadMusic(), LoadCover(), _chart.GenerateInternals());

            Debug.Log(
                $"load cover and audio files: {stopwatch.ElapsedMilliseconds} ms");
        }

        async void GameBegin()
        {
            Debug.Log("game begin");

            // create pools
            _linePool = new(() =>
            {
                var obj = Instantiate(JudgeLinePrefab);
                obj.name += " [Pooled]";
                obj.transform.localScale = ScreenAdapter.ScaleVector3(obj.transform.localScale);
                return obj;
            }, 5);

            _notePool = new(() =>
            {
                var obj = Instantiate(NotePrefab);
                obj.name += " [Pooled]";
                obj.GetComponent<PhiNoteWrapper>().Player = this;
                return obj;
            }, 5);

            _holdNotePool = new(() =>
            {
                var obj = Instantiate(HoldNotePrefab);
                obj.name += " [Pooled]";
                obj.GetComponent<PhiHoldNoteWrapper>().Player = this;
                return obj;
            }, 5);

            // play animation
            await MetadataDisplay.Display(string.Join('\n', "Name: " + _chart.Metadata.Name,
                "Difficulty: " + _chart.Metadata.Level,
                "Composer: " + _chart.Metadata.Composer, "Charter: " + _chart.Metadata.Charter));

            // start timer
            Timer = new SystemTimer();
            Timer.Reset();

            // get first bpm event
            _bpmEventGenerator = _chart.GetBpmEvents();
            _bpmEventGenerator.MoveNext();
            _nextBpmEvent = _bpmEventGenerator.Current;

            // music
            _musicLength = _audioClip.length;

            // generate lines
            Lines = _chart.GetLines();
            for (int i = 0; i < Lines.Count; i++)
            {
                var obj = _linePool.RequestObject();
                LineObjects.Add(obj);
                _lineWrappers.Add(obj.GetComponent<PhiLineWrapper>());
                obj.SetActive(true);
            }

            _landDistances = new float[Lines.Count];

            // notes
            _notes.AddRange(_chart.GetNotes());

            // events
            _eventsGenerator = _chart.GetEvents();

            // enable services and threads
            GameRunning = true;
            BgmAudioSource.PlayOneShot(_audioClip);

            _dispatcher.OnException += Debug.LogException;
            _dispatcher.Start();
            _dispatcher.Dispatch(FetchNewEvents);
        }

        private IList<LineEvent>[] eventsChunks = new IList<LineEvent>[24];
        private int eventsChunksBegin = 0;

        private void BeatUpdate()
        {
            FetchNewJudgeNotes();
            // add new line events
            // wait for thread
            if (eventsChunks[eventsChunksBegin] == null)
            {
                Debug.Log("Cannot keep up: EventsProducer");
                _dispatcher.Dispatch(FetchNewEvents);
            }
            while (eventsChunks[eventsChunksBegin] == null)
            {
            }

            _runningEvents.AddRange(eventsChunks[eventsChunksBegin]);
            eventsChunks[eventsChunksBegin] = null;
            eventsChunksBegin++;
            if (eventsChunksBegin == eventsChunks.Length)
            {
                eventsChunksBegin = 0;
            }

            _dispatcher.Dispatch(FetchNewEvents);
        }

       
        private void DoLineEvent(LineEvent lineEvent, float beat)
        {
            var last = lineEvent.EndTime - lineEvent.BeginTime;
            var easingX = (beat - lineEvent.BeginTime) / last;
            easingX = Mathf.Clamp(easingX, 0, 1);
            var easingY = EasingFunctions.Invoke(
                lineEvent.EasingFunc, easingX
                , lineEvent.EasingFuncRange.Low,
                lineEvent.EasingFuncRange.High);
            var value = lineEvent.BeginValue + (lineEvent.EndValue - lineEvent.BeginValue) * easingY;

            var lineId = (int) lineEvent.LineId;
            var lineObj = _lineWrappers[lineId];


            switch (lineEvent.Type)
            {
                case LineEventType.Alpha:
                {
                    var renderer = lineObj.LineBody;
                    var color = renderer.color;
                    color.a = value / 255f;
                    renderer.color = color;
                    break;
                }
                case LineEventType.MoveX:
                {
                    var transform = lineObj.transform;
                    var pos = transform.position;
                    pos.x = ScreenAdapter.ToGameXPos(value);
                    transform.position = pos;
                    break;
                }
                case LineEventType.MoveY:
                {
                    var transform = lineObj.transform;
                    var pos = transform.position;
                    pos.y = ScreenAdapter.ToGameYPos(value);
                    transform.position = pos;
                    break;
                }
                case LineEventType.Rotate:
                {
                    var transform = lineObj.transform;
                    transform.rotation = Quaternion.Euler(0, 0, -value);
                    Lines[lineId].Rotation = -value / 180f * MathF.PI;
                    break;
                }
                case LineEventType.Speed:
                {
                    Lines[lineId].Speed = value;
                    break;
                }
                case LineEventType.Incline:
                    break;
            }
        }

        private void ProcessLineEvents()
        {
            for (int i = 0; i < _runningEvents.Length; i++)
            {
                var item = _runningEvents[i];
                if (item.BeginTime > CurrentBeats) continue;
                if (CurrentBeats > item.EndTime)
                {
                    _runningEvents.RemoveAt(i);
                    i--;
                }

                DoLineEvent(item, CurrentBeats);
            }
        }

        private void UpdateLineHeight()
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                var newYPos = line.FindYPos(CurrentBeats);
                line.YPosition = newYPos;
            }
        }

        private void FetchNewNotes()
        {
            for (int i = 0; i < _notes.Length; i++)
            {
                var note = _notes[i]; 
                var yOffset = note.YPosition - Lines[(int) note.LineId].YPosition;
                if (yOffset <= 25f && yOffset >= 0)
                {
                    _newNotes.Enqueue(note);
                    _notes.RemoveAt(i);
                    i--;
                }
            }
        }

        private void FetchNewJudgeNotes()
        {
            // avoid unnecessary locking
            if (_currentMaxJudgeBeats >= _currentBeatCount + JUDGE_FUTURE_BEATS) return;

            lock (JudgeNotes)
            {
                while (_currentMaxJudgeBeats < _currentBeatCount + JUDGE_FUTURE_BEATS)
                {
                    JudgeNotes.AddRange(_chart.GetNotesByBeatIndex(_currentMaxJudgeBeats));
                    _currentMaxJudgeBeats++;
                }
            }
        }

        

        public void OnNoteFinalize(PhiNoteWrapper note)
        {
            _notePool.ReturnObject(note.gameObject);
        }
        public void OnNoteFinalize(PhiHoldNoteWrapper note)
        {
            _holdNotePool.ReturnObject(note.gameObject);
        }

        private void FetchNewEvents()
        {
            for (int i = 0; i < eventsChunks.Length; i++)
            {
                if (eventsChunks[i] != null) continue;
                if (!_eventsGenerator.MoveNext()) eventsChunks[i] = new List<LineEvent>();
                else eventsChunks[i] = _eventsGenerator.Current;
            }
        }
        
        private void GameTick()
        {
            if (_currentBpm != 0f) CurrentBeats = Timer.Time / 1000f / _beatLast;
            if (Timer.Time / 1000f > FINISH_LENGTH_OFFSET + _musicLength)
            {
                GameRunning = false;
                GameFinish();
                return;
            }
            if (_nextBpmEvent.Key <= CurrentBeats)
            {
                _currentBpm = _nextBpmEvent.Value;
                _nextBpmEvent = _bpmEventGenerator.MoveNext()
                    ? _bpmEventGenerator.Current
                    : new(float.PositiveInfinity, 0f);
                _beatLast = 60f / _currentBpm;
            }

            _currentBeatCount = (int) CurrentBeats;
            if (_currentBeatCount > _lastBeatCount)
            {
                for (int i = 0; i < _currentBeatCount - _lastBeatCount; i++)
                {
                    BeatUpdate();
                }

                _lastBeatCount = _currentBeatCount;
            }

            ProcessLineEvents();
            _dispatcher.Dispatch(UpdateLineHeight);
            _dispatcher.Dispatch(FetchNewNotes);
            ProcessNewNotes();
        }

        private void GameFinish()
        {
            NavigationService.Get().JumpScene("Scenes/PlayFinishScene");
        }

        private void ProcessNewNotes()
        {
            var noteCount = _newNotes.Count;
            if (noteCount == 0) return;

            lock (_newNotes)
            {
                for (var i = 0; i < noteCount; i++)
                {
                    var note = _newNotes.Dequeue();

                    var noteObj = note.NoteGameObject;
                    if (noteObj != null) continue;

                    var isHold = note.Type == NoteType.Hold;

                    noteObj = note.NoteGameObject =
                        isHold ? _holdNotePool.RequestObject() : _notePool.RequestObject();

                    var lineWrapper = _lineWrappers[(int) note.LineId];

                    noteObj.transform.parent = note.ReverseDirection
                        ? lineWrapper.DownNoteViewport
                        : lineWrapper.UpNoteViewport;

                    var localPos = noteObj.transform.localPosition;
                    localPos.y = note.YPosition - Lines[(int) note.LineId].YPosition;
                    localPos.x = ScreenAdapter.ToGameXPos(note.XPosition);
                    noteObj.transform.localPosition = localPos;

                    IPhiNoteWrapper noteWrapper =
                        isHold
                            ? noteObj.GetComponent<PhiHoldNoteWrapper>()
                            : noteObj.GetComponent<PhiNoteWrapper>();
                    noteWrapper.NoteStart(note);
                    noteObj.SetActive(true);
                }
            }
        }
    }
}