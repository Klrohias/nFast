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
using UnityEngine.UI;
using System.Drawing;
using Klrohias.NFast.ChartLoader.LargePez;
using Klrohias.NFast.GamePlay;
using Klrohias.NFast.Native;
using SixLabors.ImageSharp.Formats.Bmp;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using Image = SixLabors.ImageSharp.Image;

public class GamePlayer : MonoBehaviour
{
    private const float ASPECT_RATIO = 16f / 9f;
    public Transform BackgroundTransform;
    public SpriteRenderer[] BackgroundImages;
    private float scaleFactor = 1f;
    private Queue<Action> runOnMainThreadQueue = new();
    private ObjectPool linePool;
    private ObjectPool notePool;
    private ObjectPool holdNotePool;
    public GameObject JudgeLinePrefab;
    private bool gameRunning = false;
    private IChart chart;

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

    private Texture2D coverTexture = null;
    private AudioClip audioClip = null;
    async Task LoadChart(string filePath, bool useLargeChart = false)
    {
        var cachePath = OSService.Get().CachePath;
        if (useLargeChart)
        {
            await Async.RunOnThread(() =>
            {
                var largePezChart = LargePezLoader.Load(filePath, cachePath);
            });
            return;
        }
        // TODO: support pez only now
        PezRoot pezChart = null;
        NFastChart chart = null;
        
        Stopwatch stopwatch = Stopwatch.StartNew();
        await Async.RunOnThread(() =>
        {
            pezChart = PezLoader.LoadPezChart(filePath);
        });
        Debug.Log($"load pez chart: {stopwatch.ElapsedMilliseconds} ms");
        
        stopwatch.Restart();
        var coverPath = Path.Combine(cachePath, pezChart.Metadata.Background);
        var musicPath = Path.Combine(cachePath, pezChart.Metadata.Song);
        bool coverExtracted = false;
        bool audioExtracted = false;
        await Task.WhenAll(Async.RunOnThread(() =>
            {
                chart = pezChart.ToChart();
            }),
            Async.RunOnThread(() =>
            {
                var coverStream = File.OpenWrite(coverPath);
                PezLoader.ExtractFile(pezChart, pezChart.Metadata.Background, coverStream);
                coverStream.Flush();
                coverStream.Close();
                coverExtracted = true;
            }),
            Async.RunOnThread(() =>
            {
                var musicStream = File.OpenWrite(musicPath);
                PezLoader.ExtractFile(pezChart, pezChart.Metadata.Background, musicStream);
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

    private TouchService mainTouchService;
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
        
        mainTouchService.enabled = true;
        gameRunning = true;
        Threading.RunNewThread(EventsProducer);
    }

    private LineEvent[] lineEvents = null;
    private int lineEventsBegin = 0;
    private LineEvent[][] lineEventsChucks = new LineEvent[4][];
    private int eventsChuckBegin = 0;
    private void EventsProducer()
    {
        while (gameRunning)
        {

        }
    }

    private Task runOnMainThread(Func<Task> a)
    {
        return Async.CallbackToTask((resolve) =>
        {
            lock (runOnMainThreadQueue)
            {
                runOnMainThreadQueue.Enqueue(async () =>
                {
                    await a();
                    resolve();
                });
            }
        });
    }

    void GameTick()
    {

    }
    void Update()
    {
        while (runOnMainThreadQueue.Count > 0)
            runOnMainThreadQueue.Dequeue()();

        if (gameRunning) GameTick();
    }
}
