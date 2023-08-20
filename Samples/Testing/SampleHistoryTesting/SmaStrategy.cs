#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleHistoryTesting.SampleHistoryTestingPublic
File: SmaStrategy.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleHistoryTesting
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Charting;

	class SmaStrategy : Strategy
	{
		private IChart _chart;

		private readonly List<MyTrade> _myTrades = new();
		private bool? _isShortLessThenLong;
		private SimpleMovingAverage _shortSma;
		private SimpleMovingAverage _longSma;

		public IChartCandleElement ChartCandlesElem { get; set; }
		public IChartTradeElement ChartTradesElem { get; set; }
		public IChartIndicatorElement ChartLongElem { get; set; }
		public IChartIndicatorElement ChartShortElem { get; set; }

		public SmaStrategy()
        {
			_longSmaParam = this.Param(nameof(LongSma), 80);
			_shortSmaParam = this.Param(nameof(ShortSma), 30);
			_candleTypeParam = this.Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)));
			_buildFromParam = this.Param<DataType>(nameof(BuildFrom));
			_buildFieldParam = this.Param<Level1Fields?>(nameof(BuildField));

			_candleTypeParam.AllowNull = false;
		}

		private readonly StrategyParam<int> _longSmaParam;

		public int LongSma
		{
			get => _longSmaParam.Value;
			set => _longSmaParam.Value = value;
		}

		private readonly StrategyParam<int> _shortSmaParam;

		public int ShortSma
		{
			get => _shortSmaParam.Value;
			set => _shortSmaParam.Value = value;
		}

		private readonly StrategyParam<DataType> _candleTypeParam;

		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		private readonly StrategyParam<DataType> _buildFromParam;

		public DataType BuildFrom
		{
			get => _buildFromParam.Value;
			set => _buildFromParam.Value = value;
		}

		private readonly StrategyParam<Level1Fields?> _buildFieldParam;

		public Level1Fields? BuildField
		{
			get => _buildFieldParam.Value;
			set => _buildFieldParam.Value = value;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			// !!! DO NOT FORGET add it in case use IsFormed property (see code below)
			Indicators.Add(_longSma = new SimpleMovingAverage { Length = LongSma });
			Indicators.Add(_shortSma = new SimpleMovingAverage { Length = ShortSma });

			_chart = this.GetChart();

			var subscription = new Subscription(CandleType, Security)
			{
				MarketData =
				{
					IsFinishedOnly = true,
					BuildFrom = BuildFrom,
					BuildMode = BuildFrom is null ? MarketDataBuildModes.Load : MarketDataBuildModes.Build,
					BuildField = BuildField,
				}
			};

			subscription
				.WhenCandleReceived(this)
				.Do(ProcessCandle)
				.Apply(this);

			this
				.WhenNewMyTrade()
				.Do(_myTrades.Add)
				.Apply(this);

			_isShortLessThenLong = null;

			Subscribe(subscription);

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// strategy are stopping
			if (ProcessState == ProcessStates.Stopping)
			{
				CancelActiveOrders();
				return;
			}

			this.AddInfoLog(LocalizedStrings.Str3634Params, candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.SecurityId);

			// process new candle
			var longValue = _longSma.Process(candle);
			var shortValue = _shortSma.Process(candle);

			// all indicators added in OnStarted now is fully formed and we can use it
			// or user turned off allow trading
			if (this.IsFormedAndAllowTrading())
			{
				// in case we subscribed on non finished only candles
				if (candle.State != CandleStates.Finished)
					return;

				// calc new values for short and long
				var isShortLessThenLong = shortValue.GetValue<decimal>() < longValue.GetValue<decimal>();

				if (_isShortLessThenLong == null)
				{
					_isShortLessThenLong = isShortLessThenLong;
				}
				else if (_isShortLessThenLong != isShortLessThenLong) // crossing happened
				{
					// if short less than long, the sale, otherwise buy
					var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;

					// calc size for open position or revert
					var volume = Position == 0 ? Volume : Position.Abs().Min(Volume) * 2;

					// calc order price as a close price
					var price = candle.ClosePrice;

					RegisterOrder(this.CreateOrder(direction, price, volume));

					// or revert position via market quoting
					//var strategy = new MarketQuotingStrategy(direction, volume);
					//ChildStrategies.Add(strategy);

					// store current values for short and long
					_isShortLessThenLong = isShortLessThenLong;
				}
			}

			var trade = _myTrades.FirstOrDefault();
			_myTrades.Clear();

			if (_chart == null)
				return;

			var data = _chart.CreateData();

			data
				.Group(candle.OpenTime)
					.Add(ChartCandlesElem, candle)
					.Add(ChartShortElem, shortValue)
					.Add(ChartLongElem, longValue)
					.Add(ChartTradesElem, trade)
					;

			_chart.Draw(data);
		}
	}
}