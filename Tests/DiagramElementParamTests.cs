namespace StockSharp.Tests;

using StockSharp.Diagram;

[TestClass]
public class DiagramElementParamTests : BaseTestClass
{
#pragma warning disable CS0618 // a value kept by somebody else's type may still persist itself through the obsolete contract
	private sealed class SyncSettings : IPersistable
	{
		public int Number { get; set; }

		void IPersistable.Load(SettingsStorage storage) => Number = storage.GetValue<int>(nameof(Number));
		void IPersistable.Save(SettingsStorage storage) => storage.Set(nameof(Number), Number);
	}
#pragma warning restore CS0618

	private sealed class AsyncSettings : IAsyncPersistable
	{
		public int Number { get; set; }

		Task IAsyncPersistable.LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
		{
			Number = storage.GetValue<int>(nameof(Number));
			return Task.CompletedTask;
		}

		Task IAsyncPersistable.SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
		{
			storage.Set(nameof(Number), Number);
			return Task.CompletedTask;
		}
	}

	private async Task<T> RoundTripAsync<T>(T value)
	{
		var source = new DiagramElementParam<T> { Name = "value", Value = value };
		var storage = new SettingsStorage();
		await source.SaveAsync(storage, CancellationToken);

		var loaded = new DiagramElementParam<T> { Name = "value" };
		await loaded.LoadAsync(storage, CancellationToken);

		return loaded.Value;
	}

	[TestMethod]
	public async Task ValueThatPersistsItself_RoundTrips()
	{
		var restored = await RoundTripAsync(new AsyncSettings { Number = 5 });

		restored.AssertNotNull();
		restored.Number.AssertEqual(5);
	}

	/// <summary>
	/// A value is written the way it is going to be read: loading takes any type that persists itself
	/// for a typed storage, so saving has to write one for the obsolete contract as well.
	/// </summary>
	[TestMethod]
	public async Task ValueThatPersistsThroughTheObsoleteContract_RoundTrips()
	{
		var restored = await RoundTripAsync(new SyncSettings { Number = 5 });

		restored.AssertNotNull();
		restored.Number.AssertEqual(5);
	}

	[TestMethod]
	public async Task PlainValue_RoundTrips()
		=> (await RoundTripAsync(42)).AssertEqual(42);
}
