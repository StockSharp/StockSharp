namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The market data buffer.
	/// </summary>
	/// <typeparam name="TKey">The key type.</typeparam>
	/// <typeparam name="TMarketData">Market data type.</typeparam>
	public class MarketDataBuffer<TKey, TMarketData>
	{
		private readonly SynchronizedDictionary<TKey, List<TMarketData>> _data = new SynchronizedDictionary<TKey, List<TMarketData>>();

		/// <summary>
		/// The buffer size.
		/// </summary>
		public int Size { get; set; }

		/// <summary>
		/// To add new information to the buffer.
		/// </summary>
		/// <param name="key">The key possessing new information.</param>
		/// <param name="data">New information.</param>
		public void Add(TKey key, TMarketData data)
		{
			Add(key, new[] { data });
		}

		/// <summary>
		/// To add new information to the buffer.
		/// </summary>
		/// <param name="key">The key possessing new information.</param>
		/// <param name="data">New information.</param>
		public void Add(TKey key, IEnumerable<TMarketData> data)
		{
			_data.SyncDo(d => d.SafeAdd(key).AddRange(data));
		}

		/// <summary>
		/// To get accumulated data from the buffer and delete them.
		/// </summary>
		/// <returns>Gotten data.</returns>
		public IDictionary<TKey, IEnumerable<TMarketData>> Get()
		{
			return _data.SyncGet(d =>
			{
				var retVal = d.ToDictionary(p => p.Key, p => (IEnumerable<TMarketData>)p.Value);
				d.Clear();
				return retVal;
			});
		}

		/// <summary>
		/// To get accumulated data from the buffer and delete them.
		/// </summary>
		/// <param name="key">The key possessing market data.</param>
		/// <returns>Gotten data.</returns>
		public IEnumerable<TMarketData> Get(TKey key)
		{
			if (key.IsDefault())
				throw new ArgumentNullException("key");

			return _data.SyncGet(d =>
			{
				var data = d.TryGetValue(key);

				if (data != null)
				{
					var retVal = data.CopyAndClear();
					d.Remove(key);
					return retVal;
				}

				return Enumerable.Empty<TMarketData>();
			});
		}
	}

	/// <summary>
	/// The market data buffer.
	/// </summary>
	/// <typeparam name="TMarketData">Market data type.</typeparam>
	public class MarketDataBuffer<TMarketData> : MarketDataBuffer<Security, TMarketData>
	{
	}
}