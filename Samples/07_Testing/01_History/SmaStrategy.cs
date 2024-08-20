namespace StockSharp.Samples.Testing.History
{
	using System;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Indicators;
	using StockSharp.Algo.Strategies;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	class SmaStrategy : Strategy
	{
		private bool? _isShortLessThenLong;

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
			this.AddInfoLog(LocalizedStrings.SmaNewCandleLog, candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.SecurityId);

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