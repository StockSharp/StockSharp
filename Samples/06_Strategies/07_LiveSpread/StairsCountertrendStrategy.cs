namespace StockSharp.Samples.Strategies.LiveSpread;

using System;
using System.Collections.Generic;

using Ecng.Logging;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

/// <summary>
/// Countertrend strategy that opens positions against established trends.
/// It counts consecutive bullish or bearish candles and takes opposite positions.
/// </summary>
public class StairsCountertrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleDataType;
	private readonly StrategyParam<int> _length;

	private int _bullLength;
	private int _bearLength;

	/// <summary>
	/// Initializes a new instance of <see cref="StairsCountertrendStrategy"/>.
	/// </summary>
	public StairsCountertrendStrategy()
	{
		_candleDataType = Param(nameof(CandleDataType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy calculation", "Base settings");

		_length = Param(nameof(Length), 2)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Number of consecutive candles to identify a trend", "Base settings")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);
	}

	/// <summary>
	/// Candle data type for subscription.
	/// </summary>
	public DataType CandleDataType
	{
		get => _candleDataType.Value;
		set => _candleDataType.Value = value;
	}

	/// <summary>
	/// Number of consecutive candles required to identify a trend.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Returns list of strategy's working securities and data types.
	/// </summary>
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleDataType) };
	}

	/// <summary>
	/// On strategy started.
	/// </summary>
	protected override void OnStarted(DateTimeOffset time)
	{
		// Reset counters on start
		_bullLength = 0;
		_bearLength = 0;

		// Subscribe to candles
		var subscription = SubscribeCandles(CandleDataType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		base.OnStarted(time);
	}

	/// <summary>
	/// Process a new candle.
	/// </summary>
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Identify bullish or bearish candle
		if (candle.OpenPrice < candle.ClosePrice)
		{
			_bullLength++;
			_bearLength = 0;

			this.AddInfoLog($"Bullish candle detected. Streak: {_bullLength}");
		}
		else if (candle.OpenPrice > candle.ClosePrice)
		{
			_bullLength = 0;
			_bearLength++;

			this.AddInfoLog($"Bearish candle detected. Streak: {_bearLength}");
		}

		// Check if we can trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_bullLength >= Length && Position >= 0)
		{
			// Bearish trend detected - go short
			decimal volume = Volume + Math.Abs(Position);
			this.AddInfoLog($"Selling {volume} after {_bullLength} bullish candles");
			SellMarket(volume);
		}
		else if (_bearLength >= Length && Position <= 0)
		{
			// Bullish trend detected - go long
			decimal volume = Volume + Math.Abs(Position);
			this.AddInfoLog($"Buying {volume} after {_bearLength} bearish candles");
			BuyMarket(volume);
		}
	}
}