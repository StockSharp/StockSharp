namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// Базовый источник данных для <see cref="ICandleBuilder"/>, который переводит данные из типа <typeparamref name="TSourceValue"/> в <see cref="ICandleBuilderSourceValue"/>.
	/// </summary>
	/// <typeparam name="TSourceValue">Тип исходных данных (например, <see cref="Trade"/>).</typeparam>
	public abstract class ConvertableCandleBuilderSource<TSourceValue> : BaseCandleBuilderSource
	{
		static ConvertableCandleBuilderSource()
		{
			if (typeof(TSourceValue) == typeof(Trade))
			{
				DefaultConverter = ((Func<Trade, ICandleBuilderSourceValue>)(t => new TradeCandleBuilderSourceValue(t))).To<Func<TSourceValue, ICandleBuilderSourceValue>>();
				DefaultFilter = ((Func<Trade, bool>)(t => t.IsSystem)).To<Func<TSourceValue, bool>>();
			}
			else if (typeof(TSourceValue) == typeof(MarketDepth))
			{
				DefaultConverter = ((Func<MarketDepth, ICandleBuilderSourceValue>)(d => new DepthCandleBuilderSourceValue(d))).To<Func<TSourceValue, ICandleBuilderSourceValue>>();
				DefaultFilter = v => true;
			}
			else
				throw new InvalidOperationException(LocalizedStrings.Str653Params.Put(typeof(TSourceValue)));
		}

		/// <summary>
		/// Инициализировать <see cref="ConvertableCandleBuilderSource{TSourceValue}"/>.
		/// </summary>
		protected ConvertableCandleBuilderSource()
		{
		}

		/// <summary>
		/// Функция по-умолчанию для перевода данных из типа <typeparamref name="TSourceValue"/> в <see cref="ICandleBuilderSourceValue"/>.
		/// </summary>
		public static Func<TSourceValue, ICandleBuilderSourceValue> DefaultConverter { get; private set; }

		private Func<TSourceValue, ICandleBuilderSourceValue> _converter = DefaultConverter;

		/// <summary>
		/// Функция для перевода данных из типа <typeparamref name="TSourceValue"/> в <see cref="ICandleBuilderSourceValue"/>.
		/// </summary>
		public Func<TSourceValue, ICandleBuilderSourceValue> Converter
		{
			get { return _converter; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_converter = value;
			}
		}

		/// <summary>
		/// Функция по-умолчанию для фильтрации данных <typeparamref name="TSourceValue"/>.
		/// </summary>
		public static Func<TSourceValue, bool> DefaultFilter { get; private set; }

		private Func<TSourceValue, bool> _filter = DefaultFilter;

		/// <summary>
		/// Функция для фильтрации данных <typeparamref name="TSourceValue"/>.
		/// </summary>
		public Func<TSourceValue, bool> Filter
		{
			get { return _filter; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_filter = value;
			}
		}

		/// <summary>
		/// Сконвертировать новые данные с помощью <see cref="Converter"/>.
		/// </summary>
		/// <param name="values">Новые исходные данные.</param>
		/// <returns>Данные, в формате <see cref="ICandleBuilder"/>.</returns>
		protected IEnumerable<ICandleBuilderSourceValue> Convert(IEnumerable<TSourceValue> values)
		{
			return values.Where(Filter).Select(Converter);
		}

		/// <summary>
		/// Сконвертировать и передать новые данные в метод <see cref="BaseCandleBuilderSource.RaiseProcessing"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="values">Новые исходные данные.</param>
		protected virtual void NewSourceValues(CandleSeries series, IEnumerable<TSourceValue> values)
		{
			RaiseProcessing(series, Convert(values));
		}
	}

	/// <summary>
	/// Источник данных, работающий непосредственно с готовой коллекцией данных.
	/// </summary>
	/// <typeparam name="TSourceValue">Тип исходных данных (например, <see cref="Trade"/>).</typeparam>
	public class RawConvertableCandleBuilderSource<TSourceValue> : ConvertableCandleBuilderSource<TSourceValue>
	{
		private readonly Security _security;
		private readonly DateTime _from;
		private readonly DateTime _to;

		/// <summary>
		/// Создать <see cref="RawConvertableCandleBuilderSource{TSourceValue}"/>.
		/// </summary>
		/// <param name="security">Инструмент, данные которого передаются в источник.</param>
		/// <param name="from">Время первого значения.</param>
		/// <param name="to">Время последнего значения.</param>
		/// <param name="values">Готовая коллеция данные.</param>
		public RawConvertableCandleBuilderSource(Security security, DateTime from, DateTime to, IEnumerable<TSourceValue> values)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (values == null)
				throw new ArgumentNullException("values");

			_security = security;
			_from = from;
			_to = to;

			Values = values;
		}

		/// <summary>
		/// Приоритет источника по скорости (0 - самый оптимальный).
		/// </summary>
		public override int SpeedPriority
		{
			get { return 0; }
		}

		/// <summary>
		/// Готовая коллеция данные.
		/// </summary>
		public IEnumerable<TSourceValue> Values { get; private set; }

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (series.Security != _security)
				yield break;

			yield return new Range<DateTimeOffset>(_from, _to);
		}

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

			if (series.Security != _security)
				return;

			NewSourceValues(series, Values);

			RaiseStopped(series);
		}

		/// <summary>
		/// Прекратить получение данных, запущенное через <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public override void Stop(CandleSeries series)
		{
			RaiseStopped(series);
		}
	}
}