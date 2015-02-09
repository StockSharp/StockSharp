namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Common;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Входное значение индикатора, на основе которого он обновит свое значение, а так значение, хранящее результат вычисления индикатора.
	/// </summary>
	public interface IIndicatorValue : IComparable<IIndicatorValue>, IComparable
	{
		/// <summary>
		/// Индикатор.
		/// </summary>
		IIndicator Indicator { get; }

		/// <summary>
		/// Значение индикатора отсутствует.
		/// </summary>
		bool IsEmpty { get; }

		/// <summary>
		/// Является ли значение окончательным (индикатор окончательно формирует свое значение и более не будет изменяться в данной точке времени).
		/// </summary>
		bool IsFinal { get; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		bool IsFormed { get; }

		/// <summary>
		/// Входное значение.
		/// </summary>
		IIndicatorValue InputValue { get; set; }

		/// <summary>
		/// Поддерживает ли значение необходимый для индикатора тип данных.
		/// </summary>
		/// <param name="valueType">Тип данных, которым оперирует индикатор.</param>
		/// <returns><see langword="true"/>, если тип данных поддерживается, иначе, <see langword="false"/>.</returns>
		bool IsSupport(Type valueType);

		/// <summary>
		/// Получить значение по типу данных.
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <returns>Значение.</returns>
		T GetValue<T>();

		/// <summary>
		/// Изменить входное значение индикатора новым значением (например, оно получено от другого индикатора).
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		/// <returns>Новый объект, содержащий входное значение.</returns>
		IIndicatorValue SetValue<T>(IIndicator indicator, T value);
	}

	/// <summary>
	/// Базовое значение индикатора, работающее с один типом данных.
	/// </summary>
	/// <typeparam name="TValue">Тип значения.</typeparam>
	public class SingleIndicatorValue<TValue> : IIndicatorValue
	{
		/// <summary>
		/// Создать <see cref="SingleIndicatorValue{TValue}"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		public SingleIndicatorValue(IIndicator indicator, TValue value)
			: this(indicator)
		{
			Value = value;
			IsEmpty = value.IsNull();
			IsFormed = indicator.IsFormed;
		}

		/// <summary>
		/// Создать <see cref="SingleIndicatorValue{TValue}"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		public SingleIndicatorValue(IIndicator indicator)
		{
			if (indicator == null)
				throw new ArgumentNullException("indicator");

			Indicator = indicator;
			IsEmpty = true;
			IsFormed = indicator.IsFormed;
		}

		/// <summary>
		/// Значение.
		/// </summary>
		public TValue Value { get; private set; }

		/// <summary>
		/// Индикатор.
		/// </summary>
		public IIndicator Indicator { get; private set; }

		/// <summary>
		/// Значение индикатора отсутствует.
		/// </summary>
		public bool IsEmpty { get; private set; }

		/// <summary>
		/// Является ли значение окончательным (индикатор окончательно формирует свое значение и более не будет изменяться в данной точке времени).
		/// </summary>
		public bool IsFinal { get; set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public bool IsFormed { get; private set; }

		/// <summary>
		/// Входное значение.
		/// </summary>
		public IIndicatorValue InputValue { get; set; }

		/// <summary>
		/// Поддерживает ли значение необходимый для индикатора тип данных.
		/// </summary>
		/// <param name="valueType">Тип данных, которым оперирует индикатор.</param>
		/// <returns><see langword="true"/>, если тип данных поддерживается, иначе, <see langword="false"/>.</returns>
		public virtual bool IsSupport(Type valueType)
		{
			return valueType == typeof(TValue);
		}

		/// <summary>
		/// Получить значение по типу данных.
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <returns>Значение.</returns>
		public virtual T GetValue<T>()
		{
			ThrowIfEmpty();
			return Value.To<T>();
		}

		/// <summary>
		/// Изменить входное значение индикатора новым значением (например, оно получено от другого индикатора).
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		/// <returns>Новый объект, содержащий входное значение.</returns>
		public virtual IIndicatorValue SetValue<T>(IIndicator indicator, T value)
		{
			return new SingleIndicatorValue<T>(indicator, value) { IsFinal = IsFinal, InputValue = this };
		}

		private void ThrowIfEmpty()
		{
			if (IsEmpty)
				throw new InvalidOperationException(LocalizedStrings.Str910);
		}

		/// <summary>
		/// Сравнить с другим значением индикатора.
		/// </summary>
		/// <param name="other">Другое значение, с которым необходимо сравнивать.</param>
		/// <returns>Код сравнения.</returns>
		public virtual int CompareTo(IIndicatorValue other)
		{
			return Value.Compare(other.GetValue<TValue>());
		}

		/// <summary>
		/// Сравнить с другим значением индикатора.
		/// </summary>
		/// <param name="other">Другое значение, с которым необходимо сравнивать.</param>
		/// <returns>Код сравнения.</returns>
		public int CompareTo(object other)
		{
			var value = other as IIndicatorValue;

			if(other == null)
				throw new ArgumentException(LocalizedStrings.Str911, "other");

			return CompareTo(value);
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return IsEmpty ? "Empty" : Value.ToString();
		}
	}

	/// <summary>
	/// Значение индикатора, работающее с типом данных <see cref="decimal"/>.
	/// </summary>
	public class DecimalIndicatorValue : SingleIndicatorValue<decimal>
	{
		/// <summary>
		/// Создать <see cref="DecimalIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		public DecimalIndicatorValue(IIndicator indicator, decimal value)
			: base(indicator, value)
		{
		}

		/// <summary>
		/// Создать <see cref="DecimalIndicatorValue"/>. 
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		public DecimalIndicatorValue(IIndicator indicator)
			: base(indicator)
		{
		}

		///// <summary>
		///// Привести <see cref="decimal"/> значение к объекту <see cref="DecimalIndicatorValue"/>.
		///// </summary>
		///// <param name="value"><see cref="decimal"/> значение.</param>
		///// <returns>Объект <see cref="DecimalIndicatorValue"/>.</returns>
		//public static implicit operator DecimalIndicatorValue(decimal value)
		//{
		//	return new DecimalIndicatorValue(value);
		//}

		/// <summary>
		/// Изменить входное значение индикатора новым значением (например, оно получено от другого индикатора).
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		/// <returns>Новый объект, содержащий входное значение.</returns>
		public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
		{
			return typeof(T) == typeof(decimal)
				? new DecimalIndicatorValue(indicator, value.To<decimal>()) { IsFinal = IsFinal, InputValue = this }
				: base.SetValue(indicator, value);
		}
	}

	/// <summary>
	/// Значение индикатора, работающее с типом данных <see cref="Candle"/>.
	/// </summary>
	public class CandleIndicatorValue : SingleIndicatorValue<Candle>
	{
		private readonly Func<Candle, decimal> _getPart;

		/// <summary>
		/// Создать <see cref="CandleIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		public CandleIndicatorValue(IIndicator indicator, Candle value)
			: this(indicator, value, ByClose)
		{
		}

		/// <summary>
		/// Создать <see cref="CandleIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		/// <param name="getPart">Конвертер свечи, через который можно получить ее параметр. По-умолчанию используется <see cref="ByClose"/>.</param>
		public CandleIndicatorValue(IIndicator indicator, Candle value, Func<Candle, decimal> getPart)
			: base(indicator, value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			if (getPart == null)
				throw new ArgumentNullException("getPart");

			_getPart = getPart;

			IsFinal = value.State == CandleStates.Finished;
		}

		/// <summary>
		/// Создать <see cref="CandleIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		private CandleIndicatorValue(IIndicator indicator)
			: base(indicator)
		{
		}

		/// <summary>
		/// Конвертер, который берет из свечи цену закрытия <see cref="Candle.ClosePrice"/>.
		/// </summary>
		public static readonly Func<Candle, decimal> ByClose = c => c.ClosePrice;

		/// <summary>
		/// Конвертер, который берет из свечи цену открытия <see cref="Candle.OpenPrice"/>.
		/// </summary>
		public static readonly Func<Candle, decimal> ByOpen = c => c.OpenPrice;

		/// <summary>
		/// Конвертер, который берет из свечи середину тела (<see cref="Candle.OpenPrice"/> + <see cref="Candle.ClosePrice"/>) / 2.
		/// </summary>
		public static readonly Func<Candle, decimal> ByMiddle = c => (c.ClosePrice + c.OpenPrice) / 2;

		///// <summary>
		///// Привести <see cref="Candle"/> значение к объекту <see cref="CandleIndicatorValue"/>.
		///// </summary>
		///// <param name="value"><see cref="Candle"/> значение.</param>
		///// <returns>Объект <see cref="CandleIndicatorValue"/>.</returns>
		//public static implicit operator CandleIndicatorValue(Candle value)
		//{
		//	return new CandleIndicatorValue(value);
		//}

		/// <summary>
		/// Поддерживает ли значение необходимый для индикатора тип данных.
		/// </summary>
		/// <param name="valueType">Тип данных, которым оперирует индикатор.</param>
		/// <returns><see langword="true"/>, если тип данных поддерживается, иначе, <see langword="false"/>.</returns>
		public override bool IsSupport(Type valueType)
		{
			return valueType == typeof(decimal) || base.IsSupport(valueType);
		}

		/// <summary>
		/// Получить значение по типу данных.
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <returns>Значение.</returns>
		public override T GetValue<T>()
		{
			var candle = base.GetValue<Candle>();
			return typeof(T) == typeof(decimal) ? _getPart(candle).To<T>() : candle.To<T>();
		}

		/// <summary>
		/// Изменить входное значение индикатора новым значением (например, оно получено от другого индикатора).
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		/// <returns>Новый объект, содержащий входное значение.</returns>
		public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
		{
			var candle = value as Candle;

			return candle != null
					? new CandleIndicatorValue(indicator, candle) { InputValue = this }
					: value.IsNull() ? new CandleIndicatorValue(indicator) : base.SetValue(indicator, value);
		}
	}

	/// <summary>
	/// Значение индикатора, работающее с типом данных <see cref="MarketDepth"/>.
	/// </summary>
	public class MarketDepthIndicatorValue : SingleIndicatorValue<MarketDepth>
	{
		private readonly Func<MarketDepth, decimal?> _getPart;

		/// <summary>
		/// Создать <see cref="MarketDepthIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="depth">Стакан.</param>
		public MarketDepthIndicatorValue(IIndicator indicator, MarketDepth depth)
			: this(indicator, depth, ByMiddle)
		{
		}

		/// <summary>
		/// Создать <see cref="MarketDepthIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="depth">Стакан.</param>
		/// <param name="getPart">Конвертер стакана, через который можно получить его параметр. По-умолчанию используется <see cref="ByMiddle"/>.</param>
		public MarketDepthIndicatorValue(IIndicator indicator, MarketDepth depth, Func<MarketDepth, decimal?> getPart)
			: base(indicator, depth)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			if (getPart == null)
				throw new ArgumentNullException("getPart");

			_getPart = getPart;
		}

		/// <summary>
		/// Конвертер, который берет из стакана цену лучшего бида <see cref="MarketDepth.BestBid"/>.
		/// </summary>
		public static readonly Func<MarketDepth, decimal?> ByBestBid = d => d.BestBid != null ? d.BestBid.Price : (decimal?)null;

		/// <summary>
		/// Конвертер, который берет из стакана цену лучшего оффера <see cref="MarketDepth.BestAsk"/>.
		/// </summary>
		public static readonly Func<MarketDepth, decimal?> ByBestAsk = d => d.BestAsk != null ? d.BestAsk.Price : (decimal?)null;

		/// <summary>
		/// Конвертер, который берет из стакана середину спреда <see cref="MarketDepthPair.MiddlePrice"/>.
		/// </summary>
		public static readonly Func<MarketDepth, decimal?> ByMiddle = d => d.BestPair == null ? (decimal?)null : d.BestPair.MiddlePrice;

		/// <summary>
		/// Поддерживает ли значение необходимый для индикатора тип данных.
		/// </summary>
		/// <param name="valueType">Тип данных, которым оперирует индикатор.</param>
		/// <returns><see langword="true"/>, если тип данных поддерживается, иначе, <see langword="false"/>.</returns>
		public override bool IsSupport(Type valueType)
		{
			return valueType == typeof(decimal) || base.IsSupport(valueType);
		}

		/// <summary>
		/// Получить значение по типу данных.
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <returns>Значение.</returns>
		public override T GetValue<T>()
		{
			var depth = base.GetValue<MarketDepth>();
			return typeof(T) == typeof(decimal) ? (_getPart(depth) ?? 0).To<T>() : depth.To<T>();
		}

		/// <summary>
		/// Изменить входное значение индикатора новым значением (например, оно получено от другого индикатора).
		/// </summary>
		/// <typeparam name="T">Тип данных, которым оперирует индикатор.</typeparam>
		/// <param name="indicator">Индикатор.</param>
		/// <param name="value">Значение.</param>
		/// <returns>Новый объект, содержащий входное значение.</returns>
		public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
		{
			return new MarketDepthIndicatorValue(indicator, base.GetValue<MarketDepth>(), _getPart)
			{
				IsFinal = IsFinal,
				InputValue = this
			};
		}
	}
}