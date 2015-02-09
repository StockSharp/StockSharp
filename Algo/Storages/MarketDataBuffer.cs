namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Буфер маркет-данных.
	/// </summary>
	/// <typeparam name="TKey">Тип ключа.</typeparam>
	/// <typeparam name="TMarketData">Тип маркет-данных.</typeparam>
	public class MarketDataBuffer<TKey, TMarketData>
	{
		private readonly SynchronizedDictionary<TKey, List<TMarketData>> _data = new SynchronizedDictionary<TKey, List<TMarketData>>();

		/// <summary>
		/// Размер буфера.
		/// </summary>
		public int Size { get; set; }

		/// <summary>
		/// Добавить новую информацию в буфер.
		/// </summary>
		/// <param name="key">Ключ, которому принадлежит новая информация.</param>
		/// <param name="data">Новая информация.</param>
		public void Add(TKey key, TMarketData data)
		{
			Add(key, new[] { data });
		}

		/// <summary>
		/// Добавить новую информацию в буфер.
		/// </summary>
		/// <param name="key">Ключ, которому принадлежит новая информация.</param>
		/// <param name="data">Новая информация.</param>
		public void Add(TKey key, IEnumerable<TMarketData> data)
		{
			_data.SyncDo(d => d.SafeAdd(key).AddRange(data));
		}

		/// <summary>
		/// Получить накопленные данные из буфера и удалить их.
		/// </summary>
		/// <returns>Полученные данные.</returns>
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
		/// Получить накопленные данные из буфера и удалить их.
		/// </summary>
		/// <param name="key">Ключ, которому принадлежат маркет-данных.</param>
		/// <returns>Полученные данные.</returns>
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
	/// Буфер маркет-данных.
	/// </summary>
	/// <typeparam name="TMarketData">Тип маркет-данных.</typeparam>
	public class MarketDataBuffer<TMarketData> : MarketDataBuffer<Security, TMarketData>
	{
	}
}