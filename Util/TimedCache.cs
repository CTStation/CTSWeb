using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;


namespace CTSWeb.Util
{
	// The cache stores multiple values under the same key
	// For the same key, values are retrieved in FIFO style
	// A maximum lifespan is expressed in milisecounds, and values not accessed for this duration are removed and disposed of
	//		A method passed in the constructir will be called upon disposal
	// void Push(Key, Value) stores a value
	// Bool TryPop(Key, out Value) returns true if a value is found for this key. 
	//		In this case, the value is removed from the cache and returned in the secound argument
	//		Otherwise, the secound argument is undefined

	// The FIFO queue for each key is never removed, to avoid a race condition

	public class TimedCache<tKey, tValue>
	{
		private class TItem<tValue2>
		{
			private readonly int _iLastUsedTick;
			private readonly tValue2 _oValue;

			public TItem(tValue2 roValue)
			{
				_oValue = roValue;
				_iLastUsedTick = System.Environment.TickCount;
			}

			public tValue2 Value { get => _oValue; }
			public int LastUsed { get => _iLastUsedTick; }
		}

		private const int S_Resolution = 100;		// Number of time the cleanup is called for each lifespan
		private readonly ConcurrentDictionary< tKey, ConcurrentQueue< TItem<tValue> > > _oCache;
		private readonly int _iLifespanTicks;   
		private readonly Action<tValue> _oDisposeValue = null;
		private readonly Thread _oCleanupThread;

		
		public TimedCache(Action<tValue> roDisposeValue, int viLifespanTicks = 300000)    // Default life span = 5', 300'', 300000ms
		{
			_oCache = new ConcurrentDictionary<tKey, ConcurrentQueue<TItem<tValue>>>();
			_iLifespanTicks = (viLifespanTicks < S_Resolution) ? S_Resolution : viLifespanTicks;			// Minimum for a 1/100 resoltion of scanning for old values
			_oDisposeValue = roDisposeValue;
            _oCleanupThread = new Thread(this.PrRemoveOldItems)
            {
                Name = "TimedCache cleanup every " + ((Double)(_iLifespanTicks / S_Resolution / 1000.0)).ToString() + " s",
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true
            };
            _oCleanupThread.Start(this);
		}


		public void Push (tKey roKey, tValue roValue)
        {
			TItem<tValue> oItem = new TItem<tValue>(roValue);
			_oCache.GetOrAdd(roKey, (oKey) => { return new ConcurrentQueue<TItem<tValue>>(); }).Enqueue(oItem);
        }


		public bool TryPop(tKey roKey, out tValue roValue)
        {
			bool bRet = false;
            roValue = default(tValue);

            if (_oCache.TryGetValue(roKey, out ConcurrentQueue<TItem<tValue>> oFIFO))
            {
                if (oFIFO.TryDequeue(out TItem<tValue> oItem))
                {
                    // Can return a somewhat outdated entry, never much more than the resolution
                    roValue = oItem.Value;
                    bRet = true;
                }
            }
            return bRet;
		}


		private void PrRemoveOldItems(object rObj)
		{
			
			int iLastLiveTick;
			TimedCache<tKey, tValue> oCache = rObj as TimedCache<tKey, tValue>;

			while (true)
			{
				// Avoid underflow during the first 5 minutes of server uptime
				iLastLiveTick = Environment.TickCount;
				if (oCache._iLifespanTicks < iLastLiveTick) iLastLiveTick -= oCache._iLifespanTicks;
				foreach (KeyValuePair<tKey, ConcurrentQueue< TItem<tValue>>> o in oCache._oCache)
				{
					foreach (TItem<tValue> oItem in o.Value)
					{
						if (oItem.LastUsed < iLastLiveTick)
						{
							// We found an old entry. However it may have been poped by another process
							if (o.Value.TryDequeue(out TItem<tValue> oRemovedItem))
							{
								if (!(oRemovedItem.LastUsed < iLastLiveTick))
								{
									// The old entry was robed by another process while scaning the FIFO, and we discared a valid entry. 
									// Too bad, but no way to recover it, so silently push that under the rug in non debug mode
									Debug.Print ("Discarded a still yound entry, aged " + ((Environment.TickCount - oRemovedItem.LastUsed) / 1000.0).ToString() + "s");
								}
                                oCache._oDisposeValue?.Invoke(oRemovedItem.Value);
                            }
						} else {
							break;	// if an entry is live, all the next ones should be. Otherwise, they'll get caght on the next run 
                        }
					}
				}
				Thread.Sleep(oCache._iLifespanTicks / S_Resolution);
			}
		}
	}
}
