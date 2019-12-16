namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Candles series holder to create <see cref="Candle"/> instances.
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
			Series = series ?? throw new ArgumentNullException(nameof(series));
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
		public bool UpdateCandle(CandleMessage message, out Candle candle)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			candle = null;

			if (_currentCandle != null && _currentCandle.OpenTime == message.OpenTime)
			{
				if (_currentCandle.State == CandleStates.Finished)
					return false;

				_currentCandle.Update(message);
			}
			else
				_currentCandle = message.ToCandle(Series);

			candle = _currentCandle;
			return true;
		}
	}

	/// <summary>
	/// Candles holder to create <see cref="Candle"/> instances.
	/// </summary>
	public class CandlesHolder
	{
		private readonly CachedSynchronizedDictionary<long, CandlesSeriesHolder> _holders = new CachedSynchronizedDictionary<long, CandlesSeriesHolder>();

		/// <summary>
		/// List of all candles series, subscribed via <see cref="CreateCandleSeries"/>.
		/// </summary>
		public IEnumerable<CandleSeries> AllCandleSeries => _holders.CachedValues.Select(h => h.Series);

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
			if (transactionId == 0)
				throw new ArgumentNullException(nameof(transactionId));

			_holders.Add(transactionId, new CandlesSeriesHolder(series));
		}

		/// <summary>
		/// Remove series tracking.
		/// </summary>
		/// <param name="transactionId">Request identifier.</param>
		/// <returns>Candles series.</returns>
		public CandleSeries TryRemoveCandleSeries(long transactionId)
		{
			lock (_holders.SyncRoot)
				return _holders.TryGetAndRemove(transactionId)?.Series;
		}

		/// <summary>
		/// Get request identifier by series.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Request identifier.</returns>
		public long? TryGetTransactionId(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			lock (_holders.SyncRoot)
				return _holders.CachedPairs.Where(p => p.Value.Series == series).FirstOr()?.Key;
		}

		/// <summary>
		/// Get series by request identifier.
		/// </summary>
		/// <param name="transactionId">Request identifier.</param>
		/// <returns>Candles series or <see langword="null"/> if identifier is non exist.</returns>
		public CandleSeries TryGetCandleSeries(long transactionId) => _holders.TryGetValue(transactionId)?.Series;

		/// <summary>
		/// Update candles by new message.
		/// </summary>
		/// <param name="transactionId">Request identifier.</param>
		/// <param name="message">Message.</param>
		/// <returns>Candles series.</returns>
		public Tuple<CandleSeries, Candle> UpdateCandles(long transactionId, CandleMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var info = _holders.TryGetValue(transactionId);

			if (info == null)
				return null;
					
			if (!info.UpdateCandle(message, out var candle))
				return null;
				
			return Tuple.Create(info.Series, candle);
		}
	}
}