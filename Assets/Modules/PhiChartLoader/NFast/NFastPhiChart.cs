using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Klrohias.NFast.PhiChartLoader;
using Klrohias.NFast.Utilities;
using UnityEngine;

namespace Klrohias.NFast.PhiChartLoader.NFast
{
    public class NFastPhiChart : IPhiChart
    {
        public ChartMetadata Metadata { get; set; }
        public ChartNote[] Notes { get; set; }
        public ChartLine[] Lines { get; set; }
        public LineEvent[] LineEvents { get; set; }
        public List<KeyValuePair<ChartTimespan, float>> BpmEvents { get; set; }

        public IEnumerator<IList<ChartNote>> GetNotes()
        {
            var notes = new ChartNote[Notes.Length];
            Array.Copy(Notes, notes, Notes.Length);
            int reduceIndex = 0;
            int beats = 0;

            while (reduceIndex < notes.Length)
            {
                var resultNotes = new List<ChartNote>(Math.Max(8, (notes.Length - reduceIndex) / 4));
                for (int i = reduceIndex; i < notes.Length; i++)
                {
                    var note = notes[i];
                    var startTime = Convert.ToInt32(MathF.Floor(note.StartTime.Beats));
                    if (startTime == beats)
                    {
                        resultNotes.Add(note);
                        notes[i] = notes[reduceIndex];
                        notes[reduceIndex] = null;
                        reduceIndex++;
                    }
                }

                beats++;
                yield return resultNotes;
            }
        }

        public IEnumerator<IList<LineEvent>> GetEvents()
        {
            var events = new LineEvent[LineEvents.Length];
            Array.Copy(LineEvents, events, LineEvents.Length);
            int reduceIndex = 0;
            int beats = 0;

            while (reduceIndex < events.Length)
            {
                var resultEvents = new List<LineEvent>(Math.Max(8, (events.Length - reduceIndex) / 4));
                for (int i = reduceIndex; i < events.Length; i++)
                {
                    var lineEvent = events[i];
                    var startTime = (int)lineEvent.BeginTime.Beats;
                    if (startTime == beats)
                    {
                        resultEvents.Add(lineEvent);
                        events[i] = events[reduceIndex];
                        events[reduceIndex] = null;
                        reduceIndex++;
                    }
                }

                beats++;
                yield return resultEvents;
            }
        }

        public IList<ChartLine> GetLines()
        {
            return Lines;
        }

        public IEnumerator<KeyValuePair<ChartTimespan, float>> GetBpmEvents()
        {
            return BpmEvents.GetEnumerator();
        }
    }

    public class ChartMetadata
    {
        public string MusicFileName { get; set; } = "";
        public string BackgroundFileName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Level { get; set; } = "";
        public string Charter { get; set; } = "";
        public string Composer { get; set; } = "";
    }

    public enum NoteType
    {
        Tap,
        Flick,
        Hold,
        Drag
    }
    public class ChartNote
    {
        public NoteType Type { get; set; } = NoteType.Tap;
        public ChartTimespan StartTime { get; set; } = ChartTimespan.Zero;
        public ChartTimespan EndTime { get; set; } = ChartTimespan.Zero;
        public float XPosition { get; set; } = 0f;
        public uint LineId { get; set; } = 0;
        public bool ReverseDirection { get; set; } = false;
        public bool IsFakeNote { get; set; } = false;
    }

    public class ChartLine
    {
        public uint LineId { get; set; } = 0;
        internal void ToSpeedSegment(IEnumerable<LineEvent> events)
        {
            var lastBeats = 0f;
            var lastSpeed = 0f;
            var result = new List<SpeedSegment>();
            var isFirst = true;
            foreach (var lineEvent in events)
            {
                if (!isFirst)
                {
                    result.Add(new()
                    {
                        BeginTime = new(lastBeats),
                        EndTime = lineEvent.BeginTime,
                        EndValue = lastSpeed,
                        BeginValue = lastSpeed,
                        IsStatic = true
                    });
                }

                if (lineEvent.BeginTime.Beats != lineEvent.EndTime.Beats)
                {
                    result.Add(new()
                    {
                        BeginTime = lineEvent.BeginTime,
                        EndTime = lineEvent.EndTime,
                        BeginValue = lineEvent.BeginValue,
                        EndValue = lineEvent.EndValue,
                        IsStatic = false
                    });
                }

                lastBeats = lineEvent.EndTime.Beats;
                lastSpeed = lineEvent.EndValue;
                isFirst = false;
            }

            SpeedSegments = result.ToArray();
        }
        internal struct SpeedSegment
        {
            public ChartTimespan BeginTime;
            public ChartTimespan EndTime;
            public float BeginValue;
            public float EndValue;
            public bool IsStatic;
        }
        internal SpeedSegment[] SpeedSegments;
    }

    public enum EventType
    {
        Alpha,
        MoveX,
        MoveY,
        Rotate,
        Speed,
        Incline
    }

    public enum EasingFunction
    {
        Linear,
        SineIn,
        SineOut,
        SineInOut,
        QuadIn,
        QuadOut,
        QuadInOut,
        CubicIn,
        CubicOut,
        CubicInOut,
        QuartIn,
        QuartOut,
        QuartInOut,
        QuintIn,
        QuintOut,
        ExpoIn,
        ExpoOut,
        CircIn,
        CircOut,
        CircInOut,
        BackIn,
        BackOut,
        BackInOut,
        ElasticIn,
        ElasticOut,
        BounceIn,
        BounceOut,
        BounceInOut,
    }

    public class LineEvent
    {
        public EventType Type { get; set; }
        public float BeginValue { get; set; }
        public ChartTimespan BeginTime { get; set; }
        public float EndValue { get; set; }
        public ChartTimespan EndTime { get; set; }
        public EasingFunction EasingFunc { get; set; }
        public (float Low, float High) EasingFuncRange { get; set; }
        public uint LineId { get; set; }
    }

    public struct ChartTimespan
    {
        public float Beats;
        public static ChartTimespan Zero { get; } = new ChartTimespan {Beats = 0};

        private void FromBeatsFraction(int s1, int s2, int s3)
        {
            var S1 = s1;
            var S2 = s2;
            var S3 = s3;
            if (S2 > S3)
            {
                S1 += S2 / S3;
                S2 %= S3;
            }

            Beats = S1 + (float) S2 / S3;
        }

        public ChartTimespan(IList<int> items)
        {
            Beats = 0f;
            FromBeatsFraction(items[0], items[1], items[2]);
        }

        public ChartTimespan(int s1, int s2, int s3)
        {
            Beats = 0f;
            FromBeatsFraction(s1, s2, s3);
        }

        public ChartTimespan(float beats)
        {
            Beats = beats;
        }

        public override string ToString() => Beats.ToString();
    }
}