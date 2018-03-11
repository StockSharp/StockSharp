namespace StockSharp.Algo.Candles
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Candles series holder to create <see cref="Candle"/> instancies.
	/// </summary>
	public class CandlesSeriesHolder
	{
		private Candle _currentCandle;

		/// <summary>
		/// Initializes a new instance of the <see cref="CandlesSeriesHolder"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public CandlesSeriesHolder(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			Series = series;
		}

		/// <summary>
		/// Candles series.
		/// </summary>
		public CandleSeries Series { get; }

		/// <summary>
		/// Update candle by new message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="candle">Updated candle.</param>
		/// <returns>Candles series.</returns>
		public CandleSeries UpdateCandle(CandleMessage message, out Candle candle)
		{
			candle = null;

			if (_currentCandle != null && _currentCandle.OpenTime == message.OpenTime)
			{
				if (_currentCandle.State == CandleStates.Finished)
					return null;

				_currentCandle.Update(message);
			}
			else
				_currentCandle = message.ToCandle(Series);

			candle = _currentCandle;
			return Series;
		}
	}

	/// <summary>
	/// Candles holder to create <see cref="Candle"/> instancies.
	/// </summary>
	public class CandlesHolder
	{
		private readonly SynchronizedDictionary<long, CandlesSeriesHolder> _holders = new SynchronizedDictionary<long, CandlesSeriesHolder>();

		/// <summary>
		/// Clear state.
		/// </summary>
		public void Clear() => _holders.Clear();

		/// <summary>
		/// Create new series tracking.
		/// </summary>
		/// <param name="transactionId">Request identifier.</param>
		/// <param name="series">Candles series.</param>
		public void CreateCandleSeries(long transactionId, CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (transactionId == 0)
				throw new ArgumentNullException(nameof(transactionId));

			_holders.Add(transactionId, new CandlesSeriesHolder(series));
		}

		/// <summary>
		/// Remove series tracking.
		/// </summary>
		/// <param name="transactionId">Request identifier.</param>
		/// <returns>Candles series.</returns>
		public CandleSeries RemoveCandleSeries(long transactionId)
		{
			CandlesSeriesHolder info;

			lock (_holders.SyncRoot)
				info = _holders.TryGetAndRemove(transactionId);

			return info?.Series;
		}

		/// <summary>
		/// Get request identifier by series.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Request identifier.</returns>
		public long TryGetTransactionId(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			lock (_holders.SyncRoot)
				return _holders.FirstOrDefault(p => p.Value.Series == series).Key;
		}

		/// <summary>
		/// Get series by request identifier.
		/// </summary>
		/// <param name="transactionId">Request identifier.</param>
		/// <returns>Candles series or <see langword="null"/> if identifier is non exist.</returns>
		public CandleSeries TryGetCandleSeries(long transactionId) => _holders.TryGetValue(transactionId)?.Series;

		/// <summary>
		/// Update candle by new message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="candle">Updated candle.</param>
		/// <returns>Candles series.</returns>
		public CandleSeries UpdateCandle(CandleMessage message, out Candle candle)
		{
			var info = _holders.TryGetValue(message.OriginalTransactionId);

			if (info != null)
				return info.UpdateCandle(message, out candle);

			candle = null;
			return null;
		}
	}
}