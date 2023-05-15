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
		event Action<CandleSeries, ICandleMessage> ICandleManager<ICandleMessage>.Processing
		{
			add => CandleManager.Processing += value;
			remove => CandleManager.Processing -= value;
		}

		/// <inheritdoc />
		event Action<CandleSeries> ICandleManager<ICandleMessage>.Stopped
		{
			add => CandleManager.Stopped += value;
			remove => CandleManager.Stopped -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use Subscribe method.")]
		public virtual void Start(CandleSeries series, DateTimeOffset? from = null, DateTimeOffset? to = null)
			=> CandleManager.Start(series, from, to);

		/// <inheritdoc />
		[Obsolete("Use UnSubscribe method.")]
		public virtual void Stop(CandleSeries series) => CandleManager.Stop(series);

		/// <inheritdoc />
		IEnumerable<CandleSeries> ICandleManager<ICandleMessage>.Series => CandleManager.Series;
	}
}