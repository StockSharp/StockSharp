namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SampleUiEventRouterTests
{
	[TestMethod]
	[Timeout(10_000)]
	public void BackgroundCallback_IsQueuedAndRoutedExactlyOnce()
	{
		var dispatcher = new RecordingDispatcher { HasAccess = false };
		using var router = new SampleUiEventRouter(dispatcher);
		var routed = 0;

		router.Dispatch(() => routed++);

		Assert.AreEqual(0, routed);
		Assert.AreEqual(1, dispatcher.PendingCount);
		dispatcher.Drain();
		Assert.AreEqual(1, routed);
	}

	[TestMethod]
	[Timeout(10_000)]
	public void Dispose_DropsAlreadyQueuedAndFutureCallbacks()
	{
		var dispatcher = new RecordingDispatcher { HasAccess = false };
		var router = new SampleUiEventRouter(dispatcher);
		var routed = 0;

		router.Dispatch(() => routed++);
		router.Dispose();
		dispatcher.Drain();
		router.Dispatch(() => routed++);

		Assert.AreEqual(0, routed);
		Assert.AreEqual(0, dispatcher.PendingCount);
	}

	private sealed class RecordingDispatcher : ISampleUiDispatcher
	{
		private readonly Queue<Action> _pending = new();

		public bool HasAccess { get; init; }

		public int PendingCount => _pending.Count;

		public bool CheckAccess() => HasAccess;

		public void Post(Action action) => _pending.Enqueue(action);

		public void Drain()
		{
			while (_pending.Count > 0)
				_pending.Dequeue()();
		}
	}
}
