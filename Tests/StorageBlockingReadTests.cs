namespace StockSharp.Tests;

using System.Runtime.CompilerServices;

/// <summary>
/// The blocking readers kept for code written before the storages became asynchronous are called from wherever
/// that code runs, a window's own thread included. A storage that answers later, as one on another machine does,
/// must not need that thread back to finish the read.
/// </summary>
[TestClass]
public class StorageBlockingReadTests : BaseTestClass
{
	private static readonly TimeSpan _pause = TimeSpan.FromMilliseconds(20);
	private static readonly DateTime _date = new(2025, 3, 4, 0, 0, 0, DateTimeKind.Utc);
	private static readonly SecurityId _securityId = new() { SecurityCode = "TEST", BoardCode = BoardCodes.Test };

	// A window's thread as a read sees it: what is posted to it runs only once the thread is free again.
	private sealed class WaitingContext : SynchronizationContext
	{
		private int _posted;

		public int Posted => Volatile.Read(ref _posted);

		public override void Post(SendOrPostCallback callback, object state)
			=> Interlocked.Increment(ref _posted);
	}

	private sealed class LateStorage(ExecutionMessage[] ticks) : IMarketDataStorage<ExecutionMessage>
	{
		private readonly ExecutionMessage[] _ticks = ticks ?? throw new ArgumentNullException(nameof(ticks));

		IAsyncEnumerable<DateTime> IMarketDataStorage.GetDatesAsync()
			=> LateOne(_date);

		DataType IMarketDataStorage.DataType => DataType.Ticks;
		SecurityId IMarketDataStorage.SecurityId => _securityId;
		IMarketDataStorageDrive IMarketDataStorage.Drive => Mock.Of<IMarketDataStorageDrive>();
		bool IMarketDataStorage.AppendOnlyNew { get; set; }

		IMarketDataSerializer IMarketDataStorage.Serializer
			=> Mock.Of<IMarketDataSerializer>(s => s.TimePrecision == TimeSpan.FromTicks(1));

		IMarketDataSerializer<ExecutionMessage> IMarketDataStorage<ExecutionMessage>.Serializer
			=> Mock.Of<IMarketDataSerializer<ExecutionMessage>>();

		IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date)
			=> Late(_ticks);

		IAsyncEnumerable<ExecutionMessage> IMarketDataStorage<ExecutionMessage>.LoadAsync(DateTime date)
			=> Late(_ticks);

		async ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken)
		{
			await Task.Delay(_pause, cancellationToken);

			var first = _ticks[0].ServerTime;
			var last = _ticks[^1].ServerTime;

			return Mock.Of<IMarketDataMetaInfo>(i => i.FirstTime == first && i.LastTime == last);
		}

		ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => new(0);
		ValueTask<int> IMarketDataStorage<ExecutionMessage>.SaveAsync(IEnumerable<ExecutionMessage> data, CancellationToken cancellationToken) => new(0);
		ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => default;
		ValueTask IMarketDataStorage<ExecutionMessage>.DeleteAsync(IEnumerable<ExecutionMessage> data, CancellationToken cancellationToken) => default;
		ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => default;
	}

	// Every item comes after a pause, the way an answer from another machine does.
	private static async IAsyncEnumerable<T> Late<T>(T[] items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		foreach (var item in items)
		{
			await Task.Delay(_pause, cancellationToken);
			yield return item;
		}
	}

	private static IAsyncEnumerable<T> LateOne<T>(T item)
		=> Late(new[] { item });

	private static ExecutionMessage[] Ticks()
		=>
		[
			Tick(1, 100),
			Tick(2, 101),
			Tick(3, 102),
		];

	private static ExecutionMessage Tick(int hour, decimal price)
		=> new()
		{
			SecurityId = _securityId,
			DataTypeEx = DataType.Ticks,
			ServerTime = _date.AddHours(hour),
			TradePrice = price,
			TradeVolume = 1,
		};

	// Runs a read the way a window would: on a thread whose context nobody else serves while the read blocks it.
	private T OnAWindowsThread<T>(Func<T> read)
	{
		var context = new WaitingContext();
		var result = default(T);
		Exception failure = null;

		var thread = new Thread(() =>
		{
			SynchronizationContext.SetSynchronizationContext(context);

			try
			{
				result = read();
			}
			catch (Exception error)
			{
				failure = error;
			}
		})
		{
			IsBackground = true,
		};

		thread.Start();

		IsTrue(thread.Join(TimeSpan.FromSeconds(10)), "The read is waiting for the very thread it blocks.");
		IsNull(failure, failure?.ToString());
		AreEqual(0, context.Posted, "The read left work for the thread it was blocking.");

		return result;
	}

#pragma warning disable CS0618 // The blocking readers are what is being tested.

	[TestMethod]
	public void DatesAreReadWithoutTheCallersThread()
	{
		var storage = new LateStorage(Ticks());

		var dates = OnAWindowsThread(() => ((IMarketDataStorage)storage).GetDates().ToArray());

		AreEqual(1, dates.Length);
		AreEqual(_date, dates[0]);
	}

	[TestMethod]
	public void DatesOfARangeAreReadWithoutTheCallersThread()
	{
		var storage = new LateStorage(Ticks());

		var dates = OnAWindowsThread(() => storage.GetDates(_date.AddDays(-1), _date.AddDays(1)).ToArray());

		AreEqual(1, dates.Length);
		AreEqual(_date, dates[0]);
	}

	[TestMethod]
	public void ADayIsReadWithoutTheCallersThread()
	{
		var ticks = Ticks();
		var storage = new LateStorage(ticks);

		var typed = OnAWindowsThread(() => storage.Load(_date).ToArray());
		var untyped = OnAWindowsThread(() => ((IMarketDataStorage)storage).Load(_date).ToArray());

		IsTrue(typed.SequenceEqual(ticks));
		IsTrue(untyped.SequenceEqual(ticks));
	}

	[TestMethod]
	public void ARangeIsReadWithoutTheCallersThread()
	{
		var ticks = Ticks();
		var storage = new LateStorage(ticks);

		var loaded = OnAWindowsThread(() => storage.Load(_date, _date.AddDays(1)).ToArray());

		IsTrue(loaded.SequenceEqual(ticks), "read: " + string.Join(", ", loaded.Select(t => t.TradePrice)));
	}

	[TestMethod]
	public void TheFirstItemOfARangeIsReadWithoutTheCallersThread()
	{
		var storage = new LateStorage(Ticks());

		// What a pane asks before it offers an export.
		IsTrue(OnAWindowsThread(() => storage.Load(_date, _date.AddDays(1)).Any()));
	}

	[TestMethod]
	public void ADrivesDatesAreReadWithoutTheCallersThread()
	{
		var drive = new Mock<IMarketDataStorageDrive>();
		drive.Setup(d => d.GetDatesAsync()).Returns(() => LateOne(_date));

		var dates = OnAWindowsThread(() => drive.Object.GetDates().ToArray());

		AreEqual(1, dates.Length);
		AreEqual(_date, dates[0]);
	}

	[TestMethod]
	public void ADrivesDataTypesAreReadWithoutTheCallersThread()
	{
		var drive = new Mock<IMarketDataDrive>();
		drive
			.Setup(d => d.GetAvailableDataTypesAsync(It.IsAny<SecurityId>(), It.IsAny<StorageFormats>()))
			.Returns(() => Late(new[] { DataType.Ticks, DataType.Level1 }));

		var types = OnAWindowsThread(() => drive.Object.GetAvailableDataTypes(_securityId, StorageFormats.Binary).ToArray());

		IsTrue(types.SequenceEqual([DataType.Ticks, DataType.Level1]));
	}

	[TestMethod]
	public void AReadAbandonedHalfWayLetsGoOfTheStorage()
	{
		var ticks = Ticks();
		var disposed = false;

		async IAsyncEnumerable<ExecutionMessage> Watched([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			try
			{
				foreach (var tick in ticks)
				{
					await Task.Delay(_pause, cancellationToken);
					yield return tick;
				}
			}
			finally
			{
				await Task.Delay(_pause, CancellationToken.None);
				disposed = true;
			}
		}

		var storage = new Mock<IMarketDataStorage>();
		storage.Setup(s => s.LoadAsync(_date)).Returns(() => Watched());

		var first = OnAWindowsThread(() => storage.Object.Load(_date).First());

		AreSame(ticks[0], first);
		IsTrue(disposed, "The storage was left open by a read that stopped early.");
	}

	[TestMethod]
	public void AFailureOfTheStorageReachesTheReaderAsItIs()
	{
		static async IAsyncEnumerable<DateTime> Failing()
		{
			await Task.Delay(_pause);
			yield return _date;
			throw new InvalidOperationException("the other side went away");
		}

		var drive = new Mock<IMarketDataStorageDrive>();
		drive.Setup(d => d.GetDatesAsync()).Returns(Failing);

		var error = OnAWindowsThread<Exception>(() =>
		{
			try
			{
				_ = drive.Object.GetDates().ToArray();
				return null;
			}
			catch (Exception e)
			{
				return e;
			}
		});

		IsNotNull(error);
		IsTrue(error is InvalidOperationException, error.ToString());
		AreEqual("the other side went away", error.Message);
	}

#pragma warning restore CS0618
}
