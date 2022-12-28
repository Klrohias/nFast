﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
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
        private static readonly Regex PezChartLocate = new Regex("Chart:([0-9a-zA-Z ._]+)");
        private static readonly Regex PezPathLocate = new Regex("Path:([0-9a-zA-Z ._]+)");
        private const string NFAST_CHART = "_CHART_";
        private static string LocateRegExp(Regex regex, string str)
        {
            var locate = regex.Matches(str);
            if (locate.Count == 0) return null;
            return locate[0].Groups[1].Value.Trim();
        }

        private static async Task<LoadResult> LoadPezChart(LoadContext context)
        {
            var result = new LoadResult();
            var zipFile = new ZipFile(context.Path);
            var resourceProvider = new ZipResourceProvider(null, zipFile);

            // parse info.txt
            var infoStream = await resourceProvider.GetStreamResource(PEZ_INFO);
            var content = await new StreamReader(infoStream).ReadToEndAsync();
            infoStream.Close();

            var path = LocateRegExp(PezPathLocate, content);
            var chartPath = LocateRegExp(PezChartLocate, content);

            // load chart
            var chartStream = await resourceProvider.GetStreamResource(chartPath);
            var chart = JsonConvert.DeserializeObject<PezChart>(
                await new StreamReader(chartStream).ReadToEndAsync());
            chartStream.Close();

            if (chart == null)
                throw new NullReferenceException("Failed to load Pez chart");
                    
            var cachePath = Path.Combine(context.CachePath, path);
            if (!Directory.Exists(cachePath)) Directory.CreateDirectory(cachePath);

            resourceProvider.CachePath = cachePath;
            result.ResourceProvider = resourceProvider;
            result.Chart = chart.ToNFastChart();

            return result;
        }

        private static async Task<LoadResult> LoadNFastChart(LoadContext context)
        {
            var result = new LoadResult();
            var zipFile = new ZipFile(context.Path);
            var resourceProvider = new ZipResourceProvider(null, zipFile);

            var stream = await resourceProvider.GetStreamResource(NFAST_CHART);
            var chart = await MemoryPackSerializer.DeserializeAsync<PhiChart>(stream);

            if (chart == null) throw new NullReferenceException("Failed to deserialize NFast chart");

            var cachePath = Path.Combine(context.Path, chart.Metadata.ChartId);
            if (!Directory.Exists(cachePath)) Directory.CreateDirectory(cachePath);
            resourceProvider.CachePath = cachePath;
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
                case ".pez": return LoadPezChart(ctx);
                case ".nfp": return LoadNFastChart(ctx);
            }
            throw new ArgumentOutOfRangeException($"Unknown file type '{ext ?? "undefined"}");
        }
    }
}