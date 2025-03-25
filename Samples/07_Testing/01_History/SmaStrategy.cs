namespace StockSharp.Samples.Testing.History
{
	using System;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	class SmaStrategy : Strategy
	{
		private bool? _isShortLessThenLong;

		public SmaStrategy()
        {
			_longSma = Param(nameof(LongSma), 80);
			_shortSma = Param(nameof(ShortSma), 30);
			_takeValue = Param(nameof(TakeValue), new Unit(0, UnitTypes.Absolute));
			_stopValue = Param(nameof(StopValue), new Unit(2, UnitTypes.Percent));
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetRequired();
			_candleTimeFrame = Param<TimeSpan?>(nameof(CandleTimeFrame));
			_buildFrom = Param<DataType>(nameof(BuildFrom));
			_buildField = Param<Level1Fields?>(nameof(BuildField));
		}

		private readonly StrategyParam<TimeSpan?> _candleTimeFrame;

		public TimeSpan? CandleTimeFrame
		{
			get => _candleTimeFrame.Value;
			set => _candleTimeFrame.Value = value;
		}

		private readonly StrategyParam<int> _longSma;

		public int LongSma
		{
			get => _longSma.Value;
			set => _longSma.Value = value;
		}

		private readonly StrategyParam<int> _shortSma;

		public int ShortSma
		{
			get => _shortSma.Value;
			set => _shortSma.Value = value;
		}

		private readonly StrategyParam<DataType> _candleType;

		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		private readonly StrategyParam<DataType> _buildFrom;

		public DataType BuildFrom
		{
			get => _buildFrom.Value;
			set => _buildFrom.Value = value;
		}

		private readonly StrategyParam<Level1Fields?> _buildField;

		public Level1Fields? BuildField
		{
			get => _buildField.Value;
			set => _buildField.Value = value;
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

			_isShortLessThenLong = null;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// ------- initiate subscription ---------

			var dt = CandleTimeFrame is null
				? CandleType
				: DataType.Create(CandleType.MessageType, CandleTimeFrame);

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

			// ----------------------------------------

			// ---------- create indicators -----------

			var longSma = new SMA { Length = LongSma };
			var shortSma = new SMA { Length = ShortSma };

			// ----------------------------------------

			// --- bind candles set and indicators ----

			SubscribeCandles(subscription)
				// bind indicators to the candles
				.Bind(longSma, shortSma, OnProcess)
				// start processing
				.Start();

			// ----------------------------------------

			// ----------- configure chart ------------

			var area = CreateChartArea();

			// area can be null in case of no GUI (strategy hosted in Runner or in own console app)
			if (area != null)
			{
				DrawCandles(area, subscription);
				
				DrawIndicator(area, shortSma, System.Drawing.Color.Coral);
				DrawIndicator(area, longSma);

				DrawOwnTrades(area);
			}

			// ----------------------------------------

			// ---- configure position protection -----

			// start protection by take profit and-or stop loss
			StartProtection(TakeValue, StopValue);

			// ----------------------------------------
		}

		private void OnProcess(ICandleMessage candle, decimal longValue, decimal shortValue)
		{
			LogInfo(LocalizedStrings.SmaNewCandleLog, candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.SecurityId);

			// in case we subscribed on non finished only candles
			if (candle.State != CandleStates.Finished)
				return;

			// calc new values for short and long
			var isShortLessThenLong = shortValue < longValue;

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

				if (direction == Sides.Buy)
					BuyLimit(price, volume);
				else
					SellLimit(price, volume);

				// store current values for short and long
				_isShortLessThenLong = isShortLessThenLong;
			}
		}
	}
}