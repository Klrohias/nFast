using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.SharpZipLib;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;
using Newtonsoft.Json;

namespace Klrohias.NFast.ChartLoader.Pez
{
    public class PezLoader
    {
        internal const string PEZ_INFO = "info.txt";
        internal static Regex chartLocate = new Regex("Chart:([0-9a-zA-Z ._]+)");

        internal static IEnumerable<ZipEntry> readZipEntries(ZipFile zipfile)
        {
            foreach (ZipEntry zipEntry in zipfile)
            {
                yield return zipEntry;
            }
        }
        public static PezChart LoadPezChart(string path)
        {
            // pez is a zip file
            // firstly, open zip
            var zipFile = new ZipFile(path);

            var files = readZipEntries(zipFile).Take(6).ToDictionary(x => x.Name);
            
            // read info.txt
            if (!files.ContainsKey(PEZ_INFO))
                throw new Exception("failed to load pez file: info.txt not exists");

            using var pezInfoStream = zipFile.GetInputStream(files[PEZ_INFO]);
            var info = new StreamReader(pezInfoStream).ReadToEnd();
            var locate = chartLocate.Matches(info);
            if (locate.Count == 0) throw new Exception("failed to load pez file: invalid info.txt");
            var chartName = locate[0].Groups[1].Value.Trim();

            // read chart
            if (!files.ContainsKey(chartName))
                throw new Exception("failed to load pez file: chart not found");

            using var pezChartStream = zipFile.GetInputStream(files[chartName]);
            var chart = JsonConvert.DeserializeObject<PezChart>(new StreamReader(pezChartStream).ReadToEnd());

            chart.zipFile = zipFile;
            chart.files = files;

            return chart;
        }

        public static byte[] ExtractFile(PezChart root, string name)
        {
            if (!root.files.ContainsKey(name)) throw new ArgumentException("file not exists");
            var file = root.files[name];
            var result = new MemoryStream(Convert.ToInt32(file.Size));
            ExtractFile(root, name, result);
            return result.ToArray();
        }

        public static void ExtractFile(PezChart root, string name, Stream outStream)
        {
            if (!root.files.ContainsKey(name)) throw new ArgumentException("file not exists");
            var file = root.files[name];

            using var stream = root.zipFile.GetInputStream(file);
            stream.CopyTo(outStream);
            stream.Close();
        }
    }
}