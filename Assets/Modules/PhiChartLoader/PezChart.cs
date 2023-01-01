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
                .OrderBy(x => x.BeginBeats).ToList();

            // convert lines/notes
            var countOfNotes = JudgeLineList.Sum(x => x.Notes?.Count ?? 0);
            var notesArray = new PhiNote[countOfNotes];
            var linesArray = new PhiUnit[JudgeLineList.Count];
            var eventsList = new UnorderedList<UnitEvent>(512);

            uint unitId = 0;
            var noteIndex = 0;
            foreach (var judgeLine in JudgeLineList)
            {
                var line = linesArray[unitId] = judgeLine.ToUnitObject();
                line.UnitId = unitId;

                // cast events
                var events = judgeLine.EventLayers
                    .SelectMany(x => (IEnumerable<UnitEvent>) x?.ToNFastEvents(unitId) ?? Array.Empty<UnitEvent>())
                    .Concat((IEnumerable<UnitEvent>) judgeLine.Extended?.ToNFastEvents(unitId) ??
                            Array.Empty<UnitEvent>())
                    .ToArray();

                eventsList.AddRange(events);

                // cast notes
                if (judgeLine.Notes != null)
                {
                    foreach (var note in judgeLine.Notes.Select(x => x.ToNFastNote(unitId)))
                    {
                        notesArray[noteIndex] = note;
                        noteIndex++;
                    }
                }

                unitId++;
            }
            
            // generate nfast chart
            var chart = new PhiChart()
            {
                Metadata = PezMetadata.ToNFastMetadata(),
                Notes = notesArray,
                Units = linesArray,
                BpmEvents = bpmEvents.ToArray(),
                UnitEvents = eventsList.AsArray()
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

        public BpmEvent ToNFastEvent()
            => BpmEvent.Create(ChartTimespan.FromBeatsFraction(Time), Bpm);
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
                default: return EasingFunction.Linear;
            }
        }
        public UnitEvent ToNFastEvent(uint unitId = 0, UnitEventType type = UnitEventType.Alpha)
        {
            return new UnitEvent()
            {
                BeginBeats = ChartTimespan.FromBeatsFraction(StartTime),
                BeginValue = Start,
                EasingFuncRange = (EasingLeft, EasingRight),
                EndBeats = ChartTimespan.FromBeatsFraction(EndTime),
                EndValue = End,
                EasingFunc = ToNFastEasingFunction(),
                Type = type,
                UnitId = unitId
            };
        }
    }
    public class PezLineEventLayer
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

        public List<UnitEvent> ToNFastEvents(uint unitId = 0)
        {
            var result = new List<UnitEvent>();
            if (AlphaEvents != null)
            {
                result.AddRange(AlphaEvents.Select(x => x.ToNFastEvent(unitId, UnitEventType.Alpha)));
            }
            if (MoveXEvents != null)
            {
                result.AddRange(MoveXEvents.Select(x => x.ToNFastEvent(unitId, UnitEventType.MoveX)));
            }
            if (MoveYEvents != null)
            {
                result.AddRange(MoveYEvents.Select(x => x.ToNFastEvent(unitId, UnitEventType.MoveY)));
            }
            if (RotateEvents != null)
            {
                result.AddRange(RotateEvents.Select(x => x.ToNFastEvent(unitId, UnitEventType.Rotate)));
            }
            if (SpeedEvents != null)
            {
                result.AddRange(SpeedEvents.Select(x => { 
                    var lineEvent = x.ToNFastEvent(unitId, UnitEventType.Speed);
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

    public class PezExtendedEvents
    {
        [JsonProperty("inclineEvents")] public List<PezLineEvent> InclineEvents { get; set; }

        [JsonProperty("scaleXEvents")] public List<PezLineEvent> ScaleXEvents { get; set; }

        [JsonProperty("scaleYEvents")] public List<PezLineEvent> ScaleYEvents { get; set; }
        [JsonProperty("textEvents")] public List<object> TextEvents { get; set; }

        public List<UnitEvent> ToNFastEvents(uint unitId = 0)
        {
            var result = new List<UnitEvent>();
            if (InclineEvents != null)
                result.AddRange(InclineEvents.Select(x => x.ToNFastEvent(unitId, UnitEventType.Incline)));
            if (ScaleXEvents != null)
                result.AddRange(ScaleXEvents.Select(x => x.ToNFastEvent(unitId, UnitEventType.ScaleX)));
            if (ScaleYEvents != null)
                result.AddRange(ScaleXEvents.Select(x => x.ToNFastEvent(unitId, UnitEventType.ScaleY)));
            return result;
        }
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
        public List<PezLineEventLayer> EventLayers { get; set; }

        [JsonProperty("extended")]
        public PezExtendedEvents Extended { get; set; }

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

        [JsonProperty("attachUI")]
        public string AttachUI { get; set; }

        private PhiUnitType GetUnitType()
        {
            if (!string.IsNullOrEmpty(AttachUI)) return PhiUnitType.AttachUI;
            if (Extended != null && Extended.TextEvents != null && Extended.TextEvents.Count != 0)
                return PhiUnitType.Text;
            return PhiUnitType.Line;
        }

        public PhiUnit ToUnitObject()
            => new PhiUnit
            {
                ParentUnitId = Father,
                Type = GetUnitType()
            };
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
        public float Size { get; set; }

        [JsonProperty("speed")]
        public float Speed { get; set; }

        [JsonProperty("startTime")]
        public List<int> StartTime { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("visibleTime")]
        public double VisibleTime { get; set; }

        [JsonProperty("yOffset")]
        public float YOffset { get; set; }

        public PhiNote ToNFastNote(uint unitId = 0)
        {
            return new()
            {
                unitId = unitId,
                EndBeats = ChartTimespan.FromBeatsFraction(EndTime),
                BeginBeats = ChartTimespan.FromBeatsFraction(StartTime),
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
                },
                YPosition = YOffset
            };
        }
    }
}