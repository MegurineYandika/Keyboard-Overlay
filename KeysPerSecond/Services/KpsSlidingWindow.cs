using System;
using System.Diagnostics;

namespace KeysPerSecond.Services
{
    /// <summary>
    /// High-performance circular buffer for computing Keys Per Second (KPS)
    /// across an exact sliding time window (e.g. 1000ms) with zero heap allocation.
    /// </summary>
    public sealed class KpsSlidingWindow
    {
        private readonly long[] _timestamps;
        private readonly int _capacity;
        private int _head;
        private int _tail;
        private int _count;

        /// <summary>
        /// Creates a sliding window buffer capable of holding up to <paramref name="capacity"/> keypresses.
        /// Default 2048 is more than enough even for extreme multi-finger 200+ KPS bursts.
        /// </summary>
        public KpsSlidingWindow(int capacity = 2048)
        {
            if (capacity <= 0 || (capacity & (capacity - 1)) != 0)
            {
                // Round up to power of two for fast wrapping if desired, but modulo is also fine
                capacity = Math.Max(64, capacity);
            }

            _capacity = capacity;
            _timestamps = new long[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }

        public int Capacity => _capacity;
        public int CurrentCount => _count;

        /// <summary>
        /// Records a key press at the specified high-resolution timestamp.
        /// Zero allocation. O(1).
        /// </summary>
        public void RecordPress(long timestamp)
        {
            _timestamps[_head] = timestamp;
            _head = (_head + 1) % _capacity;

            if (_count < _capacity)
            {
                _count++;
            }
            else
            {
                // Buffer is full; overwrite oldest item and advance tail
                _tail = (_tail + 1) % _capacity;
            }
        }

        /// <summary>
        /// Evicts all timestamps older than (currentTimestamp - windowTicks)
        /// and returns the number of active keypresses within the sliding window.
        /// Zero allocation. Amortized O(1).
        /// </summary>
        public int UpdateAndGetCount(long currentTimestamp, long windowTicks)
        {
            long cutoff = currentTimestamp - windowTicks;

            while (_count > 0 && _timestamps[_tail] < cutoff)
            {
                _tail = (_tail + 1) % _capacity;
                _count--;
            }

            return _count;
        }

        /// <summary>
        /// Clears all recorded timestamps in the window.
        /// </summary>
        public void Reset()
        {
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }
}
