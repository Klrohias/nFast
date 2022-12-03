using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Newtonsoft.Json;

namespace Klrohias.NFast.ChartLoader.Pez
{
    public class PezLoader
    {
        private const string PEZ_INFO = "info.txt";
        private static Regex chartLocate = new Regex("Chart:([0-9a-zA-Z .]+)");
        public static PezRoot LoadPezChart(string path)
        {
            // pez is a zip file
            // firstly, open zip
            var zFile = ZipFile.OpenRead(path);
            var files = zFile.Entries.Take(6).ToDictionary(x => x.FullName);
            
            // read info.txt
            if (!files.ContainsKey(PEZ_INFO))
                throw new Exception("failed to load pez file: info.txt not exists");

            var info = new StreamReader(files[PEZ_INFO].Open()).ReadToEnd();
            var locate = chartLocate.Matches(info);
            if (locate.Count == 0) throw new Exception("failed to load pez file: invalid info.txt");
            var chartName = locate[0].Groups[1].Value.Trim();

            // read chart
            if (!files.ContainsKey(chartName))
                throw new Exception("failed to load pez file: chart not found");

            var chart = JsonConvert.DeserializeObject<PezRoot>(new StreamReader(files[chartName].Open()).ReadToEnd());
            chart.files = files;

            return chart;
        }
    }
}