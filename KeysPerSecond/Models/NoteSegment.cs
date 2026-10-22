using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KeysPerSecond.Models
{
    /// <summary>
    /// Blittable, unmanaged-friendly struct representing an active or completed note segment in the waterfall piano roll.
    /// Zero heap allocation and no GC references.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct NoteSegment
    {
        public int ColumnIndex;
        public long StartTimestamp;
        public long EndTimestamp;
        public bool IsActive;

        public NoteSegment(int columnIndex, long startTimestamp)
        {
            ColumnIndex = columnIndex;
            StartTimestamp = startTimestamp;
            EndTimestamp = 0;
            IsActive = true;
        }

        public NoteSegment(int columnIndex, long startTimestamp, long endTimestamp)
        {
            ColumnIndex = columnIndex;
            StartTimestamp = startTimestamp;
            EndTimestamp = endTimestamp;
            IsActive = false;
        }

        public readonly long GetDurationTicks(long currentTimestamp) =>
            IsActive ? Math.Max(0, currentTimestamp - StartTimestamp) : Math.Max(0, EndTimestamp - StartTimestamp);

        public readonly double GetDurationMs(long currentTimestamp) =>
            GetDurationTicks(currentTimestamp) * 1000.0 / Stopwatch.Frequency;
    }
}

