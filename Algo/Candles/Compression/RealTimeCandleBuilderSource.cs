#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Compression.Algo
File: RealTimeCandleBuilderSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The base data source for <see cref="ICandleBuilder"/> which receives data from <see cref="IConnector"/>.
	/// </summary>
	/// <typeparam name="T">The source data type (for example, <see cref="Trade"/>).</typeparam>
	public abstract class RealTimeCandleBuilderSource<T> : ConvertableCandleBuilderSource<T>
	{
		private readonly SynchronizedDictionary<Security, CachedSynchronizedList<CandleSeries>> _registeredSeries = new SynchronizedDictionary<Security, CachedSynchronizedList<CandleSeries>>();
		private readonly OrderedPriorityQueue<DateTimeOffset, CandleSeries> _seriesByDates = new OrderedPriorityQueue<DateTimeOffset, CandleSeries>();

		/// <summary>
		/// Initializes a new instance of the <see cref="RealTimeCandleBuilderSource{T}"/>.
		/// </summary>
		/// <param name="connector">The connection through which new data will be received.</param>
		protected RealTimeCandleBuilderSource(IConnector connector)
		{
			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			Connector = connector;
			Connector.MarketTimeChanged += OnConnectorMarketTimeChanged;
		}

		/// <summary>
		/// The source priority by speed (0 - the best).
		/// </summary>
		public override int SpeedPriority => 1;

		/// <summary>
		/// The connection through which new data will be received.
		/// </summary>
		public IConnector Connector { get; }

		/// <summary>
		/// To send data request.
		/// </summary>
		/// <param name="series">The candles series for which data receiving should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		public override void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			bool registerSecurity;

			series.IsNew = true;
			_registeredSeries.SafeAdd(series.Security, out registerSecurity).Add(series);
			
			if (registerSecurity)
				RegisterSecurity(series.Security);

			_seriesByDates.Add(new KeyValuePair<DateTimeOffset, CandleSeries>(to, series));
		}

		/// <summary>
		/// To stop data receiving starting through <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public override void Stop(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var registeredSeries = _registeredSeries.TryGetValue(series.Security);

			if (registeredSeries == null)
				return;

			registeredSeries.Remove(series);

			if (registeredSeries.Count == 0)
			{
				UnRegisterSecurity(series.Security);
				_registeredSeries.Remove(series.Security);
			}

			_seriesByDates.RemoveWhere(i => i.Value == series);

			RaiseStopped(series);
		}

		/// <summary>
		/// To register the getting data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		protected abstract void RegisterSecurity(Security security);

		/// <summary>
		/// To stop the getting data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		protected abstract void UnRegisterSecurity(Security security);

		/// <summary>
		/// To get previously accumulated values.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Accumulated values.</returns>
		protected abstract IEnumerable<T> GetSecurityValues(Security security);

		/// <summary>
		/// Synchronously to add new data received from <see cref="Connector"/>.
		/// </summary>
		/// <param name="values">New data.</param>
		protected void AddNewValues(IEnumerable<T> values)
		{
			if (_registeredSeries.Count == 0)
				return;

			foreach (var group in Convert(values).GroupBy(v => v.Security))
			{
				var security = group.Key;

				var registeredSeries = _registeredSeries.TryGetValue(security);

				if (registeredSeries == null)
					continue;

				var seriesCache = registeredSeries.Cache;

				var securityValues = group.OrderBy(v => v.Time).ToArray();

				foreach (var series in seriesCache)
				{
					if (series.IsNew)
					{
						RaiseProcessing(series, Convert(GetSecurityValues(security)).OrderBy(v => v.Time));
						series.IsNew = false;
					}
					else
					{
						RaiseProcessing(series, securityValues);
					}
				}
			}
		}

		private void OnConnectorMarketTimeChanged(TimeSpan value)
		{
			if (_seriesByDates.Count == 0)
				return;

			var pair = _seriesByDates.Peek();

			while (pair.Key <= Connector.CurrentTime)
			{
				RaiseStopped(pair.Value);
				_seriesByDates.Dequeue();

				if (_seriesByDates.Count == 0)
					break;

				pair = _seriesByDates.Peek();
			}
		}
	}

	/// <summary>
	/// The data source for <see cref="CandleBuilder{T}"/> which creates <see cref="ICandleBuilderSourceValue"/> from tick trades <see cref="Trade"/>.
	/// </summary>
	public class TradeCandleBuilderSource : RealTimeCandleBuilderSource<Trade>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TradeCandleBuilderSource"/>.
		/// </summary>
		/// <param name="connector">The connection through which new trades will be received using the event <see cref="IConnector.NewTrades"/>.</param>
		public TradeCandleBuilderSource(IConnector connector)
			: base(connector)
		{
			Connector.NewTrades += AddNewValues;
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var trades = GetSecurityValues(series.Security);

			yield return new Range<DateTimeOffset>(trades.IsEmpty() ? Connector.CurrentTime : trades.Min(v => v.Time), DateTimeOffset.MaxValue);
		}

		/// <summary>
		/// To register the getting data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		protected override void RegisterSecurity(Security security)
		{
			Connector.RegisterTrades(security);
		}

		/// <summary>
		/// To stop the getting data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		protected override void UnRegisterSecurity(Security security)
		{
			Connector.UnRegisterTrades(security);
		}

		/// <summary>
		/// To get previously accumulated values.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Accumulated values.</returns>
		protected override IEnumerable<Trade> GetSecurityValues(Security security)
		{
			return Connector.Trades.Filter(security);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			Connector.NewTrades -= AddNewValues;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// The data source for <see cref="CandleBuilder{T}"/> which creates <see cref="ICandleBuilderSourceValue"/> from the order book <see cref="MarketDepth"/>.
	/// </summary>
	public class MarketDepthCandleBuilderSource : RealTimeCandleBuilderSource<MarketDepth>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MarketDepthCandleBuilderSource"/>.
		/// </summary>
		/// <param name="connector">The connection through which changed order books will be received using the event <see cref="IConnector.MarketDepthsChanged"/>.</param>
		public MarketDepthCandleBuilderSource(IConnector connector)
			: base(connector)
		{
			Connector.MarketDepthsChanged += OnMarketDepthsChanged;
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			yield return new Range<DateTimeOffset>(Connector.CurrentTime, DateTimeOffset.MaxValue);
		}

		/// <summary>
		/// To register the getting data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		protected override void RegisterSecurity(Security security)
		{
			Connector.RegisterMarketDepth(security);
		}

		/// <summary>
		/// To stop the getting data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		protected override void UnRegisterSecurity(Security security)
		{
			Connector.UnRegisterMarketDepth(security);
		}

		/// <summary>
		/// To get previously accumulated values.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Accumulated values.</returns>
		protected override IEnumerable<MarketDepth> GetSecurityValues(Security security)
		{
			return Enumerable.Empty<MarketDepth>();
		}

		private void OnMarketDepthsChanged(IEnumerable<MarketDepth> depths)
		{
			AddNewValues(depths.Select(d => d.Clone()));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			Connector.MarketDepthsChanged -= OnMarketDepthsChanged;
			base.DisposeManaged();
		}
	}
}