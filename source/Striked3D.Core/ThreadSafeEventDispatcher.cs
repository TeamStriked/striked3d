﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Striked3D.Core
{
    public class ThreadSafeEventDispatcher<T> where T : class
    {
        readonly object _lock = new object();

        private class RemovableDelegate
        {
            public readonly T Delegate;
            public bool RemovedDuringRaise;

            public RemovableDelegate(T @delegate)
            {
                Delegate = @delegate;
            }
        };

        List<RemovableDelegate> _delegates = new List<RemovableDelegate>();

        Int32 _raisers;  // indicate whether the event is being raised

        // Raises the Event
        public void Raise(Func<T, bool> raiser)
        {
            try
            {
                List<RemovableDelegate> raisingDelegates;
                lock (_lock)
                {
                    raisingDelegates = new List<RemovableDelegate>(_delegates);
                    _raisers++;
                }

                foreach (RemovableDelegate d in raisingDelegates)
                {
                    lock (_lock)
                        if (d.RemovedDuringRaise)
                            continue;

                    raiser(d.Delegate);  // Could use return value here to stop.                    
                }
            }
            finally
            {
                lock (_lock)
                    _raisers--;
            }
        }

        // Override + so that += works like events.
        // Adds are not recognized for any event currently being raised.
        //
        public static ThreadSafeEventDispatcher<T> operator +(ThreadSafeEventDispatcher<T> tsd, T @delegate)
        {
            lock (tsd._lock)
                if (!tsd._delegates.Any(d => d.Delegate == @delegate))
                    tsd._delegates.Add(new RemovableDelegate(@delegate));
            return tsd;
        }

        // Override - so that -= works like events.  
        // Removes are recongized immediately, even for any event current being raised.
        //
        public static ThreadSafeEventDispatcher<T> operator -(ThreadSafeEventDispatcher<T> tsd, T @delegate)
        {
            lock (tsd._lock)
            {
                int index = tsd._delegates
                    .FindIndex(h => h.Delegate == @delegate);

                if (index >= 0)
                {
                    if (tsd._raisers > 0)
                        tsd._delegates[index].RemovedDuringRaise = true; // let raiser know its gone

                    tsd._delegates.RemoveAt(index); // okay to remove, raiser has a list copy
                }
            }

            return tsd;
        }
    }
}
