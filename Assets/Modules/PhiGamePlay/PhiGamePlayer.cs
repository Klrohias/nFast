using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.Utilities;
using UnityEngine;
using Klrohias.NFast.Native;
using Klrohias.NFast.UIComponent;
using Klrohias.NFast.Resource;
using Klrohias.NFast.Tween;
using Klrohias.NFast.UIControllers;
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
        public float CurrentBeats => _currentBeats;
        [SerializeField] private float _currentBeats = 0f;
        private BpmEvent _nextBpmEvent;
        private float _currentBpm = 0f;
        private float _beatLast = 0f;
        private IEnumerator<BpmEvent> _bpmEventGenerator;
        private readonly ThreadDispatcher _dispatcher = new ThreadDispatcher();
        private readonly Queue<PhiNote> _newNotes = new Queue<PhiNote>();

        // audio
        private const float FINISH_LENGTH_OFFSET = 3f; // unit: second
        private float _musicLength = 0f; // unit: second

        // events
        private readonly UnorderedList<UnitEvent> _runningEvents = new UnorderedList<UnitEvent>();
        private IEnumerator<IList<UnitEvent>> _eventsGenerator;

        // unity resources
        public Transform BackgroundTransform;
        public SpriteRenderer[] BackgroundImages;
        public GameObject JudgeLinePrefab;
        public GameObject AttachUIUnitPrefab;
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
        private ObjectPool _attachUiPool;
        private ObjectPool _linePool;
        private ObjectPool _notePool;
        private ObjectPool _holdNotePool;

        // chart data
        private IResourceProvider _resourceProvider;
        private PhiChart _chart;
        internal IList<PhiUnit> Units;
        internal readonly List<GameObject> UnitObjects = new List<GameObject>();
        private readonly List<IPhiUnitWrapper> _unitWrappers = new List<IPhiUnitWrapper>();
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

            Stopwatch stopwatch = Stopwatch.StartNew();
            var loadResult = await ChartLoader.LoadChartAsync(filePath, cachePath);
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

        private void InitPools()
        {
            _linePool = new(() =>
            {
                var obj = Instantiate(JudgeLinePrefab);
                obj.name += " [Pooled]";
                obj.GetComponent<PhiLineWrapper>().Player = this;
                return obj;
            }, 5);

            _attachUiPool = new(() =>
            {
                var obj = Instantiate(AttachUIUnitPrefab);
                obj.name += " [Pooled]";
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
        }

        async void GameBegin()
        {
            Debug.Log("game begin");

            InitPools();

            // get first bpm event
            _bpmEventGenerator = _chart.BpmEvents.GetEnumerator();
            _bpmEventGenerator.MoveNext();
            _nextBpmEvent = _bpmEventGenerator.Current;

            // music
            _musicLength = _audioClip.length;

            // generate units
            Units = _chart.Units;
            for (int i = 0; i < Units.Count; i++)
            {
                var obj = CreateUnitObject(Units[i]);
                obj.name += $"(UnitId: {i})";
                obj.SetActive(true);
                var unitWrapper = _unitWrappers[i];
                if (unitWrapper != null) unitWrapper.Unit = Units[i];

                UnitObjects.Add(obj);
            }
            for (int i = 0; i < Units.Count; i++)
            {
                var item = Units[i];
                var parentLineId = item.ParentObjectId;
                if (parentLineId == -1) continue;
                UnitObjects[i].transform.SetParent(UnitObjects[parentLineId].transform, true);
            }

            _landDistances = new float[Units.Count];

            // notes
            _notes.AddRange(_chart.Notes);

            // warm up
            if (_notes.Length > 4096)
            {
                ToastService.Get().Show(ToastService.ToastType.Success, "预热中...");
                await Tweener.Get().RunTween(5000f, (val) =>
                {
                    _notePool.WarmUp(Convert.ToInt32(val));
                }, beginValue: 0f, endValue: MathF.Min(10240f, _notes.Length / 5f));
            }

            // events
            _eventsGenerator = _chart.GetEvents();

            // play animation
            await MetadataDisplay.Display(string.Join('\n', "Name: " + _chart.Metadata.Name,
                "Difficulty: " + _chart.Metadata.Level,
                "Composer: " + _chart.Metadata.Composer, "Charter: " + _chart.Metadata.Charter));


            // start timer
            Timer = new SystemTimer();
            Timer.Reset();

            // enable services and threads

            _dispatcher.OnException += Debug.LogException;
            _dispatcher.Start();
            _dispatcher.Dispatch(FetchNewEvents);

            GameRunning = true;
            BgmAudioSource.PlayOneShot(_audioClip);
        }

        private GameObject CreateUnitObject(PhiUnit unit)
        {
            switch (unit.Type)
            {
                case PhiUnitType.Line:
                {
                    var obj = _linePool.RequestObject();
                    _unitWrappers.Add(obj.GetComponent<PhiLineWrapper>());
                    return obj;
                }
                case PhiUnitType.AttachUI:
                {
                    var obj = _attachUiPool.RequestObject();
                    _unitWrappers.Add(null);
                    return obj;
                }
                case PhiUnitType.Text:
                {
                    // TODO: Add TextUnitPrefab
                    var obj = _attachUiPool.RequestObject();
                    _unitWrappers.Add(null);
                    return obj;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private IList<UnitEvent>[] eventsChunks = new IList<UnitEvent>[24];
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

            var retryCount = 30;
            while (eventsChunks[eventsChunksBegin] == null && retryCount > 0)
            {
                Thread.Sleep(100);
                retryCount--;
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

       
        private void DoUnitEvent(UnitEvent unitEvent, float beat)
        {
            var last = unitEvent.EndBeats - unitEvent.BeginBeats;
            var easingX = (beat - unitEvent.BeginBeats) / last;
            easingX = Mathf.Clamp(easingX, 0, 1);
            var easingY = EasingFunctions.Invoke(
                unitEvent.EasingFunc, easingX
                , unitEvent.EasingFuncRange.Low,
                unitEvent.EasingFuncRange.High);
            var value = unitEvent.BeginValue + (unitEvent.EndValue - unitEvent.BeginValue) * easingY;
            var unitObj = _unitWrappers[(int)unitEvent.LineId];
            unitObj?.DoEvent(unitEvent.Type, value);
        }

        private void ProcessLineEvents()
        {
            for (int i = 0; i < _runningEvents.Length; i++)
            {
                var item = _runningEvents[i];
                if (item.BeginBeats > _currentBeats) continue;
                if (_currentBeats > item.EndBeats)
                {
                    _runningEvents.RemoveAt(i);
                    i--;
                }

                DoUnitEvent(item, _currentBeats);
            }
        }

        private void UpdateLineHeight()
        {
            for (int i = 0; i < Units.Count; i++)
            {
                var line = Units[i];
                var newYPos = line.FindYPos(_currentBeats);
                line.YPosition = newYPos;
            }
        }

        private void FetchNewNotes()
        {
            lock (_newNotes)
            {
                for (int i = 0; i < _notes.Length; i++)
                {
                    var note = _notes[i];
                    var height = note.NoteHeight - Units[(int)note.UnitObjectId].YPosition;
                    if (height <= 25f && height >= 0)
                    {
                        _newNotes.Enqueue(note);
                        _notes.RemoveAt(i);
                        i--;
                    }
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
                if (!_eventsGenerator.MoveNext()) eventsChunks[i] = new List<UnitEvent>();
                else eventsChunks[i] = _eventsGenerator.Current;
            }
        }

        private void GameTick()
        {
            if (_currentBpm != 0f) _currentBeats = Timer.Time / 1000f / _beatLast;
            if (Timer.Time / 1000f > FINISH_LENGTH_OFFSET + _musicLength)
            {
                GameRunning = false;
                GameFinish();
                return;
            }

            if (_nextBpmEvent.BeginBeats <= _currentBeats)
            {
                _currentBpm = _nextBpmEvent.Value;
                _nextBpmEvent = _bpmEventGenerator.MoveNext()
                    ? _bpmEventGenerator.Current
                    : BpmEvent.Create(float.PositiveInfinity, 0f);
                _beatLast = 60f / _currentBpm;
            }

            _currentBeatCount = (int) _currentBeats;
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

                    noteObj = note.NoteGameObject =
                        note.Type == NoteType.Hold ? _holdNotePool.RequestObject() : _notePool.RequestObject();

                    var lineWrapper = _unitWrappers[(int) note.UnitObjectId];

                    var typedWrapper = (PhiLineWrapper) lineWrapper;
                    noteObj.transform.parent = note.ReverseDirection
                        ? typedWrapper.DownNoteViewport
                        : typedWrapper.UpNoteViewport;

                    var localPos = noteObj.transform.localPosition;
                    var noteYOffset = ScreenAdapter.ToGameYPos(note.YPosition);
                    localPos.y = note.NoteHeight - Units[(int) note.UnitObjectId].YPosition + noteYOffset;
                    localPos.x = ScreenAdapter.ToGameXPos(note.XPosition);
                    noteObj.transform.localPosition = localPos;

                    var noteWrapper = noteObj.GetComponent<IPhiNoteWrapper>();
                    noteWrapper.NoteStart(note);
                    noteObj.SetActive(true);
                }
            }
        }
    }
}