#region Copyright
// ----------------------- IMPORTANT - READ CAREFULLY: COPYRIGHT NOTICE -------------------
// -- THIS SOFTWARE IS THE PROPERTY OF CTStation S.A.S. IN ANY COUNTRY                   --
// -- (WWW.CTSTATION.NET). ANY COPY, CHANGE OR DERIVATIVE WORK                           --
// -- IS SUBJECT TO CTSTATION S.A.S.’S PRIOR WRITTEN CONSENT.                            --
// -- THIS SOFTWARE IS REGISTERED TO THE FRENCH ANTI-PIRACY AGENCY (APP).                --
// -- COPYRIGHT 2020-01 CTSTATTION S.A.S. – ALL RIGHTS RESERVED.                         --
// ----------------------------------------------------------------------------------------
#endregion


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using log4net;


namespace CTSWeb.Util
{
	// A FIFO cache regularly pruned of old items
	//		Cleaning runs in a dedicated thread for each object. The thread is never stopped
	//		The cache stores multiple values under the same key
	//		Values with the same key are retrieved in FIFO style
	//		A maximum lifespan is expressed in milliseconds, and values not accessed for this duration are removed and disposed of
	//		A method passed in the constructor will be called upon disposal

	// void Push(Key, Value) stores a value
	//
	// Bool TryPop(Key, out Value) returns true if a value is found for this key. 
	//		In this case, the value is removed from the cache and returned in the second argument
	//		Otherwise, the second argument is undefined

	public class TimedCache<tKey, tValue>
	{
		private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
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

		
		public TimedCache(Action<tValue> roDisposeValue, int viLifespanTicks = 300000)    // Default life span = 5', 300s, 300000ms
		{
			_oCache = new ConcurrentDictionary<tKey, ConcurrentQueue<TItem<tValue>>>();
			_iLifespanTicks = (viLifespanTicks < S_Resolution) ? S_Resolution : viLifespanTicks;			// Minimum for a 1/100 resolution of scanning for old values
			_oDisposeValue = roDisposeValue;
            _oCleanupThread = new Thread(this.PrRemoveOldItems)
            {
                Name = "TimedCache cleanup every " + ((Double)(_iLifespanTicks / S_Resolution / 1000.0)).ToString() + " second",
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
				// The FIFO queue for each key is never removed, to avoid a race condition
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
									// The old entry was robed by another process while scanning the FIFO, and we discarded a valid entry. 
									// Too bad, but no way to recover it, so silently push that under the rug in non debug mode
									_oLog.Debug($"Discarded a still young entry, aged {((Environment.TickCount - oRemovedItem.LastUsed) / 1000.0)} s");
								}
								_oLog.Debug($"Discarding {oRemovedItem.Value.ToString()}");
								oCache._oDisposeValue?.Invoke(oRemovedItem.Value);
								_oLog.Debug($"Completed discarding {oRemovedItem.Value.ToString()}");
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
