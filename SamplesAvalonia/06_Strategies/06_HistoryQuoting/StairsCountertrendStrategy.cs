namespace StockSharp.Samples.Strategies.HistoryQuoting.Avalonia;

using System;
using System.Collections.Generic;

using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

internal sealed class StairsCountertrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleDataType;
	private readonly StrategyParam<int> _length;
	private QuotingProcessor _quotingProcessor;
	private int _bullLength;
	private int _bearLength;

	public StairsCountertrendStrategy()
	{
		_candleDataType = Param(nameof(CandleDataType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy calculation", "Base settings");
		_length = Param(nameof(Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Number of consecutive candles to identify a trend", "Base settings")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);
	}

	public DataType CandleDataType
	{
		get => _candleDataType.Value;
		set => _candleDataType.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleDataType)];

	protected override void OnStarted2(DateTime time)
	{
		_bullLength = 0;
		_bearLength = 0;
		var subscription = SubscribeCandles(CandleDataType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenPrice < candle.ClosePrice)
		{
			_bullLength++;
			_bearLength = 0;
		}
		else if (candle.OpenPrice > candle.ClosePrice)
		{
			_bullLength = 0;
			_bearLength++;
		}

		if (_quotingProcessor is not null &&
			(_bullLength >= Length && Position >= 0 || _bearLength >= Length && Position <= 0))
		{
			_quotingProcessor.Dispose();
			_quotingProcessor = null;
		}

		if (_quotingProcessor is not null || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_bullLength >= Length && Position >= 0)
			CreateQuotingProcessor(Sides.Sell);
		else if (_bearLength >= Length && Position <= 0)
			CreateQuotingProcessor(Sides.Buy);
	}

	private void CreateQuotingProcessor(Sides side)
	{
		var behavior = new MarketQuotingBehavior(
			0,
			new Unit(0.1m, UnitTypes.Percent),
			MarketPriceTypes.Following);
		var processor = new QuotingProcessor(
			behavior,
			Security,
			Portfolio,
			side,
			Volume,
			Volume,
			TimeSpan.Zero,
			this,
			this,
			this,
			this,
			this,
			IsFormedAndOnlineAndAllowTrading,
			true,
			true)
		{
			Parent = this,
		};
		_quotingProcessor = processor;

		processor.OrderRegistered += order =>
			this.AddInfoLog($"Order {order.TransactionId} registered at price {order.Price}");
		processor.OrderFailed += fail =>
			this.AddInfoLog($"Order failed: {fail.Error?.Message}");
		processor.OwnTrade += trade =>
			this.AddInfoLog($"Trade executed: {trade.Trade.Volume} at {trade.Trade.Price}");
		processor.Finished += isOk =>
		{
			if (ReferenceEquals(_quotingProcessor, processor))
				_quotingProcessor = null;
			processor.Dispose();
		};
		processor.Start();
	}

	protected override void OnStopped()
	{
		_quotingProcessor?.Dispose();
		_quotingProcessor = null;
		base.OnStopped();
	}
}
