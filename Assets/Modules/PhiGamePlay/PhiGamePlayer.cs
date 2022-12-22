using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.PhiChartLoader.Pez;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.Utilities;
using UnityEngine;
using Klrohias.NFast.PhiChartLoader.NFast;
using Klrohias.NFast.Native;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using EventType = Klrohias.NFast.PhiChartLoader.NFast.EventType;

namespace Klrohias.NFast.PhiGamePlay
{
    public class PhiGamePlayer : MonoBehaviour
    {
        private bool gameRunning = false;
        public SystemTimer Timer { get; private set; } = null;

        // game running
        private int currentBeatCount = 0;
        private int lastBeatCount = -1;
        public float CurrentBeats { get; private set; } = 0f;
        private KeyValuePair<ChartTimespan, float> nextBpmEvent;
        private float currentBpm = 0f;
        private float beatLast = 0f;
        private IEnumerator<KeyValuePair<ChartTimespan, float>> bpmEventGenerator;
        private readonly ThreadDispatcher dispatcher = new ThreadDispatcher();
        private Queue<ChartNote> newNotes = new Queue<ChartNote>();

        // events
        private UnorderedList<LineEvent> runningEvents = new UnorderedList<LineEvent>();
        private IEnumerator<IList<LineEvent>> eventsGenerator;

        // screen adaption
        private const float ASPECT_RATIO = 16f / 9f;
        private float scaleFactor = 1f;
        private (float Width, float Height) gameVirtualResolution = (625f, 440f);
        private float gameViewport = 10f;

        // unity resources
        public Transform BackgroundTransform;
        public SpriteRenderer[] BackgroundImages;
        public GameObject JudgeLinePrefab;
        public GameObject NotePrefab;
        public GameObject HoldNotePrefab;
        private Texture2D coverTexture = null;
        private AudioClip audioClip = null;

        // object pools
        private ObjectPool linePool;
        private ObjectPool notePool;
        private ObjectPool holdNotePool;

        // chart data
        private IPhiChart chart;
        private IList<ChartLine> lines;
        private List<GameObject> lineObjects = new List<GameObject>();
        private IList<ChartNote> notes;
        private int notesBegin = 0;

        public class GameStartInfo
        {
            public bool UseLargeChart = false;
            public string Path = "";
        }
        async void Start()
        {
            // load chart file
            var loadInstruction = NavigationService.Get().ExtraData as GameStartInfo;
            if (loadInstruction == null) throw new InvalidOperationException("failed to load: unknown");

            SetupScreenScale();
            BackgroundTransform.localScale = ScaleVector3(BackgroundTransform.localScale);

            await LoadChart(loadInstruction.Path, loadInstruction.UseLargeChart);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", coverTexture);
            foreach (var backgroundImage in BackgroundImages)
            {
                backgroundImage.SetPropertyBlock(block);
            }

            GameBegin();
        }

        Vector2 ScaleVector2(Vector2 inputVector2) => inputVector2 * scaleFactor;

        Vector3 ScaleVector3(Vector3 inputVector3) =>
            new(inputVector3.x * scaleFactor, inputVector3.y * scaleFactor, inputVector3.z);

        float ToGameXPos(float x) =>
            (x / gameVirtualResolution.Width) * (gameViewport / 2) * scaleFactor * ASPECT_RATIO;

        float ToGameYPos(float x) => (x / gameVirtualResolution.Height) * (gameViewport / 2) * scaleFactor;

        void SetupScreenScale()
        {
            var safeArea = Screen.safeArea;
            var aspectRatio = safeArea.width / safeArea.height;
            if (aspectRatio < ASPECT_RATIO)
            {
                scaleFactor = aspectRatio / ASPECT_RATIO;
            }
        }

        async Task LoadChart(string filePath, bool useLargeChart = false)
        {
            var cachePath = OSService.Get().CachePath;

            Stopwatch stopwatch = Stopwatch.StartNew();

            var coverPath = "";
            var musicPath = "";

            #region Disabled
#if false
            if (useLargeChart)
            {
                await Async.RunOnThread(() =>
                {
                    chart = LargePezLoader.Load(filePath, cachePath);
                    coverPath = Path.Combine(cachePath, chart.Metadata.BackgroundFileName);
                    musicPath = Path.Combine(cachePath, chart.Metadata.MusicFileName);
                });
                await Task.WhenAll(Async.RunOnThread(() =>
                    {
                        var coverStream = File.OpenWrite(coverPath);
                        LargePezLoader.ExtractFile((LargePezChart) chart, chart.Metadata.BackgroundFileName,
                            coverStream);
                        coverStream.Flush();
                        coverStream.Close();
                    }),
                    Async.RunOnThread(() =>
                    {
                        var musicStream = File.OpenWrite(musicPath);
                        LargePezLoader.ExtractFile((LargePezChart) chart, chart.Metadata.MusicFileName, musicStream);
                        musicStream.Flush();
                        musicStream.Close();
                    }));
            }
            else
#endif
            #endregion
            {
                PezChart pezChart = null;
                await Async.RunOnThread(() =>
                {
                    pezChart = PezLoader.LoadPezChart(filePath);
                    pezChart.ConvertToNFastChart();
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
                        PezLoader.ExtractFile(pezChart, pezChart.Metadata.BackgroundFileName, coverStream);
                        coverStream.Flush();
                        coverStream.Close();
                        coverExtracted = true;
                    }),
                    Async.RunOnThread(() =>
                    {
                        var musicStream = File.OpenWrite(musicPath);
                        PezLoader.ExtractFile(pezChart, pezChart.Metadata.MusicFileName, musicStream);
                        musicStream.Flush();
                        musicStream.Close();
                        audioExtracted = true;
                    }),
                    Task.Run(async () =>
                    {
                        while (!(audioExtracted && coverExtracted))
                        {
                            await Task.Delay(80);
                        }

                        pezChart.DropZipData();
                        chart = pezChart.NFastPhiChart;
                    })
                );

                Debug.Log($"load pez chart: {stopwatch.ElapsedMilliseconds} ms");
            }

            stopwatch.Restart();

            await Task.WhenAll(Async.CallbackToTask((resolve) =>
            {
                IEnumerator CO_LoadMusic()
                {
                    using var request =
                        UnityWebRequestMultimedia.GetAudioClip($"file://{musicPath}", AudioType.MPEG);
                    yield return request.SendWebRequest();
                    if (!string.IsNullOrEmpty(request.error))
                    {
                        Debug.LogWarning($"music load failed: {request.error}");
                        resolve();
                        yield break;
                    }

                    audioClip = DownloadHandlerAudioClip.GetContent(request);
                    resolve();
                }

                StartCoroutine(CO_LoadMusic());
            }), Async.CallbackToTask((resolve) =>
            {
                IEnumerator CO_LoadCover()
                {
                    using var request =
                        UnityWebRequestTexture.GetTexture($"file://{coverPath}");
                    yield return request.SendWebRequest();
                    if (!string.IsNullOrEmpty(request.error))
                    {
                        Debug.LogWarning($"cover load failed: {request.error}");
                        resolve();
                        yield break;
                    }

                    coverTexture = DownloadHandlerTexture.GetContent(request);
                    resolve();
                }

                StartCoroutine(CO_LoadCover());
            }), ((NFastPhiChart) chart).GenerateInternals());

            Debug.Log(
                $"convert pez to nfast chart + extract files + load cover and audio files: {stopwatch.ElapsedMilliseconds} ms");
        }

        async void GameBegin()
        {
            Debug.Log("game begin");
            linePool = new(() =>
            {
                var obj = Instantiate(JudgeLinePrefab);
                obj.name += " [Pooled]";
                obj.transform.localScale = ScaleVector3(obj.transform.localScale);
                return obj;
            }, 5);

            notePool = new(() =>
            {
                var obj = Instantiate(NotePrefab);
                obj.name += " [Pooled]";
                obj.GetComponent<PhiNoteWrapper>().Player = this;
                return obj;
            }, 5);

            // start timer
            Timer = new SystemTimer();
            Timer.Reset();

            // get first bpm event
            bpmEventGenerator = chart.GetBpmEvents();
            bpmEventGenerator.MoveNext();
            nextBpmEvent = bpmEventGenerator.Current;

            // generate lines
            lines = chart.GetLines();
            for (int i = 0; i < lines.Count; i++)
            {
                var obj = linePool.RequestObject();
                lineObjects.Add(obj);
                obj.SetActive(true);
                obj.GetComponent<PhiLineWrapper>().Line = lines[i];
            }

            // notes
            notes = chart.GetNotes();

            // events
            eventsGenerator = chart.GetEvents();

            // enable services and threads
            gameRunning = true;

            dispatcher.OnException += Debug.LogException;
            dispatcher.Start();
            dispatcher.Dispatch(FetchNewEvents);
        }

        private IList<LineEvent>[] eventsChunks = new IList<LineEvent>[24];
        private int eventsChunksBegin = 0;

        private void BeatUpdate()
        {
            // add new line events
            // wait for thread
            if (eventsChunks[eventsChunksBegin] == null)
            {
                Debug.Log("Cannot keep up: EventsProducer");
                dispatcher.Dispatch(FetchNewEvents);
            }
            while (eventsChunks[eventsChunksBegin] == null)
            {
            }

            runningEvents.AddRange(eventsChunks[eventsChunksBegin]);
            eventsChunks[eventsChunksBegin] = null;
            eventsChunksBegin++;
            if (eventsChunksBegin == eventsChunks.Length)
            {
                eventsChunksBegin = 0;
            }

            dispatcher.Dispatch(FetchNewEvents);
        }

       
        private void DoLineEvent(LineEvent lineEvent, float beat)
        {
            var last = lineEvent.EndTime.Beats - lineEvent.BeginTime.Beats;
            var easingX = (beat - lineEvent.BeginTime.Beats) / last;
            easingX = Mathf.Clamp(easingX, 0, 1);
            var easingY = EasingFunctions.Invoke(
                lineEvent.EasingFunc, easingX
                , lineEvent.EasingFuncRange.Low,
                lineEvent.EasingFuncRange.High);
            var value = lineEvent.BeginValue + (lineEvent.EndValue - lineEvent.BeginValue) * easingY;

            var lineId = (int) lineEvent.LineId;
            var lineObj = lineObjects[lineId];


            switch (lineEvent.Type)
            {
                case EventType.Alpha:
                {
                    var renderer = lineObj.GetComponent<PhiLineWrapper>().LineBody;
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
                    lines[lineId].Rotation = -value / 180f * MathF.PI;
                    break;
                }
                case EventType.Incline:
                    break;
            }
        }

        private void ProcessLineEvents()
        {
            for (int i = 0; i < runningEvents.Length; i++)
            {
                var item = runningEvents[i];
                if (item.BeginTime.Beats > CurrentBeats) continue;
                if (CurrentBeats > item.EndTime.Beats)
                {
                    runningEvents.RemoveAt(i);
                }

                DoLineEvent(item, CurrentBeats);
            }
        }

        private void UpdateLineHeight()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var newYPos = line.FindYPos(CurrentBeats);
                line.YPosition = newYPos;
            }
        }

        private void FetchNewNotes()
        {
            for (int i = notesBegin; i < notes.Count; i++)
            {
                var note = notes[i]; 
                var yOffset = note.YPosition - lines[(int) note.LineId].YPosition;
                note.Height = yOffset;
                if (yOffset <= 25f && yOffset >= 0)
                {
                    newNotes.Enqueue(note);
                    (notes[i], notes[notesBegin]) = (notes[notesBegin], null);
                    notesBegin++;
                }
            }
        }

        public void OnNoteFinalize(PhiNoteWrapper note)
        {
            notePool.ReturnObject(note.gameObject);
        }

        private void FetchNewEvents()
        {
            for (int i = 0; i < eventsChunks.Length; i++)
            {
                if (eventsChunks[i] != null) continue;
                if (!eventsGenerator.MoveNext()) eventsChunks[i] = new List<LineEvent>();
                else eventsChunks[i] = eventsGenerator.Current;
            }
        }
        
        private void GameTick()
        {
            if (currentBpm != 0f) CurrentBeats = Timer.Time / 1000f / beatLast;
            if (nextBpmEvent.Key.Beats <= CurrentBeats)
            {
                currentBpm = nextBpmEvent.Value;
                nextBpmEvent = bpmEventGenerator.MoveNext()
                    ? bpmEventGenerator.Current
                    : new(new(float.PositiveInfinity), 0f);
                beatLast = 60f / currentBpm;
            }

            currentBeatCount = (int) CurrentBeats;
            if (currentBeatCount > lastBeatCount)
            {
                for (int i = 0; i < currentBeatCount - lastBeatCount; i++)
                {
                    BeatUpdate();
                }

                lastBeatCount = currentBeatCount;
            }

            ProcessLineEvents();
            dispatcher.Dispatch(UpdateLineHeight);
            dispatcher.Dispatch(FetchNewNotes);

            var touchCount = Input.touchCount;
            for (int i = 0; i < touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                ProcessTouch(touch);
            }
        }

        private Vector2 GetFloor(Vector2 lineOrigin, float rotation, Vector2 touchPos)
        {
            if (rotation % MathF.PI == 0f) rotation = 0.0001f;
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
            switch (rawTouch.phase)
            {
                case TouchPhase.Began:
                {
                    var worldPos = Camera.main.ScreenToWorldPoint(rawTouch.position);
                    foreach (var chartLine in lines)
                    {
                        var linePos = lineObjects[(int) chartLine.LineId].transform.position;
                        var floorPos = GetFloor(linePos, chartLine.Rotation, worldPos);
                        Debug.Log(
                            $"line {chartLine.LineId} {Vector2.Distance(linePos, floorPos)}");
                    }
                    break;
                }
                case TouchPhase.Moved:
                    break;
                case TouchPhase.Stationary:
                    break;
                case TouchPhase.Ended:
                    break;
                case TouchPhase.Canceled:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void Update()
        {
            if (gameRunning) GameTick();
            lock (newNotes)
            {
                while (newNotes.Count > 0)
                {
                    var note = newNotes.Dequeue();
                    var noteObj = note.NoteGameObject;
                    if (noteObj == null)
                    {
                        noteObj = note.NoteGameObject = notePool.RequestObject();
                        noteObj.SetActive(true);

                        var lineWrapper =
                            lineObjects[(int) note.LineId].GetComponent<PhiLineWrapper>();

                        noteObj.transform.parent = note.ReverseDirection
                            ? lineWrapper.DownNoteViewport
                            : lineWrapper.UpNoteViewport;

                        var localPos = noteObj.transform.localPosition;
                        localPos.y = note.YPosition - lines[(int) note.LineId].YPosition;
                        localPos.x = ToGameXPos(note.XPosition);
                        noteObj.transform.localPosition = localPos;

                        var noteWrapper = noteObj.GetComponent<PhiNoteWrapper>();
                        noteWrapper.NoteStart(note, lineWrapper);
                    }
                }
            }
        }
    }
}