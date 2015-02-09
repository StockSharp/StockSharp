namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Базовый источник данных для <see cref="ICandleBuilder"/>, который получает данные из <see cref="IConnector"/>.
	/// </summary>
	/// <typeparam name="T">Тип исходных данных (например, <see cref="Trade"/>).</typeparam>
	public abstract class RealTimeCandleBuilderSource<T> : ConvertableCandleBuilderSource<T>
	{
		private readonly SynchronizedDictionary<Security, CachedSynchronizedList<CandleSeries>> _registeredSeries = new SynchronizedDictionary<Security, CachedSynchronizedList<CandleSeries>>();

		/// <summary>
		/// Создать <see cref="RealTimeCandleBuilderSource{T}"/>.
		/// </summary>
		/// <param name="connector">Подключение, через которое будут получаться новые данные.</param>
		protected RealTimeCandleBuilderSource(IConnector connector)
		{
			if (connector == null)
				throw new ArgumentNullException("connector");

			Connector = connector;
		}

		/// <summary>
		/// Приоритет источника по скорости (0 - самый оптимальный).
		/// </summary>
		public override int SpeedPriority
		{
			get { return 1; }
		}

		/// <summary>
		/// Подключение, через которое будут получаться новые данные.
		/// </summary>
		public IConnector Connector { get; private set; }

		/// <summary>
		/// Запросить получение данных.
		/// </summary>
		/// <param name="series">Серия свечек, для которой необходимо начать получать данные.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		public override void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			bool registerSecurity;

			series.IsNew = true;
			_registeredSeries.SafeAdd(series.Security, out registerSecurity).Add(series);

			if (registerSecurity)
				RegisterSecurity(series.Security);
		}

		/// <summary>
		/// Прекратить получение данных, запущенное через <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public override void Stop(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var registeredSeries = _registeredSeries.TryGetValue(series.Security);

			if (registeredSeries == null)
				return;

			registeredSeries.Remove(series);

			if (registeredSeries.Count == 0)
			{
				UnRegisterSecurity(series.Security);
				_registeredSeries.Remove(series.Security);
			}

			RaiseStopped(series);
		}

		/// <summary>
		/// Зарегистрировать получение данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		protected abstract void RegisterSecurity(Security security);

		/// <summary>
		/// Остановить получение данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		protected abstract void UnRegisterSecurity(Security security);

		/// <summary>
		/// Получить ранее накопленные значения.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Накопленные значения.</returns>
		protected abstract IEnumerable<T> GetSecurityValues(Security security);

		/// <summary>
		/// Добавить синхронно новые данные, полученные от <see cref="Connector"/>.
		/// </summary>
		/// <param name="values">Новые данные.</param>
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

				var securityValues = group.OrderBy(v => v.Id).ToArray();

				foreach (var series in seriesCache)
				{
					if (series.IsNew)
					{
						RaiseProcessing(series, Convert(GetSecurityValues(security)).OrderBy(v => v.Id));
						series.IsNew = false;
					}
					else
					{
						RaiseProcessing(series, securityValues);
					}
				}
			}
		}
	}

	/// <summary>
	/// Источник данных для <see cref="CandleBuilder{TCandle}"/>, который создает <see cref="ICandleBuilderSourceValue"/> из тиковых сделок <see cref="Trade"/>.
	/// </summary>
	public class TradeCandleBuilderSource : RealTimeCandleBuilderSource<Trade>
	{
		/// <summary>
		/// Создать <see cref="TradeCandleBuilderSource"/>.
		/// </summary>
		/// <param name="connector">Подключение, через которое будут получаться новые сделки, используя событие <see cref="IConnector.NewTrades"/>.</param>
		public TradeCandleBuilderSource(IConnector connector)
			: base(connector)
		{
			Connector.NewTrades += AddNewValues;
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var trades = GetSecurityValues(series.Security);

			yield return new Range<DateTimeOffset>(trades.IsEmpty() ? Connector.CurrentTime : trades.Min(v => v.Time).LocalDateTime, DateTimeOffset.MaxValue);
		}

		/// <summary>
		/// Зарегистрировать получение данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		protected override void RegisterSecurity(Security security)
		{
			Connector.RegisterTrades(security);
		}

		/// <summary>
		/// Остановить получение данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		protected override void UnRegisterSecurity(Security security)
		{
			Connector.UnRegisterTrades(security);
		}

		/// <summary>
		/// Получить ранее накопленные значения.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Накопленные значения.</returns>
		protected override IEnumerable<Trade> GetSecurityValues(Security security)
		{
			return Connector.Trades.Filter(security);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			Connector.NewTrades -= AddNewValues;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// Источник данных для <see cref="CandleBuilder{TCandle}"/>, который создает <see cref="ICandleBuilderSourceValue"/> из стакана <see cref="MarketDepth"/>.
	/// </summary>
	public class MarketDepthCandleBuilderSource : RealTimeCandleBuilderSource<MarketDepth>
	{
		/// <summary>
		/// Создать <see cref="MarketDepthCandleBuilderSource"/>.
		/// </summary>
		/// <param name="connector">Подключение, через которое будут получаться измененные стаканы, используя событие <see cref="IConnector.MarketDepthsChanged"/>.</param>
		public MarketDepthCandleBuilderSource(IConnector connector)
			: base(connector)
		{
			Connector.MarketDepthsChanged += OnMarketDepthsChanged;
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			yield return new Range<DateTimeOffset>(Connector.CurrentTime, DateTimeOffset.MaxValue);
		}

		/// <summary>
		/// Зарегистрировать получение данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		protected override void RegisterSecurity(Security security)
		{
			Connector.RegisterMarketDepth(security);
		}

		/// <summary>
		/// Остановить получение данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		protected override void UnRegisterSecurity(Security security)
		{
			Connector.UnRegisterMarketDepth(security);
		}

		/// <summary>
		/// Получить ранее накопленные значения.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Накопленные значения.</returns>
		protected override IEnumerable<MarketDepth> GetSecurityValues(Security security)
		{
			return Enumerable.Empty<MarketDepth>();
		}

		private void OnMarketDepthsChanged(IEnumerable<MarketDepth> depths)
		{
			AddNewValues(depths.Select(d => d.Clone()));
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			Connector.MarketDepthsChanged -= OnMarketDepthsChanged;
			base.DisposeManaged();
		}
	}
}