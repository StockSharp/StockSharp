namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;

	partial class Strategy
	{
		private ICandleManager CandleManager => (ICandleManager)SafeGetConnector();

		/// <inheritdoc />
		int ICandleSource<Candle>.SpeedPriority => CandleManager.SpeedPriority;

		/// <inheritdoc />
		event Action<CandleSeries, Candle> ICandleSource<Candle>.Processing
		{
			add => CandleManager.Processing += value;
			remove => CandleManager.Processing -= value;
		}

		/// <inheritdoc />
		event Action<CandleSeries> ICandleSource<Candle>.Stopped
		{
			add => CandleManager.Stopped += value;
			remove => CandleManager.Stopped -= value;
		}

		/// <inheritdoc />
		IEnumerable<Range<DateTimeOffset>> ICandleSource<Candle>.GetSupportedRanges(CandleSeries series)
			=> CandleManager.GetSupportedRanges(series);

		/// <inheritdoc />
		[Obsolete("Use Subscribe method.")]
		public virtual void Start(CandleSeries series, DateTimeOffset? from = null, DateTimeOffset? to = null)
			=> CandleManager.Start(series, from, to);

		/// <inheritdoc />
		[Obsolete("Use UnSubscribe method.")]
		public virtual void Stop(CandleSeries series) => CandleManager.Stop(series);

		/// <inheritdoc />
		ICandleManagerContainer ICandleManager.Container => CandleManager.Container;

		/// <inheritdoc />
		IEnumerable<CandleSeries> ICandleManager.Series => CandleManager.Series;

		/// <inheritdoc />
		IList<ICandleSource<Candle>> ICandleManager.Sources => CandleManager.Sources;
	}
}