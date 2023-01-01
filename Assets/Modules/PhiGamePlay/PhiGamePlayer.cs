using System;
using System.Collections;
using System.Collections.Concurrent;
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
        // game running
        internal bool GameRunning { get; private set; } = false;
        internal SystemTimer Timer { get; private set; } = null;
        private BeatsTimer _beatsTimer = null;


        private int _currentBeatCount = 0;
        private int _lastBeatCount = -1;
        public float CurrentBeats => _currentBeats;
        [SerializeField] private float _currentBeats = 0f;
        private BpmEvent _nextBpmEvent;
        private IEnumerator _bpmEventGenerator;
        private readonly ThreadDispatcher _dispatcher = new ThreadDispatcher();
        public readonly Queue<PhiNote> NewNotes = new();

        // audio
        private const float FINISH_LENGTH_OFFSET = 3000f; // unit: ms
        private float _musicLength = 0f; // unit: ms

        // events
        private readonly UnorderedList<UnitEvent> _runningEvents = new UnorderedList<UnitEvent>();
        private IEnumerator<IList<UnitEvent>> _eventsGenerator;

        // unity resources
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
        public ObjectPool NotePool;
        public ObjectPool HoldNotePool;

        // chart data
        private IResourceProvider _resourceProvider;
        private PhiChart _chart;
        internal IList<PhiUnit> Units;
        internal readonly List<GameObject> UnitObjects = new();
        public readonly List<IPhiUnitWrapper> UnitWrappers = new();
        private UnorderedList<PhiNote> _notes;

        // judge
        internal readonly UnorderedList<PhiNote> JudgeNotes = new();
        private int _currentMaxJudgeBeats = 0;
        private const int JUDGE_FUTURE_BEATS = 4;
        public enum JudgeResult
        {
            Miss,
            Bad,
            Good,
            Perfect
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

        private async Task LoadResources()
        {
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
        }

        private void LoadChartProperties()
        {
            // load bpm event
            _bpmEventGenerator = _chart.BpmEvents.GetEnumerator();
            _bpmEventGenerator.MoveNext();
            _nextBpmEvent = (BpmEvent) _bpmEventGenerator.Current;

            // music
            _musicLength = _audioClip.length * 1000f;

            // events
            _eventsGenerator = _chart.GetOrderedEvents();

            // notes
            _notes = UnorderedList<PhiNote>.From(_chart.Notes);
        }

        public async Task LoadChart(ChartLoader.LoadResult loadResult)
        {
            _resourceProvider = loadResult.ResourceProvider;
            _chart = loadResult.Chart;

            // load
            await LoadResources();
            LoadChartProperties();
        }

        private void InitCover()
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", _coverTexture);
            foreach (var backgroundImage in BackgroundImages)
            {
                backgroundImage.SetPropertyBlock(block);
            }
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

            NotePool = new(() =>
            {
                var obj = Instantiate(NotePrefab);
                obj.name += " [Pooled]";
                obj.GetComponent<PhiNoteWrapper>().Player = this;
                return obj;
            }, 5);

            HoldNotePool = new(() =>
            {
                var obj = Instantiate(HoldNotePrefab);
                obj.name += " [Pooled]";
                obj.GetComponent<PhiHoldNoteWrapper>().Player = this;
                return obj;
            }, 5);
        }

        public async void RunGame()
        {
            InitCover();
            InitPools();

            // generate units
            Units = _chart.Units;
            for (int i = 0; i < Units.Count; i++)
            {
                var obj = CreateUnitObject(Units[i]);
                obj.name += $"(UnitId: {i})";
                obj.SetActive(true);
                var unitWrapper = UnitWrappers[i];
                if (unitWrapper != null) unitWrapper.Unit = Units[i];

                UnitObjects.Add(obj);
            }
            for (int i = 0; i < Units.Count; i++)
            {
                var item = Units[i];
                var parentUnitId = item.ParentUnitId;
                if (parentUnitId == -1) continue;
                UnitObjects[i].transform.SetParent(UnitObjects[parentUnitId].transform, true);
            }

            // warm up
            if (_notes.Length > 4096)
            {
                ToastService.Get().Show(ToastService.ToastType.Success, "预热中...");
                await Tweener.Get().RunTween(5000f, (val) =>
                {
                    NotePool.WarmUp(Convert.ToInt32(val));
                }, beginValue: 0f, endValue: MathF.Min(10240f, _notes.Length / 5f));
            }

            // play animation
            await MetadataDisplay.Display(string.Join('\n', "Name: " + _chart.Metadata.Name,
                "Difficulty: " + _chart.Metadata.Level,
                "Composer: " + _chart.Metadata.Composer, "Charter: " + _chart.Metadata.Charter));


            // start timer / services / threads
            Timer = new SystemTimer();
            _beatsTimer = new BeatsTimer(Timer);
            Timer.Reset();
            _beatsTimer.Reset();

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
                    UnitWrappers.Add(obj.GetComponent<PhiLineWrapper>());
                    return obj;
                }
                case PhiUnitType.AttachUI:
                {
                    var obj = _attachUiPool.RequestObject();
                    UnitWrappers.Add(null);
                    return obj;
                }
                case PhiUnitType.Text:
                {
                    // TODO: Add TextUnitPrefab
                    var obj = _attachUiPool.RequestObject();
                    UnitWrappers.Add(null);
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
            var unitObj = UnitWrappers[(int)unitEvent.UnitId];
            unitObj?.DoEvent(unitEvent.Type, value);
        }

        private void DoUnitEvents()
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

                // if (!item.isEventStarted)
                // {
                //     item.isEventStarted = true;
                //     $"event u {item.UnitId} e {item.Type} bb {item.BeginBeats} bv {item.BeginValue} ev {item.EndValue} -> begin"
                //         .Log();
                // }

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
            lock (NewNotes)
            {
                for (int i = 0; i < _notes.Length; i++)
                {
                    var note = _notes[i];
                    var height = note.NoteHeight - Units[(int)note.unitId].YPosition;
                    if (height <= 25f && height >= 0)
                    {
                        NewNotes.Enqueue(note);
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
            NotePool.ReturnObject(note.gameObject);
        }
        public void OnNoteFinalize(PhiHoldNoteWrapper note)
        {
            HoldNotePool.ReturnObject(note.gameObject);
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
            if (Timer.Time > FINISH_LENGTH_OFFSET + _musicLength)
            {
                GameRunning = false;
                GameFinish();
                return;
            }

            // update beats
            if (_nextBpmEvent != null && _nextBpmEvent.BeginBeats <= _currentBeats)
            {
                _beatsTimer.ApplyNewBpm(_nextBpmEvent.Value, _nextBpmEvent.BeginBeats);
                _nextBpmEvent = _bpmEventGenerator.MoveNext()
                    ? (BpmEvent)_bpmEventGenerator.Current
                    : null;
            }

            if (_beatsTimer.CurrentBpm != 0) _currentBeats = _beatsTimer.Beats;

            // check beats

            _currentBeatCount = (int) _currentBeats;
            if (_currentBeatCount > _lastBeatCount)
            {
                for (int i = 0; i < _currentBeatCount - _lastBeatCount; i++)
                {
                    BeatUpdate();
                }

                _lastBeatCount = _currentBeatCount;
            }

            DoUnitEvents();
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
        }
    }
}