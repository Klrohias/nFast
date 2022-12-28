using System;
using System.Collections.Generic;
using System.Linq;
using Klrohias.NFast.Utilities;
using Newtonsoft.Json;

namespace Klrohias.NFast.PhiChartLoader
{
    public class PezChart
    {
        [JsonProperty("META")]
        public PezMetadata PezMetadata { get; set; } = null;

        [JsonProperty("judgeLineList")]
        public List<PezJudgeLine> JudgeLineList { get; set; }

        [JsonProperty("BPMList")]
        public List<PezBpmEvent> BpmEvents { get; set; }
        
        public PhiChart ToNFastChart()
        {
            // convert bpm events
            var bpmEvents = BpmEvents.Select(x => x.ToNFastEvent())
                .OrderBy(x => x.Key).ToList();

            // TODO: decouple event converting

            // convert lines/notes
            var countOfNotes = JudgeLineList.Sum(x => x.Notes?.Count ?? 0);
            var notesArray = new PhiNote[countOfNotes];
            var linesArray = new PhiLine[JudgeLineList.Count];
            var countOfEvents = JudgeLineList.Sum(x => x.EventLayers?.Sum(y => y?.EventCount ?? 0) ?? 0);
            var eventsArray = new LineEvent[countOfEvents];

            uint lineId = 0;
            var noteIndex = 0;
            var eventIndex = 0;
            foreach (var judgeLine in JudgeLineList)
            {
                var line = linesArray[lineId] = judgeLine.ToChartLine();
                line.LineId = lineId;

                // cast events
                var events = judgeLine.EventLayers.SelectMany(x => x?.ToNFastEvents(lineId) ?? new List<LineEvent>())
                    .ToArray();
                events.CopyTo(eventsArray, eventIndex);
                eventIndex += events.Length;

                // cast notes
                if (judgeLine.Notes != null)
                {
                    foreach (var note in judgeLine.Notes.Select(x => x.ToNFastNote(lineId)))
                    {
                        notesArray[noteIndex] = note;
                        noteIndex++;
                    }
                }

                lineId++;
            }
            
            // generate nfast chart
            var chart = new PhiChart()
            {
                Metadata = PezMetadata.ToNFastMetadata(),
                Notes = notesArray,
                Lines = linesArray,
                BpmEvents = bpmEvents,
                LineEvents = eventsArray
            };
            return chart;
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

        public ChartMetadata ToNFastMetadata() => new()
        {
            BackgroundFileName = Background,
            Charter = Charter,
            Composer = Composer,
            Level = Level,
            MusicFileName = Song,
            Name = Name
        };
    }
    public class PezBpmEvent
    {
        [JsonProperty("bpm")] public float Bpm;
        [JsonProperty("startTime")] public List<int> Time;

        public KeyValuePair<float, float> ToNFastEvent()
            => new(ChartTimespan.FromBeatsFraction(Time), Bpm);
    }
    public class PezLineEvent
    {
        [JsonProperty("easingLeft")]
        public float EasingLeft { get; set; }

        [JsonProperty("easingRight")]
        public float EasingRight { get; set; }

        [JsonProperty("easingType")]
        public int EasingType { get; set; }

        [JsonProperty("end")]
        public float End { get; set; }

        [JsonProperty("endTime")]
        public List<int> EndTime { get; set; }

        [JsonProperty("start")]
        public float Start { get; set; }

        [JsonProperty("startTime")]
        public List<int> StartTime { get; set; }

        private EasingFunction ToNFastEasingFunction()
        {
            switch (EasingType)
            {
                case 0: return EasingFunction.Linear;
                case 1: return EasingFunction.Linear;
                case 2: return EasingFunction.SineOut;
                case 3: return EasingFunction.SineIn;
                case 4: return EasingFunction.QuadOut;
                case 5: return EasingFunction.QuadIn;
                case 6: return EasingFunction.SineInOut;
                case 7: return EasingFunction.QuadInOut;
                case 8: return EasingFunction.CubicOut;
                case 9: return EasingFunction.CubicIn;
                case 10: return EasingFunction.QuartOut;
                case 11: return EasingFunction.QuartIn;
                case 12: return EasingFunction.CubicInOut;
                case 13: return EasingFunction.QuartInOut;
                case 14: return EasingFunction.QuintOut;
                case 15: return EasingFunction.QuintIn;
                case 16: return EasingFunction.ExpoOut;
                case 17: return EasingFunction.ExpoIn;
                case 18: return EasingFunction.CircOut;
                case 19: return EasingFunction.CircIn;
                case 20: return EasingFunction.BackOut;
                case 21: return EasingFunction.BackIn;
                case 22: return EasingFunction.CircInOut;
                case 23: return EasingFunction.BackInOut;
                case 24: return EasingFunction.ElasticOut;
                case 25: return EasingFunction.ElasticIn;
                case 26: return EasingFunction.BounceOut;
                case 27: return EasingFunction.BounceIn;
                case 28: return EasingFunction.BounceInOut;
                default: throw new Exception($"Unknown easing function {EasingType}");
            }
        }
        public LineEvent ToNFastEvent(uint lineId = 0, LineEventType type = LineEventType.Alpha)
        {
            return new LineEvent()
            {
                BeginTime = ChartTimespan.FromBeatsFraction(StartTime),
                BeginValue = Start,
                EasingFuncRange = (EasingLeft, EasingRight),
                EndTime = ChartTimespan.FromBeatsFraction(EndTime),
                EndValue = End,
                EasingFunc = ToNFastEasingFunction(),
                Type = type,
                LineId = lineId
            };
        }
    }
    public class PezEventLayer
    {
        [JsonProperty("alphaEvents")]
        public List<PezLineEvent> AlphaEvents { get; set; }

        [JsonProperty("moveXEvents")]
        public List<PezLineEvent> MoveXEvents { get; set; }

        [JsonProperty("moveYEvents")]
        public List<PezLineEvent> MoveYEvents { get; set; }

        [JsonProperty("rotateEvents")]
        public List<PezLineEvent> RotateEvents { get; set; }

        [JsonProperty("speedEvents")]
        public List<PezLineEvent> SpeedEvents { get; set; }

        public List<LineEvent> ToNFastEvents(uint lineId = 0)
        {
            var result = new List<LineEvent>();
            if (AlphaEvents != null)
            {
                result.AddRange(AlphaEvents.Select(x => x.ToNFastEvent(lineId, LineEventType.Alpha)));
            }
            if (MoveXEvents != null)
            {
                result.AddRange(MoveXEvents.Select(x => x.ToNFastEvent(lineId, LineEventType.MoveX)));
            }
            if (MoveYEvents != null)
            {
                result.AddRange(MoveYEvents.Select(x => x.ToNFastEvent(lineId, LineEventType.MoveY)));
            }
            if (RotateEvents != null)
            {
                result.AddRange(RotateEvents.Select(x => x.ToNFastEvent(lineId, LineEventType.Rotate)));
            }
            if (SpeedEvents != null)
            {
                result.AddRange(SpeedEvents.Select(x => { 
                    var lineEvent = x.ToNFastEvent(lineId, LineEventType.Speed);
                    lineEvent.EasingFuncRange = (0, 1f);
                    lineEvent.EasingFunc = EasingFunction.Linear;
                    return lineEvent;
                }));
            }
            return result;
        }

        public int EventCount =>
            (AlphaEvents?.Count ?? 0) + (MoveXEvents?.Count ?? 0)
                                      + (MoveYEvents?.Count ?? 0) + (RotateEvents?.Count ?? 0)
                                      + (SpeedEvents?.Count ?? 0);
    }

    public class PezExtended
    {
        [JsonProperty("inclineEvents")]
        public List<PezLineEvent> InclineEvents { get; set; }
    }
    public class PezJudgeLine
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

        public PhiLine ToChartLine()
            => new PhiLine { };
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

        public PhiNote ToNFastNote(uint lineId = 0)
        {
            return new()
            {
                LineId = lineId,
                EndTime = ChartTimespan.FromBeatsFraction(EndTime),
                BeginTime = ChartTimespan.FromBeatsFraction(StartTime),
                XPosition = PositionX,
                ReverseDirection = Above == 0,
                IsFakeNote = IsFake == 1,
                Type = Type switch
                {
                    1 => NoteType.Tap,
                    2 => NoteType.Hold,
                    3 => NoteType.Flick,
                    4 => NoteType.Drag,
                    _ => NoteType.Tap
                }
            };
        }
    }
}