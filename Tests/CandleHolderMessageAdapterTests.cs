namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Tests for <see cref="CandleHolderMessageAdapter"/>.
/// </summary>
[TestClass]
public class CandleHolderMessageAdapterTests : BaseTestClass
{
	private static readonly TimeSpan _tf1 = TimeSpan.FromMinutes(1);
	private static readonly TimeSpan _tf5 = TimeSpan.FromMinutes(5);
	private static readonly DateTime _time = new(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

	private static SecurityId CreateId(string code)
		=> new() { SecurityCode = code, BoardCode = BoardCodes.Test };

	private static MarketDataMessage CreateSubscribe(long transactionId, SecurityId securityId, TimeSpan timeFrame)
		=> new()
		{
			IsSubscribe = true,
			TransactionId = transactionId,
			SecurityId = securityId,
			DataType2 = timeFrame.TimeFrame(),
		};

	private static TimeFrameCandleMessage CreateCandle(long originalTransactionId, SecurityId securityId = default, TimeSpan? timeFrame = null)
	{
		var candle = new TimeFrameCandleMessage
		{
			OriginalTransactionId = originalTransactionId,
			SecurityId = securityId,
			OpenTime = _time,
			OpenPrice = 100,
			HighPrice = 105,
			LowPrice = 95,
			ClosePrice = 102,
			TotalVolume = 10,
			State = CandleStates.Finished,
		};

		if (timeFrame != null)
			candle.DataType = timeFrame.Value.TimeFrame();

		return candle;
	}

	[TestMethod]
	public async Task CandleTakesSecurityAndDataTypeFromItsSubscription()
	{
		var token = CancellationToken;

		var secId = CreateId("SBER");
		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new CandleHolderMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(CreateSubscribe(1, secId, _tf1), token);

		output.Clear();

		await inner.SendOutMessageAsync(CreateCandle(1), token);

		// The subscription said which security and which time-frame was asked for, so a candle that
		// arrives carrying neither is completed from it rather than reaching the caller unidentified.
		var candle = output.OfType<CandleMessage>().Single();
		candle.SecurityId.AssertEqual(secId);
		candle.DataType.AssertEqual(_tf1.TimeFrame());
	}

	[TestMethod]
	public async Task EachSubscriptionKeepsItsOwnMetadata()
	{
		var token = CancellationToken;

		var sber = CreateId("SBER");
		var gazp = CreateId("GAZP");

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new CandleHolderMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(CreateSubscribe(1, sber, _tf1), token);
		await adapter.SendInMessageAsync(CreateSubscribe(2, gazp, _tf5), token);

		output.Clear();

		await inner.SendOutMessageAsync(CreateCandle(2), token);
		await inner.SendOutMessageAsync(CreateCandle(1), token);

		// Two series run side by side; a candle is completed from the subscription its own transaction
		// id names, never from whichever series was registered first.
		var candles = output.OfType<CandleMessage>().ToArray();
		candles.Length.AssertEqual(2);

		candles[0].SecurityId.AssertEqual(gazp);
		candles[0].DataType.AssertEqual(_tf5.TimeFrame());

		candles[1].SecurityId.AssertEqual(sber);
		candles[1].DataType.AssertEqual(_tf1.TimeFrame());
	}

	[TestMethod]
	public async Task AllSecuritiesSubscriptionDoesNotRelabelCandles()
	{
		var token = CancellationToken;

		var sber = CreateId("SBER");
		var gazp = CreateId("GAZP");

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new CandleHolderMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		// A subscription with no security asks for candles of every instrument, so its candles arrive
		// already naming their own security.
		await adapter.SendInMessageAsync(CreateSubscribe(1, default, _tf1), token);

		output.Clear();

		await inner.SendOutMessageAsync(CreateCandle(1, sber), token);
		await inner.SendOutMessageAsync(CreateCandle(1, gazp), token);

		// Each candle keeps the security it was produced for. Stamping the second one with the first
		// one's security would turn a GAZP candle into a second, contradictory SBER candle.
		var candles = output.OfType<CandleMessage>().ToArray();
		candles.Length.AssertEqual(2);

		candles[0].SecurityId.AssertEqual(sber);
		candles[1].SecurityId.AssertEqual(gazp);
	}

	[TestMethod]
	public async Task ResetForgetsSubscriptionMetadata()
	{
		var token = CancellationToken;

		var sber = CreateId("SBER");
		var gazp = CreateId("GAZP");

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new CandleHolderMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(CreateSubscribe(1, sber, _tf1), token);
		await adapter.SendInMessageAsync(new ResetMessage(), token);

		output.Clear();

		await inner.SendOutMessageAsync(CreateCandle(1, gazp, _tf5), token);

		// Reset drops everything the adapter remembered, so a transaction id used again after it
		// carries none of the old series: the candle keeps the security and time-frame it came with.
		var candle = output.OfType<CandleMessage>().Single();
		candle.SecurityId.AssertEqual(gazp);
		candle.DataType.AssertEqual(_tf5.TimeFrame());
	}

	[TestMethod]
	public async Task FinishedSubscriptionMetadataIsNotAppliedToLaterCandles()
	{
		var token = CancellationToken;

		var sber = CreateId("SBER");
		var gazp = CreateId("GAZP");

		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new CandleHolderMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(CreateSubscribe(1, sber, _tf1), token);

		await inner.SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = 1 }, token);

		output.Clear();

		await inner.SendOutMessageAsync(CreateCandle(1, gazp, _tf5), token);

		// The series ended, so its identity is spent; anything arriving afterwards keeps its own.
		var candle = output.OfType<CandleMessage>().Single();
		candle.SecurityId.AssertEqual(gazp);
		candle.DataType.AssertEqual(_tf5.TimeFrame());
	}

	[TestMethod]
	public async Task CandleArrivingDuringUnsubscribeIsStillCompleted()
	{
		var token = CancellationToken;

		var secId = CreateId("SBER");
		var inner = new RecordingPassThroughMessageAdapter();
		using var adapter = new CandleHolderMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(CreateSubscribe(1, secId, _tf1), token);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 2,
			OriginalTransactionId = 1,
			SecurityId = secId,
			DataType2 = _tf1.TimeFrame(),
		}, token);

		output.Clear();

		await inner.SendOutMessageAsync(CreateCandle(1), token);

		// Unsubscribing is not instant: candles already in flight are delivered after it, and they are
		// still candles of that series, so they must be completed rather than passed on unidentified.
		var candle = output.OfType<CandleMessage>().Single();
		candle.SecurityId.AssertEqual(secId);
		candle.DataType.AssertEqual(_tf1.TimeFrame());
	}
}
