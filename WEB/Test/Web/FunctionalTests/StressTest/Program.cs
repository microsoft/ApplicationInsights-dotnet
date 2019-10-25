
namespace StressTest
{
    using System;
    using System.Diagnostics;

    using Microsoft.Diagnostics.OperationTracking;

    class Program
    {
        const string operationName = "some-operation";

        static void Main(string[] args)
        {
            DummySpeedTest();
            NopListenerTest();
        }

        private static void DummySpeedTest()
        {
            Stopwatch w = new Stopwatch();
            long j = 0;

            w.Start();

            for (int i = 0; i < 100000000; i++)
            {
                j += i;
            }

            w.Stop();

            Console.WriteLine("Tight loop {0}ms, {1} ticks, j={2}", w.ElapsedMilliseconds, w.ElapsedTicks, j);

            j = 0;

            w.Restart();

            for (int i = 0; i < 100000000; i++)
            {
                Operation o = Operation.Start(operationName);

                j += i;

                o.Complete();
            }

            w.Stop();

            Console.WriteLine("Dummy activity {0}ms, {1} ticks, j = {2}", w.ElapsedMilliseconds, w.ElapsedTicks, j);
        }

        private static void NopListenerTest()
        {
            NopListener listener = new NopListener();

            OperationTrackingConfig.Listeners.Succeeded.Add(listener);
            OperationTrackingConfig.Listeners.Failed.Add(listener);

            OperationTrackingConfig.EnableOperationTracking();

            Stopwatch w = new Stopwatch();
            long j = 0;

            w.Start();

            for (int i = 0; i < 100000000; i++)
            {
                Operation o = Operation.Start(operationName);

                j += i;

                o.Complete();
            }

            w.Stop();

            Console.WriteLine("Nop listener {0}ms, {1} ticks, j={2}", w.ElapsedMilliseconds, w.ElapsedTicks, j);
        }
    }
}
