using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Klrohias.NFast.Utilities;
using UnityEngine;

namespace Klrohias.NFast.PhiChartLoader
{
    public partial class PhiChart
    {
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
        private const float NOTE_MIN_Y_DISTANCE = 0.10f; // unit: game unit
        private const float NOTE_MIN_X_DISTANCE = 25f; // unit: chart unit
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
            await Async.RunOnThread(() =>
            {
                foreach (var chartNote in Notes)
                {
                    chartNote.GenerateInternals(Units[chartNote.UnitId], this);
                }
                foreach (var group in Notes.Where(x => !x.IsFakeNote)
                             .GroupBy(x => (int)x.BeginBeats))
                {
                    JudgeNoteGroups[group.Key] = group.ToList();
                }
                foreach (var group in UnitEvents.GroupBy(x => (int)x.BeginBeats))
                {
                    UnitEventGroups[group.Key] = group.ToList();
                }
            });
            await Async.RunOnThread(RemoveDenseNotes);
        }

        internal void RemoveDenseNotes()
        {
            // remove the dense notes
            var unitChunks = new Dictionary<int, List<float>>[Units.Length];

            for (var i = 0; i < unitChunks.Length; i++)
            {
                unitChunks[i] = new Dictionary<int, List<float>>();
            }

            foreach (var note in Notes.OrderBy(x => x.NoteHeight))
            {
                if (note.YPosition != 0) continue; // ignore the notes that has y offset

                var chunks = unitChunks[note.UnitId];
                var noteHeight = note.NoteHeight;

                var chunkIndex = (int)(noteHeight / NOTE_MIN_Y_DISTANCE);

                var detectChunkIndex = chunkIndex - 1;

                if (chunks.TryGetValue(detectChunkIndex, out var detectChunk) &&
                    detectChunk.Any(x => MathF.Abs(note.XPosition - x) < NOTE_MIN_X_DISTANCE))
                {
                    note.WillDisplay = false;
                    continue;
                }

                if (chunks.TryGetValue(chunkIndex, out var chunk))
                    chunk.Add(note.XPosition);
                else chunks.Add(chunkIndex, new List<float> {note.XPosition});
            }
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

    public partial class PhiNote
    {
        internal GameObject NoteGameObject;
        internal float NoteLength = 0f;
        internal float NoteHeight = 0f;
        internal float JudgeTime = 0f;
        internal bool WillDisplay = true;
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

    public partial class PhiUnit
    {
        internal float Rotation = 0f;
        internal float YPosition = 0f;
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
            result.Add(new()
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
        private const float Y_POS_FACTOR = 0.5f;
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

            return offset * Y_POS_FACTOR;
        }
    }
}