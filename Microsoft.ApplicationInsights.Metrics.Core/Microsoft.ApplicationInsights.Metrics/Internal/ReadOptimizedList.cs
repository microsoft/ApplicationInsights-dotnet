using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal class ReadOptimizedList<T> : IList<T>, IReadOnlyList<T>
    {
        public class ModifiedEventArgs : EventArgs
        {
            public IReadOnlyList<T> OldData { get; }
            public IReadOnlyList<T> NewData { get; }
            public ModifiedEventArgs(List<T> oldData, List<T> newData)
            {
                this.OldData = oldData;
                this.NewData = newData;
            }
        }

        private List<T> _data;

        public ReadOptimizedList()
        {
            _data = new List<T>();
        }


        public ReadOptimizedList(IEnumerable<T> collection)
        {
            _data = new List<T>(collection);
        }

        public ReadOptimizedList(int capacity)
        {
            _data = new List<T>(capacity);
        }

        public event EventHandler<ReadOptimizedList<T>.ModifiedEventArgs> Modified;

        protected virtual void OnModified(List<T> oldData, List<T> newData)
        {
            EventHandler<ReadOptimizedList<T>.ModifiedEventArgs> modifiedHandler = this.Modified;
            if (modifiedHandler != null)
            {
                var eventArgs = new ReadOptimizedList<T>.ModifiedEventArgs(oldData, newData);
                modifiedHandler(this, eventArgs);
            }
        }

        public T this[int index]
        {
            get
            {
                List<T> data = Volatile.Read(ref _data);
                return data[index];
            }

            set
            {
                this.CopyModifyData((d) => d[index] = value);
            }
        }

        public int Count
        {
            get
            {
                return Volatile.Read(ref _data).Count;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(T item)
        {
            this.CopyModifyData((d) => d.Add(item));
        }

        public void Clear()
        {
            List<T> newData = new List<T>();
            List<T> oldData = Interlocked.Exchange(ref _data, newData);
            this.OnModified(oldData, newData);
        }

        public bool Contains(T item)
        {
            return Volatile.Read(ref _data).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Volatile.Read(ref _data).CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Volatile.Read(ref _data).GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return Volatile.Read(ref _data).IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.CopyModifyData((d) => d.Insert(index, item));
        }

        public bool Remove(T item)
        {
            bool result = false;
            this.CopyModifyData((d) => { result = d.Remove(item); });
            return result;
        }

        public void RemoveAt(int index)
        {
            this.CopyModifyData((d) => d.RemoveAt(index));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Volatile.Read(ref _data).GetEnumerator();
        }

        public void Modify(Action<List<T>> modifyerFunction)
        {
            Util.ValidateNotNull(modifyerFunction, nameof(modifyerFunction));

            this.CopyModifyData(modifyerFunction);
        }

        private void CopyModifyData(Action<List<T>> modifyerFunction)
        {
            List<T> oldData, newData;
            bool replacedSameAsModified;
            do
            {
                oldData = Volatile.Read(ref _data);

                newData = new List<T>(oldData.Count + 1);
                newData.AddRange(oldData);

                modifyerFunction(newData);

                List<T> prevData = Interlocked.CompareExchange(ref _data, newData, oldData);
                replacedSameAsModified = (prevData == oldData);
            }
            while (!replacedSameAsModified);

            this.OnModified(oldData, newData);
        }
    }
}
