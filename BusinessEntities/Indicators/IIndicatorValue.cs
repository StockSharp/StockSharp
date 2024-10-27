namespace StockSharp.Algo.Indicators;

/// <summary>
/// The indicator value, based on which it will renew its value, as well as value, containing result of indicator calculation.
/// </summary>
public interface IIndicatorValue : IComparable<IIndicatorValue>, IComparable
{
	/// <summary>
	/// Indicator.
	/// </summary>
	IIndicator Indicator { get; }

	/// <summary>
	/// No indicator value.
	/// </summary>
	bool IsEmpty { get; }

	/// <summary>
	/// Is the value final (indicator finalizes its value and will not be changed anymore in the given point of time).
	/// </summary>
	bool IsFinal { get; set; }

	/// <summary>
	/// Whether the indicator is set.
	/// </summary>
	bool IsFormed { get; set; }

	/// <summary>
	/// Value time.
	/// </summary>
	DateTimeOffset Time { get; }

	/// <summary>
	/// Does value support data type, required for the indicator.
	/// </summary>
	/// <param name="valueType">The data type, operated by indicator.</param>
	/// <returns><see langword="true" />, if data type is supported, otherwise, <see langword="false" />.</returns>
	bool IsSupport(Type valueType);

	/// <summary>
	/// To get the value by the data type.
	/// </summary>
	/// <typeparam name="T">The data type, operated by indicator.</typeparam>
	/// <param name="field">Field specified value source.</param>
	/// <returns>Value.</returns>
	T GetValue<T>(Level1Fields? field = default);

	/// <summary>
	/// To replace the indicator input value by new one (for example it is received from another indicator).
	/// </summary>
	/// <typeparam name="T">The data type, operated by indicator.</typeparam>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">Value.</param>
	/// <returns>New object, containing input value.</returns>
	IIndicatorValue SetValue<T>(IIndicator indicator, T value);

	/// <summary>
	/// Convert to primitive values.
	/// </summary>
	/// <returns>Primitive values.</returns>
	IEnumerable<object> ToValues();

	/// <summary>
	/// Convert to indicator value.
	/// </summary>
	/// <param name="values"><see cref="ToValues"/></param>
	void FromValues(object[] values);
}

/// <summary>
/// The base class for the indicator value.
/// </summary>
public abstract class BaseIndicatorValue : IIndicatorValue
{
	/// <summary>
	/// Initialize <see cref="BaseIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	protected BaseIndicatorValue(IIndicator indicator, DateTimeOffset time)
	{
		Indicator = indicator ?? throw new ArgumentNullException(nameof(indicator));
		IsFormed = indicator.IsFormed;
		Time = time;
	}

	/// <inheritdoc />
	public IIndicator Indicator { get; }

	/// <inheritdoc />
	public abstract bool IsEmpty { get; set; }

	/// <inheritdoc />
	public abstract bool IsFinal { get; set; }

	/// <inheritdoc />
	public bool IsFormed { get; set; }

	/// <inheritdoc />
	public DateTimeOffset Time { get; }

	/// <inheritdoc />
	public abstract bool IsSupport(Type valueType);

	/// <inheritdoc />
	public abstract T GetValue<T>(Level1Fields? field);

	/// <inheritdoc />
	public abstract IIndicatorValue SetValue<T>(IIndicator indicator, T value);

	/// <inheritdoc />
	public abstract int CompareTo(IIndicatorValue other);

	/// <inheritdoc />
	int IComparable.CompareTo(object other)
	{
		var value = other as IIndicatorValue;

		if (other == null)
			throw new ArgumentOutOfRangeException(nameof(other), other, LocalizedStrings.InvalidValue);

		return CompareTo(value);
	}

	/// <inheritdoc />
	public abstract IEnumerable<object> ToValues();

	/// <inheritdoc />
	public abstract void FromValues(object[] values);
}

/// <summary>
/// The base value of the indicator, operating with one data type.
/// </summary>
/// <typeparam name="TValue">Value type.</typeparam>
public class SingleIndicatorValue<TValue> : BaseIndicatorValue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SingleIndicatorValue{T}"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">Value.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public SingleIndicatorValue(IIndicator indicator, TValue value, DateTimeOffset time)
		: base(indicator, time)
	{
		Value = value;
		IsEmpty = value.IsNull();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SingleIndicatorValue{T}"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public SingleIndicatorValue(IIndicator indicator, DateTimeOffset time)
		: base(indicator, time)
	{
		IsEmpty = true;
	}

	/// <summary>
	/// Value.
	/// </summary>
	public TValue Value { get; protected set; }

	/// <inheritdoc />
	public override bool IsEmpty { get; set; }

	/// <inheritdoc />
	public override bool IsFinal { get; set; }

	/// <inheritdoc />
	public override bool IsSupport(Type valueType) => valueType.IsAssignableFrom(typeof(TValue));

	/// <inheritdoc />
	public override T GetValue<T>(Level1Fields? field)
	{
		ThrowIfEmpty();
		return Value is T t ? t : throw new InvalidCastException($"Cannot convert {typeof(TValue).Name} to {typeof(T).Name}."); ;
	}

	/// <inheritdoc />
	public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
	{
		return new SingleIndicatorValue<T>(indicator, value, Time) { IsFinal = IsFinal };
	}

	private void ThrowIfEmpty()
	{
		if (IsEmpty)
			throw new InvalidOperationException(LocalizedStrings.NoData2);
	}

	/// <inheritdoc />
	public override int CompareTo(IIndicatorValue other) => Value.Compare(other.GetValue<TValue>());

	/// <inheritdoc />
	public override string ToString() => IsEmpty ? "Empty" : Value.ToString();

	/// <summary>
	/// Cast object from <see cref="SingleIndicatorValue{TValue}"/> to <typeparamref name="TValue"/>.
	/// </summary>
	/// <param name="value">Object <see cref="SingleIndicatorValue{TValue}"/>.</param>
	/// <returns><typeparamref name="TValue"/> value.</returns>
	public static explicit operator TValue(SingleIndicatorValue<TValue> value)
		=> value.Value;

	/// <inheritdoc />
	public override IEnumerable<object> ToValues()
	{
		if (!IsEmpty)
			yield return Value;
	}

	/// <inheritdoc />
	public override void FromValues(object[] values)
	{
		if (values.Length == 0)
		{
			IsEmpty = true;
			return;
		}

		IsEmpty = false;
		Value = values[0].To<TValue>();
	}
}

/// <summary>
/// The indicator value, operating with data type <see cref="decimal"/>.
/// </summary>
public class DecimalIndicatorValue : SingleIndicatorValue<decimal>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DecimalIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">Value.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public DecimalIndicatorValue(IIndicator indicator, decimal value, DateTimeOffset time)
		: base(indicator, value, time)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DecimalIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public DecimalIndicatorValue(IIndicator indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <inheritdoc />
	public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
	{
		return typeof(T) == typeof(decimal)
			? new DecimalIndicatorValue(indicator, value.To<decimal>(), Time) { IsFinal = IsFinal }
			: base.SetValue(indicator, value);
	}

	/// <summary>
	/// Cast object from <see cref="DecimalIndicatorValue"/> to <see cref="decimal"/>.
	/// </summary>
	/// <param name="value">Object <see cref="DecimalIndicatorValue"/>.</param>
	/// <returns><see cref="decimal"/> value.</returns>
	public static explicit operator decimal(DecimalIndicatorValue value)
		=> value.Value;
}

/// <summary>
/// The indicator value, operating with data type <see cref="ICandleMessage"/>.
/// </summary>
public class CandleIndicatorValue : SingleIndicatorValue<ICandleMessage>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CandleIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">Value.</param>
	public CandleIndicatorValue(IIndicator indicator, ICandleMessage value)
		: base(indicator, value, value.CheckOnNull(nameof(value)).ServerTime)
	{
		IsFinal = value.State == CandleStates.Finished;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CandleIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public CandleIndicatorValue(IIndicator indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <inheritdoc />
	public override bool IsSupport(Type valueType) => valueType == typeof(decimal) || valueType.Is<ICandleMessage>();

	/// <inheritdoc />
	public override T GetValue<T>(Level1Fields? field)
	{
		var candle = base.GetValue<ICandleMessage>(default);

		if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
		{
			var candlePart = field switch
			{
				null or Level1Fields.LastTradePrice or Level1Fields.ClosePrice => candle.ClosePrice,
				Level1Fields.OpenPrice => candle.OpenPrice,
				Level1Fields.HighPrice => candle.HighPrice,
				Level1Fields.LowPrice => candle.LowPrice,

				Level1Fields.Volume => candle.TotalVolume,
				Level1Fields.OpenInterest => candle.OpenInterest,

				_ => throw new ArgumentOutOfRangeException(nameof(field), field, LocalizedStrings.InvalidValue),
			};

			return candlePart is T t ? t : throw new InvalidCastException($"Cannot convert decimal to {typeof(T).Name}.");
		}
		else
			return candle is T t ? t : throw new InvalidCastException($"Cannot convert candle to {typeof(T).Name}.");
	}

	/// <inheritdoc />
	public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
	{
		return value is ICandleMessage candle
				? new CandleIndicatorValue(indicator, candle) { IsFinal = IsFinal }
				: value.IsNull() ? new CandleIndicatorValue(indicator, Time) { IsFinal = IsFinal } : base.SetValue(indicator, value);
	}
}

/// <summary>
/// The indicator value, operating with data type <see cref="IOrderBookMessage"/>.
/// </summary>
public class MarketDepthIndicatorValue : SingleIndicatorValue<IOrderBookMessage>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MarketDepthIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="depth">Market depth.</param>
	public MarketDepthIndicatorValue(IIndicator indicator, IOrderBookMessage depth)
		: base(indicator, depth, depth.CheckOnNull(nameof(depth)).ServerTime)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketDepthIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public MarketDepthIndicatorValue(IIndicator indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <inheritdoc />
	public override bool IsSupport(Type valueType)
	{
		return valueType == typeof(decimal) || base.IsSupport(valueType);
	}

	/// <inheritdoc />
	public override T GetValue<T>(Level1Fields? field)
	{
		var depth = base.GetValue<IOrderBookMessage>(default);

		if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
		{
			var value = field switch
			{
				null or Level1Fields.SpreadMiddle => depth.GetSpreadMiddle(null),
				Level1Fields.BestBidPrice => depth.GetBestBid()?.Price,
				Level1Fields.BestAskPrice => depth.GetBestAsk()?.Price,
				Level1Fields.BestBidVolume => depth.GetBestBid()?.Volume,
				Level1Fields.BestAskVolume => depth.GetBestAsk()?.Volume,
				_ => throw new ArgumentOutOfRangeException(nameof(field), field, LocalizedStrings.InvalidValue),
			};

			if (value is null && typeof(T) == typeof(decimal))
				return default;

			return value.To<T>();
		}
		else
			return depth.To<T>();
	}

	/// <inheritdoc />
	public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
	{
		return new MarketDepthIndicatorValue(indicator, base.GetValue<IOrderBookMessage>(default))
		{
			IsFinal = IsFinal
		};
	}
}

/// <summary>
/// The indicator value, operating with data type <see cref="Level1ChangeMessage"/>.
/// </summary>
public class Level1IndicatorValue : SingleIndicatorValue<Level1ChangeMessage>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Level1IndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="l1Msg"><see cref="Level1ChangeMessage"/></param>
	public Level1IndicatorValue(IIndicator indicator, Level1ChangeMessage l1Msg)
		: base(indicator, l1Msg, l1Msg.CheckOnNull(nameof(l1Msg)).ServerTime)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Level1IndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public Level1IndicatorValue(IIndicator indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <inheritdoc />
	public override bool IsSupport(Type valueType)
	{
		return valueType == typeof(decimal) || base.IsSupport(valueType);
	}

	/// <inheritdoc />
	public override T GetValue<T>(Level1Fields? field)
	{
		var l1Msg = base.GetValue<Level1ChangeMessage>(default);

		if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
		{
			var value = field switch
			{
				Level1Fields.SpreadMiddle => l1Msg.GetSpreadMiddle(null),
				_ => l1Msg.TryGet(field ?? Level1Fields.LastTradePrice),
			};

			if (value is null && typeof(T) == typeof(decimal))
				return default;

			return value.To<T>();
		}
		else
			return l1Msg.To<T>();
	}

	/// <inheritdoc />
	public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
	{
		return new Level1IndicatorValue(indicator, base.GetValue<Level1ChangeMessage>(default))
		{
			IsFinal = IsFinal
		};
	}
}

/// <summary>
/// The indicator value, operating with data type <see cref="ITickTradeMessage"/>.
/// </summary>
public class TickIndicatorValue : SingleIndicatorValue<ITickTradeMessage>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TickIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="tick"><see cref="ITickTradeMessage"/></param>
	public TickIndicatorValue(IIndicator indicator, ITickTradeMessage tick)
		: base(indicator, tick, tick.CheckOnNull(nameof(tick)).ServerTime)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TickIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public TickIndicatorValue(IIndicator indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <inheritdoc />
	public override bool IsSupport(Type valueType)
	{
		return valueType == typeof(decimal) || base.IsSupport(valueType);
	}

	/// <inheritdoc />
	public override T GetValue<T>(Level1Fields? field)
	{
		var tick = base.GetValue<ITickTradeMessage>(default);

		if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
		{
			var value = field switch
			{
				Level1Fields.LastTradePrice or null => tick.Price,
				Level1Fields.LastTradeVolume => tick.Volume,
				_ => (decimal?)null,
			};

			if (value is null && typeof(T) == typeof(decimal))
				return default;

			return value.To<T>();
		}
		else
			return tick.To<T>();
	}

	/// <inheritdoc />
	public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
	{
		return new TickIndicatorValue(indicator, base.GetValue<ITickTradeMessage>(default))
		{
			IsFinal = IsFinal
		};
	}
}

/// <summary>
/// The value of the indicator, operating with pair <see ref="Tuple{TValue, TValue}" />.
/// </summary>
/// <typeparam name="TValue">Value type.</typeparam>
public class PairIndicatorValue<TValue> : SingleIndicatorValue<Tuple<TValue, TValue>>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PairIndicatorValue{T}"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="value">Value.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public PairIndicatorValue(IIndicator indicator, Tuple<TValue, TValue> value, DateTimeOffset time)
		: base(indicator, value, time)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PairIndicatorValue{T}"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public PairIndicatorValue(IIndicator indicator, DateTimeOffset time)
		: base(indicator, time)
	{
	}

	/// <inheritdoc />
	public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
	{
		return new PairIndicatorValue<TValue>(indicator, GetValue<Tuple<TValue, TValue>>(default), Time)
		{
			IsFinal = IsFinal
		};
	}
}

/// <summary>
/// The complex value of the indicator <see cref="IComplexIndicator"/>, derived as result of calculation.
/// </summary>
public class ComplexIndicatorValue : BaseIndicatorValue
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ComplexIndicatorValue"/>.
	/// </summary>
	/// <param name="indicator">Indicator.</param>
	/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
	public ComplexIndicatorValue(IComplexIndicator indicator, DateTimeOffset time)
		: base(indicator, time)
	{
		InnerValues = new Dictionary<IIndicator, IIndicatorValue>();
	}

	/// <inheritdoc />
	public override bool IsEmpty { get; set; }

	/// <inheritdoc />
	public override bool IsFinal { get; set; }

	/// <summary>
	/// Embedded values.
	/// </summary>
	public IDictionary<IIndicator, IIndicatorValue> InnerValues { get; }

	/// <summary>
	/// Gets or sets a value of inner indicator.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <returns><see cref="IIndicatorValue"/></returns>
	public IIndicatorValue this[IIndicator indicator]
	{
		get => InnerValues[indicator];
		set => InnerValues[indicator] = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Add a value of inner indicator.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <param name="value"><see cref="IIndicatorValue"/></param>
	public void Add(IIndicator indicator, IIndicatorValue value)
		=> InnerValues.Add(indicator, value);

	/// <summary>
	/// Try get a value of inner indicator.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <param name="value"><see cref="IIndicatorValue"/></param>
	/// <returns>Operation result.</returns>
	public bool TryGet(IIndicator indicator, out IIndicatorValue value)
		=> InnerValues.TryGetValue(indicator, out value);

	/// <inheritdoc />
	public override bool IsSupport(Type valueType) => InnerValues.Any(v => v.Value.IsSupport(valueType));

	/// <inheritdoc />
	public override T GetValue<T>(Level1Fields? field) => throw new NotSupportedException();

	/// <inheritdoc />
	public override IIndicatorValue SetValue<T>(IIndicator indicator, T value) => throw new NotSupportedException();

	/// <inheritdoc />
	public override int CompareTo(IIndicatorValue other) => throw new NotSupportedException();

	/// <inheritdoc />
	public override IEnumerable<object> ToValues()
	{
		if (IsEmpty)
			yield break;

		foreach (var inner in ((IComplexIndicator)Indicator).InnerIndicators)
			yield return InnerValues[inner].ToValues();
	}

	/// <inheritdoc />
	public override void FromValues(object[] values)
	{
		if (values.Length == 0)
		{
			IsEmpty = true;
			return;
		}

		IsEmpty = false;

		InnerValues.Clear();

		var idx = 0;

		foreach (var inner in ((IComplexIndicator)Indicator).InnerIndicators)
			InnerValues.Add(inner, inner.CreateValue(Time, values[idx++].To<object[]>()));
	}
}
