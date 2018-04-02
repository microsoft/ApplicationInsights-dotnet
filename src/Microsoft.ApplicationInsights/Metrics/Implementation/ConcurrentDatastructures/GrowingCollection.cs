namespace Microsoft.ApplicationInsights.Metrics.ConcurrentDatastructures
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using static System.FormattableString;

    /// <summary>@ToDo: Complete documentation before stable release. {574}</summary>
    /// <typeparam name="T">Type of collection elemets.</typeparam>
    internal class GrowingCollection<T> : IEnumerable<T>
    {
        private const int SegmentSize = 32;

        private Segment dataHead;

        /// <summary>@ToDo: Complete documentation before stable release. {371}</summary>
        public GrowingCollection()
        {
            this.dataHead = new Segment(null);
        }

        /// <summary>Gets @ToDo: Complete documentation before stable release. {072}</summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Segment currHead = Volatile.Read(ref this.dataHead);
                return currHead.GlobalCount;
            }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {146}</summary>
        /// <param name="item">@ToDo: Complete documentation before stable release. {688}</param>
        public void Add(T item)
        {
            Segment currHead = Volatile.Read(ref this.dataHead);

            bool added = currHead.TryAdd(item);
            while (false == added)
            {
                Segment newHead = new Segment(currHead);
                Segment prevHead = Interlocked.CompareExchange(ref this.dataHead, newHead, currHead);

                Segment updatedHead = (prevHead == currHead) ? newHead : prevHead;
                added = updatedHead.TryAdd(item);
            }
        }

        /// <summary>@ToDo: Complete documentation before stable release. {508}</summary>
        /// <returns>@ToDo: Complete documentation before stable release. {325}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GrowingCollection<T>.Enumerator GetEnumerator()
        {
            var enumerator = new GrowingCollection<T>.Enumerator(this.dataHead);
            return enumerator;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {563}</summary>
        /// <returns>@ToDo: Complete documentation before stable release. {016}</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #region class Enumerator 

        /// <summary>@ToDo: Complete documentation before stable release. {671}</summary>
        public class Enumerator : IEnumerator<T>
        {
            private readonly Segment head;
            private readonly int headOffset;
            private readonly int count;
            private Segment currentSegment;
            private int currentSegmentOffset;

            internal Enumerator(Segment head)
            {
                Util.ValidateNotNull(head, nameof(head));
                
                this.head = this.currentSegment = head;
                this.headOffset = this.currentSegmentOffset = head.LocalCount;
                this.count = this.headOffset + (this.head.NextSegment == null ? 0 : this.head.NextSegment.GlobalCount);
            }

            /// <summary>Gets @ToDo: Complete documentation before stable release. {648}</summary>
            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return this.count;
                }
            }

            /// <summary>Gets @ToDo: Complete documentation before stable release. {314}</summary>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return this.currentSegment[this.currentSegmentOffset];
                }
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return this.Current;
                }
            }

            /// <summary>@ToDo: Complete documentation before stable release. {941}</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
            }

            /// <summary>@ToDo: Complete documentation before stable release. {168}</summary>
            /// <returns>@ToDo: Complete documentation before stable release. {185}</returns>
            public bool MoveNext()
            {
                if (this.currentSegmentOffset == 0)
                {
                    if (this.currentSegment.NextSegment == null)
                    {
                        return false;
                    }
                    else
                    {
                        this.currentSegment = this.currentSegment.NextSegment;
                        this.currentSegmentOffset = this.currentSegment.LocalCount - 1;
                        return true;
                    }
                }
                else
                {
                    this.currentSegmentOffset--;
                    return true;
                }
            }

            /// <summary>@ToDo: Complete documentation before stable release. {307}</summary>
            public void Reset()
            {
                this.currentSegment = this.head;
                this.currentSegmentOffset = this.headOffset;
            }
        }
        #endregion class Enumerator 

        #region class Segment
        internal class Segment
        {
            private readonly Segment nextSegment;
            private readonly int nextSegmentGlobalCount;
            private readonly T[] data = new T[SegmentSize];
            private int localCount = 0;

            public Segment(Segment nextSegment)
            {
                this.nextSegment = nextSegment;
                this.nextSegmentGlobalCount = (nextSegment == null) ? 0 : nextSegment.GlobalCount;
            }

            public int LocalCount
            {
                get
                {
                    int lc = Volatile.Read(ref this.localCount);
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
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return this.nextSegment;
                }
            }

            public int GlobalCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return this.LocalCount + this.nextSegmentGlobalCount;
                }
            }

            public T this[int index]
            {
                get
                {
                    if (index < 0 || this.localCount <= index || SegmentSize <= index)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), Invariant($"Invalid index ({index})"));
                    }

                    return this.data[index];
                }
            }

            internal bool TryAdd(T item)
            {
                int index = Interlocked.Increment(ref this.localCount) - 1;
                if (index >= SegmentSize)
                {
                    Interlocked.Decrement(ref this.localCount);
                    return false;
                }

                this.data[index] = item;
                return true;
            }
        }
        #endregion class Segment
    }
}
