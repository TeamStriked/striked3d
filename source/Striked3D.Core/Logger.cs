using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Striked3D.Core
{
    public static class Logger
    {
        private static TimeSpan getTimeSinceStart()
        {
            return DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        }
        private static int GetThreadId()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }
        public static void Debug(object sendingObject, string message)
        {
            Console.WriteLine(string.Format("[{0}][{1}]: {2}s - {3}", sendingObject.GetType().Name, GetThreadId(), getTimeSinceStart().TotalSeconds, message));
        }
        public static void Debug(string message)
        {
            Console.WriteLine(string.Format("[{0}]: {1}s - {2}", GetThreadId(), getTimeSinceStart().TotalSeconds, message));
        }
        public static void Error(object sendingObject, string message, string stack)
        {
            Console.WriteLine(string.Format("[{0}][{1}]: {2}s - {3}: {4}", sendingObject.GetType().Name, GetThreadId(), getTimeSinceStart().TotalSeconds, message, stack));
        }
        public static void Error(string message, string stack)
        {
            Console.WriteLine(string.Format("[{0}]: {1}s - {2}: {3}", GetThreadId(), getTimeSinceStart().TotalSeconds, message, stack));
        }
    }
}
