using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Klrohias.NFast.ChartLoader.Pez;
using Debug = UnityEngine.Debug;

namespace Klrohias.NFast.ChartLoader.LargePez
{
    public class LargePezLoader
    {
        private const string LargeChartName = "NFAST_LARGE.json";
        public static LargePezChart Load(string path, string cacheDir)
        {
            var zipFile = new ZipFile(path);

            var files = PezLoader.readZipEntries(zipFile).Take(6).ToDictionary(x => x.Name);

            // read info.txt
            if (!files.ContainsKey(PezLoader.PEZ_INFO))
                throw new Exception("failed to load pez file: info.txt not exists");
            var chartName = LargeChartName;
            if (!files.ContainsKey(LargeChartName))
            {
                using var pezInfoStream = zipFile.GetInputStream(files[PezLoader.PEZ_INFO]);
                var info = new StreamReader(pezInfoStream).ReadToEnd();
                var locate = PezLoader.chartLocate.Matches(info);
                if (locate.Count == 0) throw new Exception("failed to load pez file: invalid info.txt");
                chartName = locate[0].Groups[1].Value.Trim();
            }

            // extract
            Task.WaitAll(files.Select((x) =>
            {
                async Task CopyTask()
                {
                    var stream = zipFile.GetInputStream(x.Value);
                    var outStream = File.OpenWrite(Path.Combine(cacheDir, x.Key));
                    await stream.CopyToAsync(outStream);
                    outStream.Close();
                }
                return CopyTask();
            }).ToArray());
            
            var chartStream = File.Open(Path.Combine(cacheDir, chartName), FileMode.Open);
            var tokenizer = new JsonTokenizer(chartStream);
            var walker = new JsonWalker(tokenizer);

            var chart = new LargePezChart()
            {
                zipFile = zipFile,
                tokenizer = tokenizer,
                walker = walker,
                files = files
            };

            walker.EnterBlock();
            var s1 = Stopwatch.StartNew();

            foreach (var keyValuePair in walker.ReadProperties())
            {
                switch (keyValuePair.Key.Value)
                {
                    case "META":
                    {
                        walker.EnterBlock();
                        var obj = new PezMetadata();
                        walker.ExtractObject(typeof(PezMetadata), obj);
                        walker.LeaveBlock();
                        chart.metadata = obj.ToNFastMetadata();
                        break;
                    }
                    
                }

                Debug.Log(keyValuePair.Key.Value);
            }

            Debug.Log("time: " + s1.ElapsedMilliseconds + "ms");
            return chart;
        }

        public static PezNote ExtractNote(LargePezChart chart, long offset)
        {
            var tokenizer = chart.tokenizer;
            var walker = chart.walker;
            tokenizer.Goto(offset);

            walker.EnterBlock();

            walker.LeaveBlock();
            throw new NotImplementedException();
        }
        /*
         *  if (!files.ContainsKey(LargeChartName))
         *  {
         *      var largeChartPath = Path.Combine(cacheDir, LargeChartName);
         *      var fileStream = File.OpenWrite(largeChartPath);
         *      var writer = new StreamWriter(fileStream);
         *      try
         *      {
         *          while (true)
         *          {
         *              writer.Write(tokenizer.ParseNextToken().ToString());
         *          }
         *      }
         *      catch(EndOfStreamException)
         *      {
         *      }
         *      writer.Flush();
         *      fileStream.Close();
         *      zipFile.BeginUpdate();
         *      zipFile.Add(new StaticDiskDataSource(largeChartPath), LargeChartName);
         *      zipFile.CommitUpdate();
         *      tokenizer.Goto(0);
         *  }
         */

        public static byte[] ExtractFile(LargePezChart root, string name)
        {
            if (!root.files.ContainsKey(name)) throw new ArgumentException("file not exists");
            var file = root.files[name];
            var result = new MemoryStream(Convert.ToInt32(file.Size));
            ExtractFile(root, name, result);
            return result.ToArray();
        }

        public static void ExtractFile(LargePezChart root, string name, Stream outStream)
        {
            if (!root.files.ContainsKey(name)) throw new ArgumentException("file not exists");
            var file = root.files[name];

            using var stream = root.zipFile.GetInputStream(file);
            stream.CopyTo(outStream);
            stream.Close();
        }
    }
}