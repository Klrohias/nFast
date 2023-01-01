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
        public ushort FileVersion { get; set; }
        public ChartMetadata Metadata { get; set; }
        public PhiNote[] Notes { get; set; }
        public PhiUnit[] Units { get; set; }
        public UnitEvent[] UnitEvents { get; set; }
        public BpmEvent[] BpmEvents { get; set; }
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