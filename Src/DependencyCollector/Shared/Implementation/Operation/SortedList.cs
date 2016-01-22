namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;

    /// <summary>
    /// Represents a collection of sorted elements that are accessible by index.
    /// </summary>
    /// <typeparam name="T">The type of element.</typeparam>
    internal class SortedList<T> : IEnumerable<T>
    {
        /// <summary>
        /// Represents a collection of objects that can be individually accessed by index.
        /// </summary>
        private readonly List<T> list;

        /// <summary>
        /// Exposes a method that compares two objects.
        /// </summary>
        private readonly IComparer<T> comparer;

        /// <summary>
        /// Initializes a new instance of the SortedList class that is empty.
        /// </summary>
        /// <param name="comparer">The IComparer implementation to use when comparing elements.</param>
        internal SortedList(IComparer<T> comparer)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException("comparer");
            }

            this.list = new List<T>();
            this.comparer = comparer;
        }

        /// <summary>
        /// Gets the number of elements contained in a SortedList object.
        /// </summary>
        public int Count
        {
            get
            {
                return this.list.Count;
            }
        }

        /// <summary>
        /// Gets the element at a specified index in a sequence.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <returns>The element at the specified position in the source sequence.</returns>
        public T this[int index]
        {
            get
            {
                return this.list[index];
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return this.list.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An IEnumerator object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Adds an element with the specified value to a SortedList object.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public void Add(T item)
        {
            bool found;
            int index = this.TryIndexOf(item, out found);
            this.list.Insert(index, item);

            Debug.Assert(this.list.Count > 0, "SortedList must not be empty after adding element to the list.");
            Debug.Assert(index == 0 || this.comparer.Compare(this.list[index - 1], item) <= 0, "Inserted element must be bigger than previous");
            Debug.Assert(index == this.list.Count - 1 || this.comparer.Compare(item, this.list[index + 1]) <= 0, "Inserted element must be less than next");
        }

        /// <summary>
        /// Removes the element at the specified index of a SortedList object.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            this.list.RemoveAt(index);
        }

        /// <summary>
        /// Removes the element with the specified value from a SortedList object.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        public void Remove(T item)
        {
            bool found;
            int index = this.TryIndexOf(item, out found);
            if (found)
            {
                this.list.RemoveAt(index);
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the IList.
        /// </summary>
        /// <param name="item">The object to locate in the IList.</param>
        /// <param name="found">True if value is found in the list, otherwise false.</param>
        /// <returns>The index of value if found in the list; otherwise, the index of value where it needs to be inserted.</returns>
        private int TryIndexOf(T item, out bool found)
        {
            found = false;
            int result = this.list.BinarySearch(item, this.comparer);
            if (result < 0)
            {
                // list does not contain the specified value, the method returns a negative integer. 
                // Applying the bitwise complement operation (~) to this negative integer to get the index that should be used as the insertion point to maintain the sort order
                result = ~result;
            }
            else
            {
                found = true;
            }

            return result;
        }
    }
}
