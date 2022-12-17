using System;
using System.Collections;
using System.Collections.Generic;
using Klrohias.NFast.ChartLoader;
using UnityEngine;

namespace Klrohias.NFast.ChartLoader.NFast
{
    public class NFastChart : IChart
    {
        public ChartMetadata Metadata { get; set; }
        public ChartNote[] Notes { get; set; }
        public ChartLine[] Lines { get; set; }
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

        public IEnumerator<IList<ChartLine>> GetLines()
        {
            yield return Lines;
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

    public class ChartNote
    {
        public ChartTimespan StartTime { get; set; } = ChartTimespan.Zero;
        public ChartTimespan EndTime { get; set; } = ChartTimespan.Zero;
        public float XPosition { get; set; } = 0f;
        public uint LineId { get; set; } = 0;
    }

    public class ChartLine
    {
        public uint LineId { get; set; } = 0;
        public LineEvent[] LineEvents;
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
    }
}