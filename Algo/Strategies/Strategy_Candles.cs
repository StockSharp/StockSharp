namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;

	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Messages;

	partial class Strategy
	{
		private ICandleManager<ICandleMessage> CandleManager => (ICandleManager<ICandleMessage>)SafeGetConnector();

		/// <inheritdoc />
		int ICandleSource<ICandleMessage>.SpeedPriority => CandleManager.SpeedPriority;

		/// <inheritdoc />
		event Action<CandleSeries, ICandleMessage> ICandleSource<ICandleMessage>.Processing
		{
			add => CandleManager.Processing += value;
			remove => CandleManager.Processing -= value;
		}

		/// <inheritdoc />
		event Action<CandleSeries> ICandleSource<ICandleMessage>.Stopped
		{
			add => CandleManager.Stopped += value;
			remove => CandleManager.Stopped -= value;
		}

		/// <inheritdoc />
		IEnumerable<Range<DateTimeOffset>> ICandleSource<ICandleMessage>.GetSupportedRanges(CandleSeries series)
			=> CandleManager.GetSupportedRanges(series);

		/// <inheritdoc />
		[Obsolete("Use Subscribe method.")]
		public virtual void Start(CandleSeries series, DateTimeOffset? from = null, DateTimeOffset? to = null)
			=> CandleManager.Start(series, from, to);

		/// <inheritdoc />
		[Obsolete("Use UnSubscribe method.")]
		public virtual void Stop(CandleSeries series) => CandleManager.Stop(series);

		/// <inheritdoc />
		ICandleManagerContainer<ICandleMessage> ICandleManager<ICandleMessage>.Container => CandleManager.Container;

		/// <inheritdoc />
		IEnumerable<CandleSeries> ICandleManager<ICandleMessage>.Series => CandleManager.Series;

		/// <inheritdoc />
		IList<ICandleSource<ICandleMessage>> ICandleManager<ICandleMessage>.Sources => CandleManager.Sources;
	}
}