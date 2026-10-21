using System.Windows;

namespace KeysPerSecond.Tests
{
    public static class WpfTestHelper
    {
        private static readonly object Lock = new();

        public static void EnsureApplication()
        {
            if (Application.Current == null)
            {
                lock (Lock)
                {
                    if (Application.Current == null)
                    {
                        try
                        {
                            new Application();
                        }
                        catch (System.InvalidOperationException)
                        {
                            // Already created by another thread
                        }
                    }
                }
            }
        }
    }
}

