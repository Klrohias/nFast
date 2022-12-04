using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chart
{
    public ChartMetadata Metadata { get; set; }
    public ChartNote[] Notes { get; set; }
    public ChartLine[] Lines { get; set; }
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
}
public class LineEvent
{
    public EventType Type { get; set; }
    public float BeginValue { get; set; }
    public ChartTimespan BeginTime { get; set; }
    public float EndValue { get; set; }
    public ChartTimespan EndTime { get; set; }
    public uint EasingFunc { get; set; }
}
public struct ChartTimespan
{
    public int S1;
    public int S2;
    public int S3;
    public static ChartTimespan Zero { get; } = new ChartTimespan {S1 = 0, S2 = 0, S3 = 0};


    public ChartTimespan(IList<int> items)
    {
        S1 = items[0];
        S2 = items[1];
        S3 = items[2];
    }

    public ChartTimespan(int s1, int s2, int s3)
    {
        S1 = s1;
        S2 = s2;
        S3 = s3;
    }

}