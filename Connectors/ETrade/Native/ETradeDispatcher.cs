namespace StockSharp.ETrade.Native
{
	using System;
	using System.Threading;

	using Ecng.Common;
	using Ecng.ComponentModel;

	sealed class ETradeDispatcher : EventDispatcher
	{
		private const string _eventThreadRequestName = "rq";
		private const string _eventThreadResponseName = "rs";
		private readonly int _eventThreadRequestId;
		private readonly int _eventThreadResponseId;

		public ETradeDispatcher(Action<Exception> errorHandler)
			: base(errorHandler)
		{
			const int num = 2; //num of Add() calls below
			var count = 0;
			var done = new ManualResetEventSlim(false);
			Action report = delegate
			{
				if (Interlocked.Increment(ref count) == num)
					done.Set();
			};
			int idmain, iddom;

			idmain = iddom = 0;

			Add(() =>
			{
				idmain = Thread.CurrentThread.ManagedThreadId;
				report();
			}, _eventThreadRequestName);

			Add(() =>
			{
				iddom = Thread.CurrentThread.ManagedThreadId;
				report();
			}, _eventThreadResponseName);

			done.Wait();

			_eventThreadRequestId = idmain;
			_eventThreadResponseId = iddom;
		}

		public void OnRequestThread(Action action)
		{
			DoSync(action, _eventThreadRequestName, _eventThreadRequestId);
		}

		public void OnRequestThreadAsync(Action action, bool forceAsync = false)
		{
			DoAsync(action, _eventThreadRequestName, _eventThreadRequestId, forceAsync);
		}

		public void OnResponseThread(Action action)
		{
			DoSync(action, _eventThreadResponseName, _eventThreadResponseId);
		}

		public void OnResponseThreadAsync(Action action, bool forceAsync = false)
		{
			DoAsync(action, _eventThreadResponseName, _eventThreadResponseId, forceAsync);
		}

		public string GetThreadName(int threadId = 0)
		{
			if (threadId == 0)
				threadId = Thread.CurrentThread.ManagedThreadId;

			if (threadId == _eventThreadRequestId)
				return _eventThreadRequestName;
			if (threadId == _eventThreadResponseId)
				return _eventThreadResponseName;

			return "unknown ({0})".Put(threadId);
		}

		private void DoAsync(Action action, string queue, int threadId, bool forceAsync = false)
		{
			if (!forceAsync && Thread.CurrentThread.ManagedThreadId == threadId)
			{
				action();
			}
			else
			{
				Add(action, queue);
			}
		}

		private void DoSync(Action action, string queue, int threadId)
		{
			if (Thread.CurrentThread.ManagedThreadId == threadId)
			{
				action();
			}
			else
			{
				var done = new ManualResetEventSlim(false);

				Add(() =>
				{
					try
					{
						action();
					}
					finally
					{
						done.Set();
					}
				}, queue);

				done.Wait();
			}
		}
	}
}