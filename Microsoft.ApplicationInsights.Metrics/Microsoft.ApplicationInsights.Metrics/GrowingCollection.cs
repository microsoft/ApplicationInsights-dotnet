using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class GrowingCollection<T> : IEnumerable<T>
    {
        private const int SegmentSize = 32;

        internal class Segment
        {
            private readonly Segment _nextSegment;
            private readonly int _nextSegmentGlobalCount;
            private readonly T[] _data = new T[SegmentSize];
            private int _localCount = 0;

            public Segment(Segment nextSegment)
            {
                _nextSegment = nextSegment;
                _nextSegmentGlobalCount = (nextSegment == null) ? 0 : nextSegment.GlobalCount;
            }

            public int LocalCount
            {
                get
                {
                    int lc = Volatile.Read(ref _localCount);
                    if (lc > SegmentSize)
                    {
                        return SegmentSize;
                    }
                    else
                    {
                        return lc;
                    }
                }
            }

            public Segment NextSegment
            {
                get
                {
                    return _nextSegment;
                }
            }

            public int GlobalCount
            {
                get
                {
                    return LocalCount + _nextSegmentGlobalCount;
                }
            }

            internal bool TryAdd(T item)
            {
                int index = Interlocked.Increment(ref _localCount) - 1;
                if (index >= SegmentSize)
                {
                    Interlocked.Decrement(ref _localCount);
                    return false;
                }

                _data[index] = item;
                return true;
            }

            public T this[int index]
            {
                get
                {
                    if (index < 0 || _localCount <= index || SegmentSize <= index)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index ({index})");
                    }

                    return _data[index];
                }
            }
        }

        public class Enumerator : IEnumerator<T>
        {
            private readonly Segment _head;
            private readonly int _headOffset;
            private readonly int _count;
            private Segment _currentSegment;
            private int _currentSegmentOffset;

            internal Enumerator(Segment head)
            {
                if (head == null)
                {
                    throw new ArgumentNullException(nameof(head));
                }

                _head = _currentSegment = head;
                _headOffset = _currentSegmentOffset = head.LocalCount;
                _count = _headOffset + (_head.NextSegment == null ? 0 : _head.NextSegment.GlobalCount);
            }

            public int Count
            {
                get
                {
                    return Count;
                }
            }

            public T Current
            {
                get
                {
                    return _currentSegment[_currentSegmentOffset];
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_currentSegmentOffset == 0)
                {
                    if (_currentSegment.NextSegment == null)
                    {
                        return false;
                    }
                    else
                    {
                        _currentSegment = _currentSegment.NextSegment;
                        _currentSegmentOffset = _currentSegment.LocalCount - 1;
                        return true;
                    }
                }
                else
                {
                    _currentSegmentOffset--;
                    return true;
                }
            }

            public void Reset()
            {
                _currentSegment = _head;
                _currentSegmentOffset = _headOffset;
            }
        }

        private Segment _dataHead;

        public GrowingCollection()
        {
            _dataHead = new Segment(null);
        }

        public int Count {
            get
            {
                Segment currHead = Volatile.Read(ref _dataHead);
                return currHead.GlobalCount;
            }
        }

        public void Add(T item)
        {
            Segment currHead = Volatile.Read(ref _dataHead);

            bool added = currHead.TryAdd(item);
            while (! added)
            {
                Segment newHead = new Segment(currHead);
                Segment prevHead = Interlocked.CompareExchange(ref _dataHead, newHead, currHead);

                Segment updatedHead = (prevHead == currHead) ? newHead : prevHead;
                added = updatedHead.TryAdd(item);
            }
        }

        public GrowingCollection<T>.Enumerator GetEnumerator()
        {
            var enumerator = new GrowingCollection<T>.Enumerator(_dataHead);
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
