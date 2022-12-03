using System;
using System.Collections;
using System.Collections.Generic;
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
using SixLabors.ImageSharp.Formats.Bmp;
using Image = SixLabors.ImageSharp.Image;

public class GamePlayer : MonoBehaviour
{
    private const float ASPECT_RATIO = 16f / 9f;
    public Transform BackgroundTransform;
    public SpriteRenderer[] BackgroundImages;
    private float scaleFactor = 1f;
    public RawImage BgImage;
    async void Start()
    {
        // load chart file
        var loadInstruction = NavigationService.Get().ExtraData as string;
        if (loadInstruction == null) throw new InvalidOperationException("failed to load: unknown");

        SetupScreenScale();
        BackgroundTransform.localScale = ScaleVector3(BackgroundTransform.localScale);

        await LoadChart(loadInstruction);
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
    async Task LoadChart(string filePath)
    {
        // TODO: support pez only now
        PezRoot pezChart = null;
        Chart chart = null;
        
        await Async.RunOnThread(() =>
        {
            pezChart = PezLoader.LoadPezChart(filePath);
        });
        
        byte[] coverData = null;
        var (coverWidth, coverHeight) = (0, 0);
        await Task.WhenAll(Async.RunOnThread(() =>
            {
                chart = pezChart.ToChart();
            }),
            Async.RunOnThread(() =>
            {
                var imageBytes = PezLoader.ExtractFile(pezChart, pezChart.Metadata.Background);
                var image = Image.Load(imageBytes);
                var coverMemoryStream = new MemoryStream();
                image.Save(coverMemoryStream, new BmpEncoder {});
                // from https://forum.unity.com/threads/texture2d-loadimage-too-slow-anyway-to-use-threading-to-speed-it-up.442622/
                byte[] bytBMP = coverMemoryStream.ToArray();
                int intPointer = 51;
                MemoryStream msTx = new MemoryStream(bytBMP, 51, image.Width * image.Height * 3);
                (coverWidth, coverHeight) = (image.Width, image.Height);
                coverData = msTx.ToArray();
            }));

        Texture2D cover = new Texture2D(coverWidth, coverHeight, TextureFormat.RGB24, false);
        cover.LoadRawTextureData(coverData);
        cover.Apply();

        BgImage.texture = cover;
    }

    void GameBegin()
    {

    }

    void Update()
    {
        
    }
}
