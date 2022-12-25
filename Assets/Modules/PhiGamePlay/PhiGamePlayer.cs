using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.PhiChartLoader.Pez;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.Utilities;
using UnityEngine;
using Klrohias.NFast.PhiChartLoader.NFast;
using Klrohias.NFast.Native;
using Cysharp.Threading.Tasks;
using Klrohias.NFast.Tween;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using EventType = Klrohias.NFast.PhiChartLoader.NFast.EventType;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiGamePlayer : MonoBehaviour
    {
        private bool _gameRunning = false;
        public SystemTimer Timer { get; private set; } = null;

        // game running
        private int _currentBeatCount = 0;
        private int _lastBeatCount = -1;
        public float CurrentBeats { get; private set; } = 0f;
        private KeyValuePair<ChartTimespan, float> _nextBpmEvent;
        private float _currentBpm = 0f;
        private float _beatLast = 0f;
        private IEnumerator<KeyValuePair<ChartTimespan, float>> _bpmEventGenerator;
        private readonly ThreadDispatcher _dispatcher = new ThreadDispatcher();
        private readonly Queue<ChartNote> _newNotes = new Queue<ChartNote>();

        // audio
        private const float FINISH_LENGTH_OFFSET = 3f;
        private float _musicLength = 0f; // unit: second

        // events
        private readonly UnorderedList<LineEvent> _runningEvents = new UnorderedList<LineEvent>();
        private IEnumerator<IList<LineEvent>> _eventsGenerator;

        // screen adaption
        private const float ASPECT_RATIO = 16f / 9f;
        private float _scaleFactor = 1f;
        private readonly (float Width, float Height) _gameVirtualResolution = (625f, 440f);
        private float _gameViewport = 10f;
        private const float NOTE_WIDTH = 2.5f * 0.88f;

        // unity resources
        public Transform BackgroundTransform;
        public SpriteRenderer[] BackgroundImages;
        public GameObject JudgeLinePrefab;
        public GameObject NotePrefab;
        public GameObject HoldNotePrefab;
        public Image LoadingMask;
        public TMP_Text MetadataText;
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
        private IPhiChart _chart;
        internal IList<ChartLine> Lines;
        private readonly List<GameObject> _lineObjects = new List<GameObject>();
        private readonly List<PhiLineWrapper> _lineWrappers = new List<PhiLineWrapper>();
        private IList<ChartNote> _notes;
        private int _notesBegin = 0;

        // judge
        private readonly UnorderedList<ChartNote> _judgeNotes = new UnorderedList<ChartNote>();
        private readonly UnorderedList<ChartNote> _judgingNotes = new UnorderedList<ChartNote>();
        private float[] _landDistances = null;
        private int _currentMaxJudgeBeats = 0;
        private const int JUDGE_FUTURE_BEATS = 4;
        public float PerfectJudgeRange = 80f;
        public float GoodJudgeRange = 150f;
        public float BadJudgeRange = 350f;

        public class GameStartInfo
        {
            public string Path = "";
        }
        async void Start()
        {
            // load chart file
            var loadInstruction = NavigationService.Get().ExtraData as GameStartInfo;
            if (loadInstruction == null) throw new InvalidOperationException("failed to load: unknown");

            SetupScreenScale();
            BackgroundTransform.localScale = ScaleVector3(BackgroundTransform.localScale);

            await LoadChart(loadInstruction.Path);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", _coverTexture);
            foreach (var backgroundImage in BackgroundImages)
            {
                backgroundImage.SetPropertyBlock(block);
            }

            GameBegin();
        }

        Vector2 ScaleVector2(Vector2 inputVector2) => inputVector2 * _scaleFactor;

        Vector3 ScaleVector3(Vector3 inputVector3) =>
            new(inputVector3.x * _scaleFactor, inputVector3.y * _scaleFactor, inputVector3.z);

        float ToGameXPos(float x) =>
            (x / _gameVirtualResolution.Width) * (_gameViewport / 2) * _scaleFactor * ASPECT_RATIO;

        float ToGameYPos(float x) => (x / _gameVirtualResolution.Height) * (_gameViewport / 2) * _scaleFactor;

        float ToChartXPos(float x) =>
            (x / ASPECT_RATIO / _scaleFactor) / (_gameViewport / 2) * _gameVirtualResolution.Width;
        void SetupScreenScale()
        {
            var safeArea = Screen.safeArea;
            var aspectRatio = safeArea.width / safeArea.height;
            if (aspectRatio < ASPECT_RATIO)
            {
                _scaleFactor = aspectRatio / ASPECT_RATIO;
            }
        }

        async Task LoadChart(string filePath)
        {
            var cachePath = OSService.Get().CachePath;

            Stopwatch stopwatch = Stopwatch.StartNew();

            var coverPath = "";
            var musicPath = "";

            // load pez chart
            {
                PezChart pezChart = null;
                await Async.RunOnThread(() =>
                {
                    pezChart = PezLoader.LoadPezChart(filePath);
                });

                bool coverExtracted = false;
                bool audioExtracted = false;

                // Metadata == null here, so use PezMetadata
                coverPath = Path.Combine(cachePath, pezChart.PezMetadata.Background);
                musicPath = Path.Combine(cachePath, pezChart.PezMetadata.Song);

                await Task.WhenAll(
                    Async.RunOnThread(() =>
                    {
                        var coverStream = File.OpenWrite(coverPath);
                        PezLoader.ExtractFile(pezChart, pezChart.PezMetadata.Background, coverStream);
                        coverStream.Flush();
                        coverStream.Close();
                        coverExtracted = true;
                    }),
                    Async.RunOnThread(() =>
                    {
                        var musicStream = File.OpenWrite(musicPath);
                        PezLoader.ExtractFile(pezChart, pezChart.PezMetadata.Song, musicStream);
                        musicStream.Flush();
                        musicStream.Close();
                        audioExtracted = true;
                    }),
                    Task.Run(async () =>
                    {

                        pezChart.ConvertToNFastChart();
                        while (!(audioExtracted && coverExtracted))
                        {
                            await Task.Delay(80);
                        }

                        pezChart.DropZipData();
                        _chart = pezChart.NFastPhiChart;
                    })
                );

                Debug.Log($"load pez chart: {stopwatch.ElapsedMilliseconds} ms");
            }

            stopwatch.Restart();

            async Task LoadMusic()
            {
                using var request =
                    UnityWebRequestMultimedia.GetAudioClip($"file://{musicPath}", AudioType.MPEG);
                await request.SendWebRequest();

                if (!string.IsNullOrEmpty(request.error))
                {
                    Debug.LogWarning($"music load failed: {request.error}");
                    return;
                }
                _audioClip = DownloadHandlerAudioClip.GetContent(request);
            }

            async Task LoadCover()
            {
                using var request =
                    UnityWebRequestTexture.GetTexture($"file://{coverPath}");
                await request.SendWebRequest();
                if (!string.IsNullOrEmpty(request.error))
                {
                    Debug.LogWarning($"cover load failed: {request.error}");
                    return;
                }
                _coverTexture = DownloadHandlerTexture.GetContent(request);
            }

            await Task.WhenAll(LoadMusic(), LoadCover(), ((NFastPhiChart) _chart).GenerateInternals());

            Debug.Log(
                $"convert pez to nfast chart + extract files + load cover and audio files: {stopwatch.ElapsedMilliseconds} ms");
        }

        async void GameBegin()
        {
            Debug.Log("game begin");

            // create pools
            _linePool = new(() =>
            {
                var obj = Instantiate(JudgeLinePrefab);
                obj.name += " [Pooled]";
                obj.transform.localScale = ScaleVector3(obj.transform.localScale);
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

            MetadataText.text = string.Join('\n', "Name: " + _chart.Metadata.Name, "Difficulty: " + _chart.Metadata.Level,
                "Composer: " + _chart.Metadata.Composer, "Charter: " + _chart.Metadata.Charter);

            await MetadataText.NTweenAlpha(300f, EasingFunction.SineIn, 0f, 1f);
            await Task.Delay(1500);
            await Task.WhenAll(MetadataText.NTweenAlpha(300f, EasingFunction.SineOut, 1f, 0f),
                LoadingMask.NTweenAlpha(300f, EasingFunction.SineOut, 1f, 0f));
            LoadingMask.gameObject.SetActive(false);

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
                _lineObjects.Add(obj);
                _lineWrappers.Add(obj.GetComponent<PhiLineWrapper>());
                obj.SetActive(true);
            }

            _landDistances = new float[Lines.Count];

            // notes
            _notes = _chart.GetNotes();

            // events
            _eventsGenerator = _chart.GetEvents();

            // enable services and threads
            _gameRunning = true;

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
                case EventType.Alpha:
                {
                    var renderer = lineObj.LineBody;
                    var color = renderer.color;
                    color.a = value / 255f;
                    renderer.color = color;
                    break;
                }
                case EventType.MoveX:
                {
                    var transform = lineObj.transform;
                    var pos = transform.position;
                    pos.x = ToGameXPos(value);
                    transform.position = pos;
                    break;
                }
                case EventType.MoveY:
                {
                    var transform = lineObj.transform;
                    var pos = transform.position;
                    pos.y = ToGameYPos(value);
                    transform.position = pos;
                    break;
                }
                case EventType.Rotate:
                {
                    var transform = lineObj.transform;
                    transform.rotation = Quaternion.Euler(0, 0, -value);
                    Lines[lineId].Rotation = -value / 180f * MathF.PI;
                    break;
                }
                case EventType.Speed:
                {
                    Lines[lineId].Speed = value;
                    break;
                }
                case EventType.Incline:
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
            for (int i = _notesBegin; i < _notes.Count; i++)
            {
                var note = _notes[i]; 
                var yOffset = note.YPosition - Lines[(int) note.LineId].YPosition;
                if (yOffset <= 25f && yOffset >= 0)
                {
                    _newNotes.Enqueue(note);
                    (_notes[i], _notes[_notesBegin]) = (_notes[_notesBegin], null);
                    _notesBegin++;
                }
            }
        }

        private void FetchNewJudgeNotes()
        {
            // avoid unnecessary locking
            if (_currentMaxJudgeBeats >= _currentBeatCount + JUDGE_FUTURE_BEATS) return;

            lock (_judgeNotes)
            {
                while (_currentMaxJudgeBeats < _currentBeatCount + JUDGE_FUTURE_BEATS)
                {
                    _judgeNotes.AddRange(_chart.GetNotesByBeatIndex(_currentMaxJudgeBeats));
                    _currentMaxJudgeBeats++;
                }
            }
        }

        private void ProcessJudgeNotes()
        {
            var currentTime = Timer.Time;
            for (int i = 0; i < _judgeNotes.Length; i++)
            {
                var item = _judgeNotes[i];
                if (currentTime - item.JudgeTime > BadJudgeRange)
                {
                    // miss
                    _judgeNotes.RemoveAt(i);
                    i--;
                    continue;
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
                _gameRunning = false;
                GameFinish();
                return;
            }
            if (_nextBpmEvent.Key <= CurrentBeats)
            {
                _currentBpm = _nextBpmEvent.Value;
                _nextBpmEvent = _bpmEventGenerator.MoveNext()
                    ? _bpmEventGenerator.Current
                    : new(new(float.PositiveInfinity), 0f);
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

            // touch & judge
            var touchCount = Input.touchCount;
            for (int i = 0; i < touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                ProcessTouch(touch);
            }

            ProcessNewNotes();
            ProcessJudgeNotes();
        }

        private void GameFinish()
        {
            NavigationService.Get().JumpScene("Scenes/PlayFinishScene");
        }
        private Vector2 GetLandPos(Vector2 lineOrigin, float rotation, Vector2 touchPos)
        {
            if (rotation % MathF.PI == 0f) return new Vector2(touchPos.x, lineOrigin.y);
            var k = MathF.Tan(rotation);
            var b = lineOrigin.y - k * lineOrigin.x;
            var k2 = -1 / k;
            var b2 = touchPos.y - k2 * touchPos.x;
            var x = (b2 - b) / (k - k2);
            var y = k * x + b;
            return new Vector2(x, y);
        }
        private void ProcessTouch(Touch rawTouch)
        {
            // avoid NullReferenceException and unnecessary code
            if (_judgeNotes.Length == 0) return;

            var worldPos = Camera.main.ScreenToWorldPoint(rawTouch.position);
            ChartNote targetNode = null;
            for (var index = 0; index < Lines.Count; index++)
            {
                var chartLine = Lines[index];
                var linePos = _lineObjects[(int) chartLine.LineId].transform.position;
                var landPos = Vector2.Distance(GetLandPos(linePos, chartLine.Rotation, worldPos), linePos);
                _landDistances[index] = landPos;
            }

            foreach (var note in _judgeNotes)
            {
                if (MathF.Abs(_landDistances[note.LineId] - ToGameXPos(note.XPosition)) > NOTE_WIDTH / 1.75f) continue;
                if ((targetNode?.JudgeTime ?? float.PositiveInfinity) > note.JudgeTime)
                    targetNode = note;
            }

            switch (rawTouch.phase)
            {
                case TouchPhase.Began:
                {
                    // judge tap/hold
                    try
                    {
                        targetNode.NoteGameObject.GetComponent<PhiNoteWrapper>().IsJudged = true;
                        print("judged!");
                    }catch{}

                    break;
                }
                case TouchPhase.Moved:
                {
                    // judge flick
                    // check hold
                    break;
                }
                case TouchPhase.Stationary:
                {
                    // check hold
                    break;
                }
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                {
                    // check hold
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
                    localPos.x = ToGameXPos(note.XPosition);
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
        private void Update()
        {
            if (_gameRunning) GameTick();
        }

        private void OnDestroy()
        {
            _gameRunning = false;
            _dispatcher.Stop();
        }
    }
}