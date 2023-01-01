using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Klrohias.NFast.Resource;
using Newtonsoft.Json;
using MemoryPack;

namespace Klrohias.NFast.PhiChartLoader
{
    public static class ChartLoader
    {
        public class LoadResult
        {
            public IResourceProvider ResourceProvider;
            public PhiChart Chart;
        }
        private struct LoadContext
        {
            public string Path;
            public string CachePath;
        }

        private const string PEZ_INFO = "info.txt";
        private static readonly Regex PezNameLocate = new Regex("Name:([^\\n]+)\\n");
        private static readonly Regex PezSongLocate = new Regex("Song:([^\\n]+)\\n");
        private static readonly Regex PezPictureLocate = new Regex("Picture:([^\\n]+)\\n");
        private static readonly Regex PezLevelLocate = new Regex("Level:([^\\n]+)\\n");
        private static readonly Regex PezComposerLocate = new Regex("Composer:([^\\n]+)\\n");
        private static readonly Regex PezCharterLocate = new Regex("Charter:([^\\n]+)\\n");
        private static readonly Regex PezChartLocate = new Regex("Chart:([0-9a-zA-Z ._]+)");
        private static readonly Regex PezPathLocate = new Regex("Path:([0-9a-zA-Z ._]+)");
        private const string NFAST_CHART = "_CHART_";
        private static string LocateRegExp(Regex regex, string str)
        {
            var locate = regex.Matches(str);
            if (locate.Count == 0) return null;
            return locate[0].Groups[1].Value.Trim();
        }

        private static async Task<PhiChart> ParseZippedChartPayload(Stream stream, bool isJsonChart)
        {
            if (isJsonChart)
            {
                // pez json
                var chart = JsonConvert.DeserializeObject<PezChart>(
                    await new StreamReader(stream).ReadToEndAsync());
                return chart?.ToNFastChart();
            }

            // pec
            var parser = new PecParser(stream);
            await parser.ParsePhiChart();
            var result = parser.Result;
            return result;
        }

        private static ChartMetadata LoadMetadataFromInfo(string infoContent)
        {
            var result = new ChartMetadata();
            result.Name = LocateRegExp(PezNameLocate, infoContent);
            result.Charter = LocateRegExp(PezCharterLocate, infoContent);
            result.Level = LocateRegExp(PezLevelLocate, infoContent);
            result.Composer = LocateRegExp(PezComposerLocate, infoContent);
            result.BackgroundFileName = LocateRegExp(PezPictureLocate, infoContent);
            result.MusicFileName = LocateRegExp(PezSongLocate, infoContent);
            return result;
        }

        private static async Task<LoadResult> LoadZippedChart(LoadContext context)
        {
            var result = new LoadResult();
            var zipFile = ZipFile.OpenRead(context.Path);
            var resourceProvider = new ZipResourceProvider(null, zipFile);

            // parse info.txt
            var infoStream = await resourceProvider.GetStreamResource(PEZ_INFO);
            var content = await new StreamReader(infoStream).ReadToEndAsync();
            infoStream.Close();

            var path = LocateRegExp(PezPathLocate, content);
            var chartPath = LocateRegExp(PezChartLocate, content);

            // load chart
            var chartStream = await resourceProvider.GetStreamResource(chartPath);
            var isJsonChart = chartStream.ReadByte() == '{'; 
            chartStream.Close(); // DeflateStream does not support Seek
            chartStream = await resourceProvider.GetStreamResource(chartPath);
            var chart = await ParseZippedChartPayload(chartStream, isJsonChart);
            if (chart.Metadata == null) chart.Metadata = LoadMetadataFromInfo(content);
            chartStream.Close();

            if (chart == null)
                throw new NullReferenceException("Failed to load Zipped chart");

            if (!string.IsNullOrWhiteSpace(context.CachePath))
            {
                var cachePath = Path.Combine(context.CachePath, path);
                if (!Directory.Exists(cachePath)) Directory.CreateDirectory(cachePath);

                resourceProvider.CachePath = cachePath;
            }

            result.ResourceProvider = resourceProvider;
            result.Chart = chart;

            return result;
        }

        private static async Task<LoadResult> LoadNFastChart(LoadContext context)
        {
            var result = new LoadResult();
            var zipFile = ZipFile.OpenRead(context.Path);
            var resourceProvider = new ZipResourceProvider(null, zipFile);

            var stream = await resourceProvider.GetStreamResource(NFAST_CHART);
            var chart = await MemoryPackSerializer.DeserializeAsync<PhiChart>(stream);

            if (chart == null) throw new NullReferenceException("Failed to deserialize NFast chart");

            if (!string.IsNullOrWhiteSpace(context.CachePath))
            {
                var cachePath = Path.Combine(context.CachePath, chart.Metadata.ChartId);
                if (!Directory.Exists(cachePath)) Directory.CreateDirectory(cachePath);
                resourceProvider.CachePath = cachePath;
            }

            result.ResourceProvider = resourceProvider;
            result.Chart = chart;

            return result;
        }
        public static Task<LoadResult> LoadChartAsync(string path, string cachePath = null)
        {
            var ext = Path.GetExtension(path);
            var ctx = new LoadContext
            {
                Path = path,
                CachePath = cachePath
            };
            // load by extension
            switch (ext)
            {
                case ".pez": return LoadZippedChart(ctx);
                case ".nfp": return LoadNFastChart(ctx);
                case ".zip": return LoadZippedChart(ctx);
                case ".pgm": return null; // in plan, not supported now
            }
            throw new ArgumentOutOfRangeException($"Unknown file type '{ext ?? "undefined"}");
        }

        public static async Task<string> ToNFastChart(string path,string cachePath, string outputPath)
        {
            var cacheOutput = Path.Combine(cachePath, NFAST_CHART);

            if (File.Exists(outputPath)) return outputPath;

            var loadResult = await LoadChartAsync(path, null);
            
            var cacheStream = File.OpenWrite(cacheOutput);
            await MemoryPackSerializer.SerializeAsync(cacheStream, loadResult.Chart);
            await cacheStream.FlushAsync();
            cacheStream.Close();

            File.Copy(path, outputPath);
            using var zipFile = ZipFile.Open(outputPath, ZipArchiveMode.Update);
            zipFile.CreateEntryFromFile(cacheOutput, NFAST_CHART, CompressionLevel.Optimal);

            File.Delete(cacheOutput);

            return outputPath;
        }
    }
}