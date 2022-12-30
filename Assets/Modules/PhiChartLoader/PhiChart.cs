using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Klrohias.NFast.Utilities;
using UnityEngine;
using MemoryPack;

namespace Klrohias.NFast.PhiChartLoader
{
    [MemoryPackable]
    public partial class PhiChart
    {
        public ushort FileVersion { get; set; } = 0;
        public ChartMetadata Metadata { get; set; }
        public PhiNote[] Notes { get; set; }
        public PhiLine[] Lines { get; set; }
        public LineEvent[] LineEvents { get; set; }
        public List<KeyValuePair<float, float>> BpmEvents { get; set; }

        public IList<PhiNote> GetNotes()
        {
            return Notes;
        }

        public IEnumerator<IList<LineEvent>> GetEvents()
        {
            int beats = 0;
            while (beats < LineEventGroups.Count)
            {
                if (LineEventGroups.TryGetValue(beats, out var result))
                    yield return result;
                else yield return new List<LineEvent>();
                beats++;
            }
        }

        public IList<PhiLine> GetLines()
        {
            return Lines;
        }

        public IEnumerator<KeyValuePair<float, float>> GetBpmEvents()
        {
            return BpmEvents.GetEnumerator();
        }

        public IList<PhiNote> GetNotesByBeatIndex(int index)
        {
            if (!JudgeNoteGroups.TryGetValue(index, out var result)) return new List<PhiNote>();
            return result;
        }

        internal readonly Dictionary<int, List<LineEvent>> LineEventGroups = new();
        internal readonly Dictionary<int, List<PhiNote>> JudgeNoteGroups = new();
        internal async Task GenerateInternals()
        {
            await Async.RunOnThread(() =>
            {
                foreach (var line in Lines)
                {
                    line.LoadSpeedSegments(
                        LineEvents.Where(x => x.LineId == line.LineId && x.Type == LineEventType.Speed)
                            .OrderBy(x => x.BeginTime));
                }
            });

            const int maxThreads = 2;
            var threadTasks = new List<Task>();
            if (Notes.Length < maxThreads)
            {
                threadTasks.Add(Async.RunOnThread(() =>
                {
                    foreach (var chartNote in Notes)
                    {
                        chartNote.GenerateInternals(Lines[chartNote.LineId], this);
                    }
                }));
            }
            else
            {
                var ranges = new (int, int)[maxThreads];
                var lastEnd = 0;
                for (int i = 0; i < maxThreads; i++)
                {
                    var end = (int) ((float) Notes.Length / maxThreads * (i + 1));
                    ranges[i] = (lastEnd, end);
                    lastEnd = end;
                }

                foreach (var range in ranges)
                {
                    threadTasks.Add(Async.RunOnThread(() =>
                    {
                        for (int i = range.Item1; i < range.Item2; i++)
                        {
                            var note = Notes[i];
                            var line = Lines[note.LineId];
                            note.GenerateInternals(line, this);
                        }
                    }));
                }
            }

            threadTasks.Add(Async.RunOnThread(() =>
            {
                foreach (var group in Notes.Where(x => !x.IsFakeNote).GroupBy(x => (int) x.BeginTime))
                {
                    JudgeNoteGroups[group.Key] = group.ToList();
                }
            }));

            threadTasks.Add(Async.RunOnThread(() =>
            {
                foreach (var group in LineEvents.GroupBy(x => (int) x.BeginTime))
                {
                    LineEventGroups[group.Key] = group.ToList();
                }
            }));

            await Task.WhenAll(threadTasks);
        }

        internal float FindJudgeTime(PhiNote note)
        {
            var noteTime = note.BeginTime;
            var result = 0f;

            KeyValuePair<float, float>? lastBpmEvent = null;
            var index = 0;
            for (; index < BpmEvents.Count; index++)
            {
                var bpmEvent = BpmEvents[index];
                if (lastBpmEvent == null)
                {
                    lastBpmEvent = bpmEvent;
                    continue;
                }

                var lastBpmEventSafe = lastBpmEvent.Value;
                if (noteTime >= lastBpmEventSafe.Key && noteTime < lastBpmEventSafe.Key)
                {
                    result += (noteTime - lastBpmEventSafe.Key) * (60f / lastBpmEventSafe.Value);
                    return result;
                }

                result += (bpmEvent.Key - lastBpmEventSafe.Key) * (60f / lastBpmEventSafe.Value);
                lastBpmEvent = bpmEvent;
            }

            {
                var lastBpmEventSafe = lastBpmEvent.Value;
                result += (noteTime - lastBpmEventSafe.Key) * (60f / lastBpmEventSafe.Value);
            }

            return result;
        }

    }

    [MemoryPackable]
    public partial class ChartMetadata
    {
        public string MusicFileName { get; set; } = "";
        public string BackgroundFileName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Level { get; set; } = "";
        public string Charter { get; set; } = "";
        public string Composer { get; set; } = "";
        public string ChartId { get; set; } = "";
    }

    public enum NoteType
    {
        Tap,
        Flick,
        Hold,
        Drag
    }
    [MemoryPackable]
    public partial class PhiNote
    {
        public NoteType Type { get; set; } = NoteType.Tap;
        public float BeginTime { get; set; } = 0f;
        public float EndTime { get; set; } = 0f;
        public float XPosition { get; set; } = 0f;
        public float YPosition { get; set; } = 0f; 
        public uint LineId { get; set; } = 0;
        public bool ReverseDirection { get; set; } = false;
        public bool IsFakeNote { get; set; } = false;

        internal GameObject NoteGameObject;
        internal float NoteLength = 0f;
        internal float NoteHeight = 0f;
        internal float JudgeTime = 0f;

        internal void GenerateInternals(PhiLine line, PhiChart chart)
        {
            NoteHeight = line.FindYPos(BeginTime);
            
            if (Type == NoteType.Hold)
            {
                var endYPos = line.FindYPos(EndTime);
                NoteLength = endYPos - NoteHeight;
            }

            JudgeTime = chart.FindJudgeTime(this) * 1000f; // unit: s -> ms
        }
    }
    [MemoryPackable]
    public partial class PhiLine
    {
        public uint LineId { get; set; } = 0;
        public int ParentLineId { get; set; } = -1;

        internal float Rotation = 0f;
        internal float YPosition = 0f;
        internal float Speed = 0f;
        internal void LoadSpeedSegments(IEnumerable<LineEvent> events)
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
                        BeginTime = lastBeats,
                        EndTime = lineEvent.BeginTime,
                        EndValue = lastSpeed,
                        BeginValue = lastSpeed,
                        IsStatic = true
                    });
                }

                if (lineEvent.BeginTime != lineEvent.EndTime)
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

                lastBeats = lineEvent.EndTime;
                lastSpeed = lineEvent.EndValue;
                isFirst = false;
            }

            // add end segment
            result.Add(new ()
            {
                BeginTime = lastBeats,
                EndTime = float.PositiveInfinity,
                BeginValue = lastSpeed,
                EndValue = lastSpeed,
                IsStatic = true
            });
            SpeedSegments = result.ToArray();
        }
        internal struct SpeedSegment
        {
            public float BeginTime;
            public float EndTime;
            public float BeginValue;
            public float EndValue;
            public bool IsStatic;
        }
        internal SpeedSegment[] SpeedSegments;
        internal float FindYPos(float targetBeats)
        {
            // A poo of code :)
            var segmentFound = false; 
            var offset = 0f;
            foreach (var speedSegment in SpeedSegments)
            {
                if (targetBeats >= speedSegment.BeginTime && targetBeats <= speedSegment.EndTime)
                {
                    var deltaBeats = targetBeats - speedSegment.BeginTime;
                    if (speedSegment.IsStatic)
                        offset += speedSegment.BeginValue * deltaBeats;
                    else
                        offset += (speedSegment.BeginValue + speedSegment.EndValue) * deltaBeats * 0.5f;
                    segmentFound = true;
                    break;
                }

                {
                    var deltaBeats = speedSegment.EndTime - speedSegment.BeginTime;
                    if (speedSegment.IsStatic)
                        offset += deltaBeats * speedSegment.BeginValue;
                    else
                        offset += speedSegment.BeginValue * deltaBeats +
                                  (speedSegment.EndValue - speedSegment.BeginValue) * deltaBeats / 2;
                }
            }

            if (!segmentFound) throw new IndexOutOfRangeException($"Segment not found by {targetBeats}");

            return offset;
        }
    }

    public enum LineEventType
    {
        Alpha,
        MoveX,
        MoveY,
        Rotate,
        Speed,
        Incline,
        ScaleX,
        ScaleY,
        Color,
    }

    [MemoryPackable]
    public partial class LineEvent
    {
        public LineEventType Type { get; set; }
        public float BeginValue { get; set; }
        public float BeginTime { get; set; }
        public float EndValue { get; set; }
        public float EndTime { get; set; }
        public EasingFunction EasingFunc { get; set; }
        public (float Low, float High) EasingFuncRange { get; set; }
        public uint LineId { get; set; }
    }

    public static class ChartTimespan
    {
        public static float FromBeatsFraction(IList<int> numbers)
        {
            return FromBeatsFraction(numbers[0], numbers[1], numbers[2]);
        }
        public static float FromBeatsFraction(int s1, int s2, int s3)
        {
            var S1 = s1;
            var S2 = s2;
            var S3 = s3;
            if (S2 > S3)
            {
                S1 += S2 / S3;
                S2 %= S3;
            }

            return S1 + (float) S2 / S3;
        }
    }
}