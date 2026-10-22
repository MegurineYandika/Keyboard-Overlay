using System;
using KeysPerSecond.Models;

namespace KeysPerSecond.Services
{
    /// <summary>
    /// Zero-allocation, high-performance circular ring buffer and active note tracker
    /// for the Piano Roll / Waterfall Bar Visualizer.
    /// Manages active held notes and finished scrolling notes with O(1) commits and amortized O(1) culling.
    /// </summary>
    public sealed class NoteStreamBuffer
    {
        private readonly object _sync = new();

        private readonly int _capacity;
        private readonly NoteSegment[] _completedNotes;
        private int _head;
        private int _tail;
        private int _count;

        private int _maxColumns;
        private NoteSegment[] _activeNotes;
        private bool[] _isColumnActive;
        private int _activeCount;

        /// <summary>
        /// Creates a new NoteStreamBuffer with the given completed note capacity and initial column tracking count.
        /// </summary>
        /// <param name="capacity">Maximum completed notes retained in the circular ring buffer (default 1024).</param>
        /// <param name="initialColumns">Initial columns capacity (default 64).</param>
        public NoteStreamBuffer(int capacity = 1024, int initialColumns = 64)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");

            _capacity = capacity;
            _completedNotes = new NoteSegment[capacity];

            _maxColumns = Math.Max(4, initialColumns);
            _activeNotes = new NoteSegment[_maxColumns];
            _isColumnActive = new bool[_maxColumns];
        }

        public int Capacity => _capacity;
        public int CompletedCount => _count;
        public int ActiveCount => _activeCount;
        public int MaxColumns => _maxColumns;
        public int Head => _head;
        public int Tail => _tail;

        /// <summary>
        /// Records physical key down (leading edge) for a column.
        /// Ignores typematic repeat events for an already active column.
        /// </summary>
        /// <returns>True if a new active note was started; false if ignored (e.g. repeat or invalid index).</returns>
        public bool OnKeyDown(int columnIndex, long timestamp)
        {
            if (columnIndex < 0)
                return false;

            lock (_sync)
            {
                EnsureColumnCapacity(columnIndex);

                // Typematic repeat suppression: key is already held down
                if (_isColumnActive[columnIndex])
                    return false;

                _isColumnActive[columnIndex] = true;
                _activeNotes[columnIndex] = new NoteSegment(columnIndex, timestamp);
                _activeCount++;
                return true;
            }
        }

        /// <summary>
        /// Records physical key up (trailing edge) for a column.
        /// Finalizes the active segment and commits it to the completed ring buffer.
        /// </summary>
        /// <returns>True if an active note was finalized; false if the column was not active.</returns>
        public bool OnKeyUp(int columnIndex, long timestamp)
        {
            if (columnIndex < 0)
                return false;

            lock (_sync)
            {
                if (columnIndex >= _maxColumns || !_isColumnActive[columnIndex])
                    return false;

                _isColumnActive[columnIndex] = false;
                _activeCount = Math.Max(0, _activeCount - 1);

                NoteSegment completed = _activeNotes[columnIndex];
                completed.EndTimestamp = timestamp;
                completed.IsActive = false;
                _activeNotes[columnIndex] = completed;

                // Commit note to circular ring buffer
                if (_count == _capacity)
                {
                    // Ring buffer is full: overwrite oldest at head and advance tail
                    _completedNotes[_head] = completed;
                    _head = (_head + 1) % _capacity;
                    _tail = (_tail + 1) % _capacity;
                }
                else
                {
                    _completedNotes[_head] = completed;
                    _head = (_head + 1) % _capacity;
                    _count++;
                }

                return true;
            }
        }

        /// <summary>
        /// Discards notes that have completely scrolled past the visible horizon.
        /// Because notes are committed in chronological order, culling is O(1) amortized.
        /// </summary>
        /// <param name="currentTimestamp">Current render tick in Stopwatch ticks.</param>
        /// <param name="maxDurationTicks">Maximum visible horizon in Stopwatch ticks.</param>
        public void CullExpiredNotes(long currentTimestamp, long maxDurationTicks)
        {
            lock (_sync)
            {
                while (_count > 0)
                {
                    ref readonly NoteSegment oldest = ref _completedNotes[_tail];
                    long elapsed = currentTimestamp - oldest.EndTimestamp;

                    if (elapsed >= maxDurationTicks)
                    {
                        _tail = (_tail + 1) % _capacity;
                        _count--;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the active note segment for a column if currently held.
        /// </summary>
        public bool TryGetActiveNote(int columnIndex, out NoteSegment note)
        {
            lock (_sync)
            {
                if (columnIndex >= 0 && columnIndex < _maxColumns && _isColumnActive[columnIndex])
                {
                    note = _activeNotes[columnIndex];
                    return true;
                }

                note = default;
                return false;
            }
        }

        /// <summary>
        /// Checks whether the column is currently active (held).
        /// </summary>
        public bool IsColumnActive(int columnIndex)
        {
            lock (_sync)
            {
                return columnIndex >= 0 && columnIndex < _maxColumns && _isColumnActive[columnIndex];
            }
        }

        /// <summary>
        /// Returns a readonly reference to a completed note in the buffer by relative index (0 to CompletedCount - 1).
        /// 0 is the oldest completed note currently in view.
        /// </summary>
        public ref readonly NoteSegment GetCompletedNote(int index)
        {
            lock (_sync)
            {
                if ((uint)index >= (uint)_count)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range.");

                int actualIndex = (_tail + index) % _capacity;
                return ref _completedNotes[actualIndex];
            }
        }

        /// <summary>
        /// Clears all active notes and circular ring buffer entries.
        /// </summary>
        public void Clear()
        {
            lock (_sync)
            {
                _count = 0;
                _head = 0;
                _tail = 0;
                _activeCount = 0;
                Array.Clear(_isColumnActive, 0, _isColumnActive.Length);
                Array.Clear(_activeNotes, 0, _activeNotes.Length);
                Array.Clear(_completedNotes, 0, _completedNotes.Length);
            }
        }

        /// <summary>
        /// Alias for Clear().
        /// </summary>
        public void Reset() => Clear();

        private void EnsureColumnCapacity(int columnIndex)
        {
            if (columnIndex >= _maxColumns)
            {
                int newCap = Math.Max(columnIndex + 1, _maxColumns * 2);
                Array.Resize(ref _activeNotes, newCap);
                Array.Resize(ref _isColumnActive, newCap);
                _maxColumns = newCap;
            }
        }
    }
}

