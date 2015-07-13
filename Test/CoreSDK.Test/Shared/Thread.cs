namespace System.Threading
{
#if WINRT

    public class Thread
    {
        public static void Sleep(int milliseconds)
        {
            using (ManualResetEvent waitEvent = new ManualResetEvent(false))
            {
                waitEvent.WaitOne(milliseconds);
            }
        }
    }

#endif
}
