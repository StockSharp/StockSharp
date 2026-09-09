namespace StockSharp.Tests;

using StockSharp.Algo.Testing;

/// <summary>
/// A replay period almost always contains days the venue was closed. Those days carry no data, and the
/// replay stands in a synthetic timeline for them - which has to belong to the day being replayed, not to
/// wherever the run began.
/// </summary>
[TestClass]
[DoNotParallelize]
public class HistoryReplayGapTests : BaseTestClass
{
	// The bundled SBER candles cover 1-3 and 6 December 2021: the weekend in between is the gap.
	private static readonly DateTime _from = new(2021, 12, 1, 0, 0, 0, DateTimeKind.Utc);
	private static readonly DateTime _to = new(2021, 12, 7, 0, 0, 0, DateTimeKind.Utc);

	[TestMethod]
	[Timeout(120_000)]
	public async Task AReplayAcrossADayWithoutDataReachesItsEnd()
	{
		var security = new Security
		{
			Id = "SBER@MICEX",
			PriceStep = 0.01m,
			VolumeStep = 1m,
			Board = ExchangeBoard.MicexEqbr,
		};
		var portfolio = new Portfolio { Name = "emulation", BeginValue = 1_000_000m };

		using var connector = new HistoryEmulationConnector(
			new CollectionSecurityProvider([security]),
			new CollectionPortfolioProvider([portfolio]),
			Helper.GetResourceStorage())
		{
			HistoryMessageAdapter =
			{
				StartDate = _from,
				StopDate = _to,
				StorageFormat = StorageFormats.Binary,
			},
		};

		var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
		var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		connector.Error += error => failure.TrySetResult(error);
		connector.ConnectionError += error => failure.TrySetResult(error);
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				finished.TrySetResult();
		};

		connector.Connect();
		connector.Subscribe(new(TimeSpan.FromMinutes(15).TimeFrame(), security));
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(finished.Task, failure.Task).WaitAsync(TimeSpan.FromSeconds(90), CancellationToken);

		if (ReferenceEquals(completed, failure.Task))
			throw new AssertFailedException($"The replay failed on a day without data: {await failure.Task}");

		IsTrue(connector.IsFinished, "A replay that reached its stop date is finished.");
	}
}
