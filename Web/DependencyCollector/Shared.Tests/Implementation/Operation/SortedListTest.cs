namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class SortedListTest
    {
        [TestMethod]
        public void TestAdd()
        {
            var sortedList = new SortedList<int>(new Int32Comparer());
            sortedList.Add(5);
            sortedList.Add(4);
            sortedList.Add(7);
            sortedList.Add(1);
            sortedList.Add(6);
            sortedList.Add(2);
            sortedList.Add(7);

            Assert.AreEqual(1, sortedList[0], "0th element in sorted list must be 1");
            Assert.AreEqual(2, sortedList[1], "1st element in sorted list must be 2");
            Assert.AreEqual(4, sortedList[2], "2nd element in sorted list must be 4");
            Assert.AreEqual(5, sortedList[3], "3rd element in sorted list must be 5");
            Assert.AreEqual(6, sortedList[4], "4th element in sorted list must be 6");
            Assert.AreEqual(7, sortedList[5], "5th element in sorted list must be 7");
            Assert.AreEqual(7, sortedList[6], "6th element in sorted list must be 7");
        }

        [TestMethod]
        public void TestCount()
        {
            var sortedList = new SortedList<int>(new Int32Comparer());
            Assert.AreEqual(0, sortedList.Count, "Sorted list must be empty after initialization");
            sortedList.Add(5);

            Assert.AreEqual(1, sortedList.Count, "After adding one element, sorted list count has to be 1");
            sortedList.RemoveAt(0);

            Assert.AreEqual(0, sortedList.Count, "Sorted list must be empty after removing the element");
        }

        [TestMethod]
        public void TestRemoveAt()
        {
            var sortedList = new SortedList<int>(new Int32Comparer());
            sortedList.Add(5);
            sortedList.Add(3);
            sortedList.Add(7);
            sortedList.RemoveAt(1);
            Assert.AreEqual(sortedList[1], 7, "1st element must be 7");
        }

        [TestMethod]
        public void TestRemove()
        {
            var sortedList = new SortedList<int>(new Int32Comparer());
            sortedList.Add(5);
            sortedList.Add(3);
            sortedList.Add(7);
            sortedList.Remove(3);
            Assert.AreEqual(sortedList[0], 5, "1st element must be 5");
            Assert.AreEqual(sortedList[1], 7, "2nd element must be 7");
        }

        private class Int32Comparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return x - y;
            }
        }
    }
}
