using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Klrohias.NFast.ChartLoader.NFast;
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
            var cacheName = chartName + ".NFAST_INDEX";
            var useCache = files.ContainsKey(cacheName);
            // extract
            Task.WaitAll(files.Select((x) =>
            {
                if (File.Exists(Path.Combine(cacheDir, x.Key))) return Task.CompletedTask;
                async Task CopyTask()
                {
                    var stream = zipFile.GetInputStream(x.Value);
                    var outStream = File.OpenWrite(Path.Combine(cacheDir, x.Key));
                    await stream.CopyToAsync(outStream);
                    outStream.Close();
                }
                return CopyTask();
            }).ToArray());

            var chartStream = File.Open(Path.Combine(cacheDir, chartName), FileMode.Open, FileAccess.ReadWrite);
            var tokenizer = new JsonTokenizer(chartStream);


            #region Experimental
            var s2 = Stopwatch.StartNew();
            if (!files.ContainsKey(LargeChartName))
            {
                var largeChartPath = Path.Combine(cacheDir, LargeChartName);
                var fileStream = File.OpenWrite(largeChartPath);
                var writer = new StreamWriter(fileStream);
                try
                {
                    while (true)
                    {
                        writer.Write(tokenizer.ParseNextToken().ToString());
                    }
                }
                catch(EndOfStreamException)
                {
                }
                writer.Flush();
                fileStream.Close();
                zipFile.BeginUpdate();
                zipFile.Add(new StaticDiskDataSource(largeChartPath), LargeChartName);
                zipFile.CommitUpdate();
                tokenizer.Goto(0);
            }
            Debug.Log("generate compressed chart: " + s2.ElapsedMilliseconds + "ms");
            #endregion

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

            void ExtractMeta()
            {
                walker.EnterBlock();
                var obj = new PezMetadata();
                walker.ExtractObject(typeof(PezMetadata), obj);
                walker.LeaveBlock();
                chart.metadata = obj.ToNFastMetadata();
            }

            void PutNoteOffset(uint beatIndex, long offset)
            {
                List<long> offsets = null;
                if (!chart.offsetMap.ContainsKey(beatIndex))
                {
                    offsets = new List<long>(32);
                    chart.offsetMap[beatIndex] = offsets;
                } else offsets = chart.offsetMap[beatIndex];
                offsets.Add(offset);
            }

            void AnalyzeNotes()
            {
                walker.EnterBlock();
                foreach (var note in walker.ReadElements())
                {
                    walker.EnterBlock();
                    foreach (var property in walker.ReadProperties())
                    {
                        if (property.Key.Value != "startTime") continue;
                        var first = int.Parse(walker.NextToken().Value);
                        tokenizer.ParseNextToken();
                        var second = int.Parse(walker.NextToken().Value);
                        tokenizer.ParseNextToken();
                        var third = int.Parse(walker.NextToken().Value);
                        var time = new ChartTimespan(first, second, third);
                        var beatIndex = (uint)Math.Floor(time.Beats);
                        PutNoteOffset(beatIndex, note.Position);
                    }
                    // walker.LeaveBlock();
                    // ReadElement requires current is the end of child block, don't LeaveBlock
                }
                walker.LeaveBlock();
            }

            void ExtractEventLayers()
            {

            }

            void ExtractJudgeLine()
            {
                walker.EnterBlock();
                foreach (var element in walker.ReadElements())
                {
                    walker.EnterBlock();
                    foreach (var property in walker.ReadProperties())
                    {
                        switch (property.Key.Value)
                        {
                            case "notes":
                            {
                                if (useCache) continue;
                                AnalyzeNotes();
                                break;
                            }
                            case "eventLayers":
                            {
                                ExtractEventLayers();
                                break;
                            }
                        }
                    }
                }
                walker.LeaveBlock();
            }

            void ExtractBpmList()
            {
                // TODO
            }

            foreach (var keyValuePair in walker.ReadProperties())
            {
                switch (keyValuePair.Key.Value)
                {
                    case "META":
                    {
                        ExtractMeta();
                        break;
                    }
                    case "judgeLineList":
                    {
                        ExtractJudgeLine();
                        break;
                    }
                    case "BPMList":
                    {
                        ExtractBpmList();
                        break;
                    }
                }
            }

            Debug.Log("time: " + s1.ElapsedMilliseconds + "ms");
            var cachePath = Path.Combine(cacheDir, cacheName);
            s1.Restart();
            if (useCache)
            {
                var cacheFileStream = File.OpenRead(cachePath);
                byte[] longBuffer = new byte[8];
                byte[] intBuffer = new byte[4];

                uint NextInt()
                {
                    cacheFileStream.Read(intBuffer);
                    return (uint) (intBuffer[0] | (intBuffer[1] << 8) | (intBuffer[2] << 16) | (intBuffer[3] << 24));
                }

                long NextLong()
                {
                    cacheFileStream.Read(longBuffer);
                    var low = longBuffer[0] | (uint) (longBuffer[1] << 8) | (uint) (longBuffer[2] << 16) |
                              (uint) (longBuffer[3] << 24);
                    var high = longBuffer[4] | (uint) (longBuffer[5] << 8) |
                                          (uint) (longBuffer[6] << 16) |
                                          (uint) (longBuffer[7] << 24);
                    return (long) ((ulong) high << 32 | low);
                }

                var streamLength = cacheFileStream.Length;
                while (cacheFileStream.Position < streamLength)
                {
                    NextInt();
                    var key = NextInt();
                    var length = NextInt();
                    var list = new long[length];
                    for (int i = 0; i < length; i++)
                    {
                        list[i] = NextLong();
                    }

                    chart.offsetMap[key] = new List<long>(list);
                }
                
                cacheFileStream.Close();
            }
            Debug.Log("load cache: " + s1.ElapsedMilliseconds + "ms");


            #region Experimental

            // Generate cache
            s1.Restart();
            if (!files.ContainsKey(cacheName))
            {
                var cacheFileStream = File.Open(cachePath, FileMode.OpenOrCreate,
                    FileAccess.ReadWrite);
                foreach (var (key, value) in chart.offsetMap)
                {
                    uint size = (uint) (4 + 4 + value.Count * 8); // [key] [value.size] [value...]
                    cacheFileStream.Write(BitConverter.GetBytes(size)); // write entity.size
                    cacheFileStream.Write(BitConverter.GetBytes(key)); // write key
                    cacheFileStream.Write(BitConverter.GetBytes(value.Count)); // write value.size
                    foreach (var index in value) // write value...
                    {
                        cacheFileStream.Write(BitConverter.GetBytes(index));
                    }
                }

                cacheFileStream.Flush();
                cacheFileStream.Close();
                zipFile.BeginUpdate();
                zipFile.Add(new StaticDiskDataSource(cachePath), cacheName);
                zipFile.CommitUpdate();
            }

            Debug.Log("generate cache: " + s1.ElapsedMilliseconds + "ms");
            #endregion
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