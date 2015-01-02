namespace StockSharp.Studio.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Windows.Media;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	[DisplayNameLoc(LocalizedStrings.Str3291Key)]
	[DescriptionLoc(LocalizedStrings.Str3292Key)]
	public class SmaStrategy : Strategy
	{
		private readonly SimpleMovingAverage _shortSma;
		private readonly SimpleMovingAverage _longSma;
		private CandleSeries _series;

		private ChartArea _area;

		public SmaStrategy()
		{
			_timeFrame = this.Param("TimeFrame", TimeSpan.FromMinutes(5));
			_longSmaPeriod = this.Param("LongSmaPeriod", 20).Optimize(15, 30);
			_shortSmaPeriod = this.Param("ShortSmaPeriod", 5).Optimize(3, 10);

			_shortSma = new SimpleMovingAverage();
			_longSma = new SimpleMovingAverage();
		}

		private readonly StrategyParam<TimeSpan> _timeFrame;

		[CategoryLoc(LocalizedStrings.Str3293Key)]
		[DisplayNameLoc(LocalizedStrings.Str1242Key)]
		[DescriptionLoc(LocalizedStrings.Str3294Key)]
		public TimeSpan TimeFrame
		{
			get { return _timeFrame.Value; }
			set { _timeFrame.Value = value; }
		}

		private readonly StrategyParam<int> _longSmaPeriod;

		[CategoryLoc(LocalizedStrings.Str3293Key)]
		[DisplayNameLoc(LocalizedStrings.LongKey)]
		[DescriptionLoc(LocalizedStrings.Str3295Key)]
		public int LongSmaPeriod
		{
			get { return _longSmaPeriod.Value; }
			set { _longSmaPeriod.Value = value; }
		}

		private readonly StrategyParam<int> _shortSmaPeriod;

		[CategoryLoc(LocalizedStrings.Str3293Key)]
		[DisplayNameLoc(LocalizedStrings.ShortKey)]
		[DescriptionLoc(LocalizedStrings.Str3296Key)]
		public int ShortSmaPeriod
		{
			get { return _shortSmaPeriod.Value; }
			set { _shortSmaPeriod.Value = value; }
		}

		protected override void OnStarted()
		{
			_series = new CandleSeries(typeof(TimeFrameCandle), Security, TimeFrame);

			_shortSma.Length = ShortSmaPeriod;
			_longSma.Length = LongSmaPeriod;

			if (_area == null)
			{
				_area = new ChartArea();

				_area.Elements.Add(new ChartCandleElement());
				_area.Elements.Add(new ChartIndicatorElement { Color = Colors.Green, StrokeThickness = 1 });
				_area.Elements.Add(new ChartIndicatorElement { Color = Colors.Red, StrokeThickness = 1 });
				_area.Elements.Add(new ChartTradeElement());

				new ChartAddAreaCommand(_area).Process(this);
			}

			this
				.WhenNewMyTrades()
				.Do(trades =>
				{
					foreach (var myTrade in trades)
					{
						new ChartDrawCommand(myTrade.Trade.Time, new Dictionary<IChartElement, object>
						{
							{ _area.Elements[3], myTrade }
						}).Process(this);
					}
				})
				.Apply(this);

			_series
				.WhenCandles()
				.Do(Process)
				.Apply(this);

			this.GetCandleManager().Start(_series);

			Security
				.WhenMarketDepthChanged(SafeGetConnector())
				.Do(md => new UpdateMarketDepthCommand(md).Process(this))
				.Apply(this);

			SafeGetConnector().RegisterMarketDepth(Security);

			base.OnStarted();
		}

		protected override void OnStopped()
		{
			SafeGetConnector().UnRegisterMarketDepth(Security);
			this.GetCandleManager().Stop(_series);

			base.OnStopped();
		}

		protected override void OnReseted()
		{
			if (_area != null)
				new ChartRemoveAreaCommand(_area).Process(this);

			_area = null;

			_shortSma.Reset();
			_longSma.Reset();

			base.OnReseted();
		}

		private void Process(Candle candle)
		{
			//this.AddInfoLog("Свеча {0}: {1};{2};{3};{4}; объем {5}", candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume);

			var isShortWasFormed = _shortSma.IsFormed;
			var isLongWasFormed = _longSma.IsFormed;

			var currentShort = _shortSma.Process(candle);
			var currentLong = _longSma.Process(candle);

			if (candle.State == CandleStates.Finished && isShortWasFormed && isLongWasFormed
				&& _longSma.Length > 1 && _shortSma.Length > 1 && candle.OpenTime > StartedTime)
			{
				Order order = null;

				var prevShort = _shortSma.GetValue(1);
				var prevLong = _longSma.GetValue(1);

				if (prevShort < prevLong && currentShort.GetValue<decimal>() > currentLong.GetValue<decimal>() && Position <= 0)
				{
					this.AddInfoLog(LocalizedStrings.Str3297);
					order = this.BuyAtMarket(Position == 0 ? Volume : Position.Abs() * 2);
				}
				else if (prevShort > prevLong && currentShort.GetValue<decimal>() < currentLong.GetValue<decimal>() && Position >= 0)
				{
					this.AddInfoLog(LocalizedStrings.Str3298);
					order = this.SellAtMarket(Position == 0 ? Volume : Position.Abs() * 2);
				}

				if (order != null)
					RegisterOrder(order);
			}

			new ChartDrawCommand(candle.OpenTime, new Dictionary<IChartElement, object>
			{
				{ _area.Elements[0], candle },
				{ _area.Elements[1], currentLong },
				{ _area.Elements[2], currentShort },
			}).Process(this);
		}
	}
}