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
        public PhiUnit[] Units { get; set; }
        public UnitEvent[] UnitEvents { get; set; }
        public BpmEvent[] BpmEvents { get; set; }
        
        public IEnumerator<IList<UnitEvent>> GetOrderedEvents()
        {
            int beats = 0;
            while (beats < UnitEventGroups.Count)
            {
                if (UnitEventGroups.TryGetValue(beats, out var result))
                    yield return result;
                else yield return Array.Empty<UnitEvent>();
                beats++;
            }
        }
        
        public IList<PhiNote> GetNotesByBeatIndex(int index)
        {
            if (!JudgeNoteGroups.TryGetValue(index, out var result)) return new List<PhiNote>();
            return result;
        }

        internal readonly Dictionary<int, List<UnitEvent>> UnitEventGroups = new();
        internal readonly Dictionary<int, List<PhiNote>> JudgeNoteGroups = new();
        internal async Task GenerateInternals()
        {
            await Async.RunOnThread(() =>
            {
                foreach (var line in Units)
                {
                    line.LoadSpeedSegments(
                        UnitEvents.Where(x => x.UnitId == line.UnitId && x.Type == UnitEventType.Speed)
                            .OrderBy(x => x.BeginBeats));
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
                        chartNote.GenerateInternals(Units[chartNote.UnitId], this);
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
                            var line = Units[note.UnitId];
                            note.GenerateInternals(line, this);
                        }
                    }));
                }
            }

            threadTasks.Add(Async.RunOnThread(() =>
            {
                foreach (var group in Notes.Where(x => !x.IsFakeNote).GroupBy(x => (int) x.BeginBeats))
                {
                    JudgeNoteGroups[group.Key] = group.ToList();
                }
            }));

            threadTasks.Add(Async.RunOnThread(() =>
            {
                foreach (var group in UnitEvents.GroupBy(x => (int) x.BeginBeats))
                {
                    UnitEventGroups[group.Key] = group.ToList();
                }
            }));

            await Task.WhenAll(threadTasks);
        }

        internal float FindJudgeTime(PhiNote note)
        {
            var noteTime = note.BeginBeats;
            var result = 0f;

            BpmEvent lastBpmEvent = null;
            var index = 0;
            for (; index < BpmEvents.Length; index++)
            {
                var bpmEvent = BpmEvents[index];
                if (lastBpmEvent == null)
                {
                    lastBpmEvent = bpmEvent;
                    continue;
                }

                if (noteTime >= lastBpmEvent.BeginBeats && noteTime < lastBpmEvent.BeginBeats)
                {
                    result += (noteTime - lastBpmEvent.BeginBeats) * (60f / lastBpmEvent.Value);
                    return result;
                }

                result += (bpmEvent.BeginBeats - lastBpmEvent.BeginBeats) * (60f / lastBpmEvent.Value);
                lastBpmEvent = bpmEvent;
            }
            result += (noteTime - lastBpmEvent!.BeginBeats) * (60f / lastBpmEvent.Value);

            return result;
        }

    }

    [MemoryPackable]
    public partial class BpmEvent
    {
        public float BeginBeats;
        public float Value;

        public static BpmEvent Create(float beginBeats, float value)
        {
            var result = new BpmEvent();
            result.BeginBeats = beginBeats;
            result.Value = value;
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
        public float BeginBeats { get; set; } = 0f;
        public float EndBeats { get; set; } = 0f;
        public float XPosition { get; set; } = 0f;
        public float YPosition { get; set; } = 0f; 
        public uint UnitId { get; set; } = 0;
        public bool ReverseDirection { get; set; } = false;
        public bool IsFakeNote { get; set; } = false;

        internal GameObject NoteGameObject;
        internal float NoteLength = 0f;
        internal float NoteHeight = 0f;
        internal float JudgeTime = 0f;

        internal void GenerateInternals(PhiUnit unit, PhiChart chart)
        {
            NoteHeight = unit.FindYPos(BeginBeats);
            
            if (Type == NoteType.Hold)
            {
                var endYPos = unit.FindYPos(EndBeats);
                NoteLength = endYPos - NoteHeight;
            }

            JudgeTime = chart.FindJudgeTime(this) * 1000f; // unit: s -> ms
        }
    }

    public enum PhiUnitType
    {
        Line,
        AttachUI,
        Text,
    }
    [MemoryPackable]
    public partial class PhiUnit
    {
        public PhiUnitType Type { get; set; } = PhiUnitType.Line;
        public uint UnitId { get; set; } = 0;
        public int ParentUnitId { get; set; } = -1;

        internal float Rotation = 0f;
        internal float YPosition = 0f;
        internal float Speed = 0f;
        internal void LoadSpeedSegments(IEnumerable<UnitEvent> events)
        {
            var lastBeats = 0f;
            var lastSpeed = 0f;
            var result = new List<SpeedSegment>();
            var isFirst = true;
            foreach (var unitEvent in events)
            {
                if (!isFirst)
                {
                    result.Add(new()
                    {
                        BeginBeats = lastBeats,
                        EndBeats = unitEvent.BeginBeats,
                        EndValue = lastSpeed,
                        BeginValue = lastSpeed,
                        IsStatic = true
                    });
                }

                if (unitEvent.BeginBeats != unitEvent.EndBeats)
                {
                    result.Add(new()
                    {
                        BeginBeats = unitEvent.BeginBeats,
                        EndBeats = unitEvent.EndBeats,
                        BeginValue = unitEvent.BeginValue,
                        EndValue = unitEvent.EndValue,
                        IsStatic = false
                    });
                }

                lastBeats = unitEvent.EndBeats;
                lastSpeed = unitEvent.EndValue;
                isFirst = false;
            }

            // add end segment
            result.Add(new ()
            {
                BeginBeats = lastBeats,
                EndBeats = float.PositiveInfinity,
                BeginValue = lastSpeed,
                EndValue = lastSpeed,
                IsStatic = true
            });
            SpeedSegments = result.ToArray();
        }
        internal struct SpeedSegment
        {
            public float BeginBeats;
            public float EndBeats;
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
                if (targetBeats >= speedSegment.BeginBeats && targetBeats <= speedSegment.EndBeats)
                {
                    var deltaBeats = targetBeats - speedSegment.BeginBeats;
                    if (speedSegment.IsStatic)
                        offset += speedSegment.BeginValue * deltaBeats;
                    else
                        offset += (speedSegment.BeginValue + speedSegment.EndValue) * deltaBeats * 0.5f;
                    segmentFound = true;
                    break;
                }

                {
                    var deltaBeats = speedSegment.EndBeats - speedSegment.BeginBeats;
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

    public enum UnitEventType
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
    public partial class UnitEvent
    {
        public UnitEventType Type { get; set; }
        public float BeginValue { get; set; }
        public float BeginBeats { get; set; }
        public float EndValue { get; set; }
        public float EndBeats { get; set; }
        public EasingFunction EasingFunc { get; set; }
        public (float Low, float High) EasingFuncRange { get; set; }
        public uint UnitId { get; set; }
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