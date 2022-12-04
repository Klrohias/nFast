using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

namespace Klrohias.NFast.ChartLoader.Pez
{
    public class PezRoot
    {
        [JsonProperty("META")]
        public PezMetadata Metadata { get; set; } = null;

        internal ZipFile zipFile = null;
        internal Dictionary<string, ZipEntry> files = null;

        [JsonProperty("judgeLineList")]
        public List<PezJudgeLineList> JudgeLineList { get; set; }

        private static IEnumerable<ChartNote> PezCastToChartNotes(PezJudgeLineList judgeLine, uint lineId)
        {
            if(judgeLine.Notes == null) yield break;
            foreach (var note in judgeLine.Notes)
            {
                yield return new()
                {
                    LineId = lineId,
                    EndTime = new ChartTimespan(note.EndTime),
                    StartTime = new ChartTimespan(note.StartTime),
                    XPosition = note.PositionX
                };
            }
        }

        public Chart ToChart()
        {
            var countOfNotes = JudgeLineList.Sum(x => x.Notes?.Count ?? 0);
            var notesArray = new ChartNote[countOfNotes];
            var linesArray = new ChartLine[JudgeLineList.Count];
            uint lineId = 0;
            int noteIndex = 0;
            foreach (var judgeLine in JudgeLineList)
            {
                var line = linesArray[lineId] = judgeLine.ToChartLine();
                line.LineId = lineId;
                if (judgeLine.EventLayers != null)
                {
                    var mainLayer = judgeLine.EventLayers[0];
                    var events =
                        new LineEvent[(mainLayer.AlphaEvents?.Count ?? 0) + (mainLayer.MoveXEvents?.Count ?? 0) +
                                      (mainLayer.MoveYEvents?.Count ?? 0) + (mainLayer.RotateEvents?.Count ?? 0) +
                                      (mainLayer.SpeedEvents?.Count ?? 0)];
                }

                // cast notes
                foreach (var note in PezCastToChartNotes(judgeLine, lineId))
                {
                    notesArray[noteIndex] = note;
                    noteIndex++;
                }

                lineId++;
            }

            var chart = new Chart()
            {
                Metadata = Metadata.ToChartMetadata(),
                Notes = notesArray,
                Lines = linesArray
            };
            return chart;
        }

        public void DropZipData()
        {
            files = null;
            zipFile = null;
            GC.Collect();
        }
    }

    public class PezMetadata
    {
        [JsonProperty("RPEVersion")]
        public int RPEVersion { get; set; }

        [JsonProperty("background")]
        public string Background { get; set; }

        [JsonProperty("charter")]
        public string Charter { get; set; }

        [JsonProperty("composer")]
        public string Composer { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("song")]
        public string Song { get; set; }

        public ChartMetadata ToChartMetadata() => new()
        {
            BackgroundFileName = Background,
            Charter = Charter,
            Composer = Composer,
            Level = Level,
            MusicFileName = Song,
            Name = Name
        };
    }


    public class PezAlphaEvent
    {
        [JsonProperty("easingLeft")]
        public double EasingLeft { get; set; }

        [JsonProperty("easingRight")]
        public double EasingRight { get; set; }

        [JsonProperty("easingType")]
        public int EasingType { get; set; }

        [JsonProperty("end")]
        public int End { get; set; }

        [JsonProperty("endTime")]
        public List<int> EndTime { get; set; }

        [JsonProperty("linkgroup")]
        public int Linkgroup { get; set; }

        [JsonProperty("start")]
        public int Start { get; set; }

        [JsonProperty("startTime")]
        public List<int> StartTime { get; set; }
    }

    public class PezEventLayer
    {
        [JsonProperty("alphaEvents")]
        public List<PezAlphaEvent> AlphaEvents { get; set; }

        [JsonProperty("moveXEvents")]
        public List<PezMoveXEvent> MoveXEvents { get; set; }

        [JsonProperty("moveYEvents")]
        public List<PezMoveYEvent> MoveYEvents { get; set; }

        [JsonProperty("rotateEvents")]
        public List<PezRotateEvent> RotateEvents { get; set; }

        [JsonProperty("speedEvents")]
        public List<PezSpeedEvent> SpeedEvents { get; set; }
    }

    public class PezExtended
    {
        [JsonProperty("inclineEvents")]
        public List<PezInclineEvent> InclineEvents { get; set; }
    }

    public class PezInclineEvent
    {
        [JsonProperty("easingLeft")]
        public double EasingLeft { get; set; }

        [JsonProperty("easingRight")]
        public double EasingRight { get; set; }

        [JsonProperty("easingType")]
        public int EasingType { get; set; }

        [JsonProperty("end")]
        public double End { get; set; }

        [JsonProperty("endTime")]
        public List<int> EndTime { get; set; }

        [JsonProperty("linkgroup")]
        public int Linkgroup { get; set; }

        [JsonProperty("start")]
        public double Start { get; set; }

        [JsonProperty("startTime")]
        public List<int> StartTime { get; set; }
    }

    public class PezJudgeLineList
    {
        [JsonProperty("Group")]
        public int Group { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Texture")]
        public string Texture { get; set; }

        [JsonProperty("bpmfactor")]
        public double Bpmfactor { get; set; }

        [JsonProperty("eventLayers")]
        public List<PezEventLayer> EventLayers { get; set; }

        [JsonProperty("extended")]
        public PezExtended Extended { get; set; }

        [JsonProperty("father")]
        public int Father { get; set; }

        [JsonProperty("isCover")]
        public int IsCover { get; set; }

        [JsonProperty("notes")]
        public List<PezNote> Notes { get; set; }

        [JsonProperty("numOfNotes")]
        public int NumOfNotes { get; set; }

        [JsonProperty("zOrder")]
        public int ZOrder { get; set; }

        public ChartLine ToChartLine()
            => new ChartLine { };
    }

    public class PezMoveXEvent
    {
        [JsonProperty("easingLeft")]
        public double EasingLeft { get; set; }

        [JsonProperty("easingRight")]
        public double EasingRight { get; set; }

        [JsonProperty("easingType")]
        public int EasingType { get; set; }

        [JsonProperty("end")]
        public double End { get; set; }

        [JsonProperty("endTime")]
        public List<int> EndTime { get; set; }

        [JsonProperty("linkgroup")]
        public int Linkgroup { get; set; }

        [JsonProperty("start")]
        public double Start { get; set; }

        [JsonProperty("startTime")]
        public List<int> StartTime { get; set; }
    }

    public class PezMoveYEvent
    {
        [JsonProperty("easingLeft")]
        public double EasingLeft { get; set; }

        [JsonProperty("easingRight")]
        public double EasingRight { get; set; }

        [JsonProperty("easingType")]
        public int EasingType { get; set; }

        [JsonProperty("end")]
        public double End { get; set; }

        [JsonProperty("endTime")]
        public List<int> EndTime { get; set; }

        [JsonProperty("linkgroup")]
        public int Linkgroup { get; set; }

        [JsonProperty("start")]
        public double Start { get; set; }

        [JsonProperty("startTime")]
        public List<int> StartTime { get; set; }
    }

    public class PezNote
    {
        [JsonProperty("above")]
        public int Above { get; set; }

        [JsonProperty("alpha")]
        public int Alpha { get; set; }

        [JsonProperty("endTime")]
        public List<int> EndTime { get; set; }

        [JsonProperty("isFake")]
        public int IsFake { get; set; }

        [JsonProperty("positionX")]
        public float PositionX { get; set; }

        [JsonProperty("size")]
        public double Size { get; set; }

        [JsonProperty("speed")]
        public double Speed { get; set; }

        [JsonProperty("startTime")]
        public List<int> StartTime { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("visibleTime")]
        public double VisibleTime { get; set; }

        [JsonProperty("yOffset")]
        public double YOffset { get; set; }
    }

    public class PezRotateEvent
    {
        [JsonProperty("easingLeft")]
        public double EasingLeft { get; set; }

        [JsonProperty("easingRight")]
        public double EasingRight { get; set; }

        [JsonProperty("easingType")]
        public int EasingType { get; set; }

        [JsonProperty("end")]
        public double End { get; set; }

        [JsonProperty("endTime")]
        public List<int> EndTime { get; set; }

        [JsonProperty("linkgroup")]
        public int Linkgroup { get; set; }

        [JsonProperty("start")]
        public double Start { get; set; }

        [JsonProperty("startTime")]
        public List<int> StartTime { get; set; }
    }

    public class PezSpeedEvent
    {
        [JsonProperty("end")]
        public double End { get; set; }

        [JsonProperty("endTime")]
        public List<int> EndTime { get; set; }

        [JsonProperty("linkgroup")]
        public int Linkgroup { get; set; }

        [JsonProperty("start")]
        public double Start { get; set; }

        [JsonProperty("startTime")]
        public List<int> StartTime { get; set; }
    }
}