namespace StockSharp.Samples.Avalonia.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class EventSubscriptionTests
{
	[TestMethod]
	[Timeout(10_000)]
	public void AttachAndDispose_AreIdempotentAndRouteEachEventOnce()
	{
		var source = new EventSource();
		var routed = 0;
		void handler() => routed++;

		var subscription = new EventSubscription(
			() => source.Raised += handler,
			() => source.Raised -= handler);

		subscription.Attach();
		subscription.Attach();
		source.Raise();

		Assert.AreEqual(1, routed);
		Assert.AreEqual(1, source.AddCount);

		subscription.Dispose();
		subscription.Dispose();
		source.Raise();

		Assert.AreEqual(1, routed);
		Assert.AreEqual(1, source.RemoveCount);
	}

	private sealed class EventSource
	{
		private Action _raised;

		public int AddCount { get; private set; }

		public int RemoveCount { get; private set; }

		public event Action Raised
		{
			add
			{
				AddCount++;
				_raised += value;
			}
			remove
			{
				RemoveCount++;
				_raised -= value;
			}
		}

		public void Raise() => _raised?.Invoke();
	}
}
