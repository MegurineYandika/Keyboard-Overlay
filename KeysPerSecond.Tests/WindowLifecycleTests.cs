using System;
using System.Threading;
using System.Windows;
using KeysPerSecond.Models;
using KeysPerSecond.ViewModels;
using Xunit;

namespace KeysPerSecond.Tests
{
    public class WindowLifecycleTests
    {
        [Fact]
        public void MainWindow_InitializesWithoutException_OnSTAThread()
        {
            Exception? caughtException = null;

            var staThread = new Thread(() =>
            {
                try
                {
                    // Ensure WPF Application context exists
                    if (Application.Current == null)
                    {
                        new Application();
                    }

                    var window = new MainWindow();
                    Assert.NotNull(window);
                    Assert.NotNull(window.DataContext);

                    // Close right away
                    window.Close();
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join(5000);

            Assert.Null(caughtException);
        }
    }
}
