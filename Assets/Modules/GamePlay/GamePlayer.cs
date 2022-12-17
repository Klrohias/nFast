using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;
using Klrohias.NFast.ChartLoader;
using Klrohias.NFast.ChartLoader.Pez;
using Klrohias.NFast.Navigation;
using Klrohias.NFast.Utilities;
using UnityEngine;
using Klrohias.NFast.ChartLoader.LargePez;
using Klrohias.NFast.ChartLoader.NFast;
using Klrohias.NFast.GamePlay;
using Klrohias.NFast.Native;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class GamePlayer : MonoBehaviour
{
    private bool gameRunning = false;
    private SystemTimer timer = null;

    // game running
    private int currentBeatCount = 0;
    private int lastBeatCount = -1;
    private float currentBeats = 0f;
    private KeyValuePair<ChartTimespan, float> nextBpmEvent;
    private float currentBpm = 0f;
    private float beatLast = 0f;
    private IEnumerator<KeyValuePair<ChartTimespan, float>> bpmEventGenerator;

    // events
    private List<LineEvent> runningEvents = new List<LineEvent>();
    

    // screen adaption
    private const float ASPECT_RATIO = 16f / 9f;
    private float scaleFactor = 1f;

    // unity resources
    public Transform BackgroundTransform;
    public SpriteRenderer[] BackgroundImages;
    public GameObject JudgeLinePrefab;
    private Texture2D coverTexture = null;
    private AudioClip audioClip = null;

    // object pools
    private ObjectPool linePool;
    private ObjectPool notePool;
    private ObjectPool holdNotePool;

    // chart data
    private IChart chart;
    private IList<ChartLine> lines;
    private List<GameObject> lineObjects = new List<GameObject>();

    // services
    private TouchService mainTouchService;

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

        mainTouchService = TouchService.Get();

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
                    LargePezLoader.ExtractFile((LargePezChart)chart, chart.Metadata.BackgroundFileName, coverStream);
                    coverStream.Flush();
                    coverStream.Close();
                }),
                Async.RunOnThread(() =>
                {
                    var musicStream = File.OpenWrite(musicPath);
                    LargePezLoader.ExtractFile((LargePezChart)chart, chart.Metadata.MusicFileName, musicStream);
                    musicStream.Flush();
                    musicStream.Close();
                }));
        }
        else
        {
            PezChart pezChart = null;
            await Async.RunOnThread(() =>
            {
                chart = pezChart = PezLoader.LoadPezChart(filePath);
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
        }));

        Debug.Log($"convert pez to nfast chart + extract files + load cover and audio files: {stopwatch.ElapsedMilliseconds} ms");
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

        // start timer
        timer = new SystemTimer();
        timer.Reset();

        // get first bpm event
        bpmEventGenerator = chart.GetBpmEvents();
        bpmEventGenerator.MoveNext();
        nextBpmEvent = bpmEventGenerator.Current;

        // enable services and threads
        mainTouchService.enabled = true;
        gameRunning = true;
        Threading.RunNewThread(NotesProducer);
        Threading.RunNewThread(EventsProducer);
        requireNewNotesChunk = true;
        requireNewEventsChunk = true;

        // generate lines
        lines = chart.GetLines();
        for (int i = 0; i < lines.Count; i++)
        {
            var obj = linePool.RequestObject();
            lineObjects.Add(obj);
            obj.SetActive(true);
        }
    }
    
    private IList<ChartNote>[] notesChunks = new IList<ChartNote>[4];
    private int notesChunksBegin = 0;
    private bool requireNewNotesChunk = false;
    private void NotesProducer()
    {
        var generator = chart.GetNotes();
        var emptyList = new List<ChartNote>();
        while (gameRunning)
        {
            Threading.WaitUntil(() => requireNewNotesChunk, 100);
            for (int i = 0; i < notesChunks.Length; i++)
            {
                if (notesChunks[i] != null) continue;

                if (!generator.MoveNext()) notesChunks[i] = emptyList;
                else notesChunks[i] = generator.Current;
            }
            requireNewNotesChunk = false;
        }
    }
    private IList<LineEvent>[] eventsChunks = new IList<LineEvent>[4];
    private int eventsChunksBegin = 0;
    private bool requireNewEventsChunk = false;
    private void EventsProducer()
    {
        var generator = chart.GetEvents();
        var emptyList = new List<LineEvent>();
        while (gameRunning)
        {
            Threading.WaitUntil(() => requireNewEventsChunk, 100);
            for (int i = 0; i < eventsChunks.Length; i++)
            {
                if (eventsChunks[i] != null) continue;
                if (!generator.MoveNext()) eventsChunks[i] = emptyList;
                else eventsChunks[i] = generator.Current;
            }
            requireNewEventsChunk = false;
        }
    }
    private void BeatUpdate()
    {
        // add new line events
        // TODO: don't use List<> for runningEvents
        // wait for thread
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
        requireNewEventsChunk = true;
    }

    private void ProcessLineEvents()
    {

    }
    private void GameTick()
    {
        if (currentBpm != 0f) currentBeats = timer.Time / 1000f / beatLast;
        if (nextBpmEvent.Key.Beats <= currentBeats)
        {
            currentBpm = nextBpmEvent.Value;
            nextBpmEvent = bpmEventGenerator.MoveNext()
                ? bpmEventGenerator.Current
                : new(new(float.PositiveInfinity), 0f);
            beatLast = 60f / currentBpm;
        }

        currentBeatCount = (int) currentBeats;
        if (currentBeatCount > lastBeatCount)
        {
            for (int i = 0; i < currentBeatCount - lastBeatCount; i++)
            {
                BeatUpdate();
            }
            lastBeatCount = currentBeatCount;
        }

        ProcessLineEvents();
    }
    void Update()
    {
        if (gameRunning) GameTick();
    }
}
