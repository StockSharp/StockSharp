namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Samples.Candles.CombineHistoryRealtime.Avalonia;

[TestClass]
public class OwnedRuntimeLifetimeTests
{
	[TestMethod]
	[Timeout(10_000)]
	public void Dispose_ReleasesEveryDependencyOnceInOrder()
	{
		var calls = new List<string>();
		var context = new DisposalProbe("context", calls);
		var entityRegistry = new DisposalProbe("entity", calls);
		var storageRegistry = new DisposalProbe("storage", calls);
		var snapshotRegistry = new DisposalProbe("snapshot", calls);
		var executor = new DisposalProbe("executor", calls);
		var lifetime = new OwnedRuntimeLifetime(
			context,
			entityRegistry,
			storageRegistry,
			snapshotRegistry,
			executor);

		lifetime.Dispose();
		lifetime.Dispose();

		CollectionAssert.AreEqual(
			new[] { "context", "entity", "storage", "snapshot", "executor" },
			calls);
		Assert.AreEqual(1, context.DisposeCount);
		Assert.AreEqual(1, entityRegistry.DisposeAsyncCount);
		Assert.AreEqual(1, storageRegistry.DisposeCount);
		Assert.AreEqual(1, snapshotRegistry.DisposeCount);
		Assert.AreEqual(1, executor.DisposeAsyncCount);
	}

	private sealed class DisposalProbe(string name, ICollection<string> calls)
		: IDisposable, IAsyncDisposable
	{
		public int DisposeCount { get; private set; }

		public int DisposeAsyncCount { get; private set; }

		public void Dispose()
		{
			DisposeCount++;
			calls.Add(name);
		}

		public ValueTask DisposeAsync()
		{
			DisposeAsyncCount++;
			calls.Add(name);
			return default;
		}
	}
}
