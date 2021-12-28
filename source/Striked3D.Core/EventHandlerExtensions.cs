using System;
using System.Collections.Generic;
using System.Text;

namespace Striked3D.Core
{
    public static class EventHandlerExtensions
    {
        public static void SafeInvoke<T>(this EventHandler<T> evt, object sender, T e) where T : EventArgs
        {
            if (evt != null)
            {
                evt(sender, e);
            }
        }
    }
}
