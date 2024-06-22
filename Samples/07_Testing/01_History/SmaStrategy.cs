namespace StockSharp.Samples.Testing.History
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
	using StockSharp.Algo.Strategies.Protective;

	class SmaStrategy : Strategy
	{
		private readonly ProtectiveController _protectiveController = new();
		private IProtectivePositionController _posController;

		private IChart _chart;

		private readonly List<MyTrade> _myTrades = new();
		private bool? _isShortLessThenLong;
		private SimpleMovingAverage _shortSma;
		private SimpleMovingAverage _longSma;

		private IChartCandleElement _chartCandlesElem;
		private IChartTradeElement _chartTradesElem;
		private IChartIndicatorElement _chartLongElem;
		private IChartIndicatorElement _chartShortElem;

		public SmaStrategy()
        {
			_longSmaParam = this.Param(nameof(LongSma), 80);
			_shortSmaParam = this.Param(nameof(ShortSma), 30);
			_takeValue = this.Param(nameof(TakeValue), new Unit(0, UnitTypes.Absolute));
			_stopValue = this.Param(nameof(StopValue), new Unit(2, UnitTypes.Percent));
			_candleTypeParam = this.Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1))).NotNull();
			_candleTimeFrameParam = this.Param<TimeSpan?>(nameof(CandleTimeFrame));
			_buildFromParam = this.Param<DataType>(nameof(BuildFrom));
			_buildFieldParam = this.Param<Level1Fields?>(nameof(BuildField));
		}

		private readonly StrategyParam<TimeSpan?> _candleTimeFrameParam;

		public TimeSpan? CandleTimeFrame
		{
			get => _candleTimeFrameParam.Value;
			set => _candleTimeFrameParam.Value = value;
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

		private readonly StrategyParam<Unit> _takeValue;

		public Unit TakeValue
		{
			get => _takeValue.Value;
			set => _takeValue.Value = value;
		}

		private readonly StrategyParam<Unit> _stopValue;

		public Unit StopValue
		{
			get => _stopValue.Value;
			set => _stopValue.Value = value;
		}

		protected override void OnReseted()
		{
			base.OnReseted();

			_protectiveController.Clear();
			_posController = default;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// !!! DO NOT FORGET add it in case use IsFormed property (see code below)
			Indicators.Add(_longSma = new SimpleMovingAverage { Length = LongSma });
			Indicators.Add(_shortSma = new SimpleMovingAverage { Length = ShortSma });
			
			_chart = this.GetChart();

			if (_chart != null)
			{
				var area = _chart.AddArea();

				_chartCandlesElem = area.AddCandles();
				_chartTradesElem = area.AddTrades();
				_chartShortElem = area.AddIndicator(_shortSma);
				_chartLongElem = area.AddIndicator(_longSma);

				// make short line coral color
				_chartShortElem.Color = System.Drawing.Color.Coral;
			}

			var dt = CandleType;

			if (CandleTimeFrame is not null)
			{
				dt = DataType.Create(dt.MessageType, CandleTimeFrame);
			}

			var subscription = new Subscription(dt, Security)
			{
				MarketData =
				{
					IsFinishedOnly = true,
					BuildFrom = BuildFrom,
					BuildMode = BuildFrom is null ? MarketDataBuildModes.LoadAndBuild : MarketDataBuildModes.Build,
					BuildField = BuildField,
				}
			};

			subscription
				.WhenCandleReceived(this)
				.Do(ProcessCandle)
				.Apply(this);

			this
				.WhenNewMyTrade()
				.Do(t =>
				{
					_myTrades.Add(t);

					var security = t.Order.Security;
					var portfolio = t.Order.Portfolio;

					if (TakeValue.IsSet() || StopValue.IsSet())
					{
						_posController ??= _protectiveController.GetController(
							security.ToSecurityId(),
							portfolio.Name,
							new LocalProtectiveBehaviourFactory(security.PriceStep, security.Decimals),
							TakeValue, StopValue, true, default, default, true);
					}

					var info = _posController?.Update(t.Trade.Price, t.GetPosition());

					if (info is not null)
						ActiveProtection(info.Value);
				})
				.Apply(this);

			_isShortLessThenLong = null;

			Subscribe(subscription);
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// strategy are stopping
			if (ProcessState == ProcessStates.Stopping)
			{
				CancelActiveOrders();
				return;
			}

			this.AddInfoLog(LocalizedStrings.SmaNewCandleLog, candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.SecurityId);

			// try activate local stop orders (if they present)
			var info = _posController?.TryActivate(candle.ClosePrice, CurrentTime);

			if (info is not null)
				ActiveProtection(info.Value);

			// process new candle
			var longValue = _longSma.Process(candle);
			var shortValue = _shortSma.Process(candle);

			// all indicators added in OnStarted now is fully formed and we can use it
			// or user turned off allow trading
			if (this.IsFormedAndOnlineAndAllowTrading())
			{
				// in case we subscribed on non finished only candles
				if (candle.State == CandleStates.Finished)
				{
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
			}

			var trade = _myTrades.FirstOrDefault();
			_myTrades.Clear();

			if (_chart == null)
				return;

			var data = _chart.CreateData();

			data
				.Group(candle.OpenTime)
					.Add(_chartCandlesElem, candle)
					.Add(_chartShortElem, shortValue)
					.Add(_chartLongElem, longValue)
					.Add(_chartTradesElem, trade)
					;

			_chart.Draw(data);
		}

		private void ActiveProtection((bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition) info)
		{
			// sending protection (=closing position) order as regular order
			RegisterOrder(this.CreateOrder(info.side, info.price, info.volume));
		}
	}
}