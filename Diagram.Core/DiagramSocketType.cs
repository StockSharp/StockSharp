namespace StockSharp.Diagram;

using System.Drawing;

/// <summary>
/// Connection type.
/// </summary>
public class DiagramSocketType : Equatable<DiagramSocketType>, INotifyPropertyChanged, IPersistable
{
	private string _name = string.Empty;

	/// <summary>
	/// The name of the connection type.
	/// </summary>
	public string Name
	{
		get => _name;
		private set
		{
			_name = value ?? throw new ArgumentNullException(nameof(value));
			OnPropertyChanged(nameof(Name));
		}
	}

	private Type _type = typeof(object);

	/// <summary>
	/// Connection type.
	/// </summary>
	public Type Type
	{
		get => _type;
		private set
		{
			_type = value ?? throw new ArgumentNullException(nameof(value));
			OnPropertyChanged(nameof(Type));
		}
	}

	private Color _color = Color.Black;

	/// <summary>
	/// The connection color.
	/// </summary>
	public Color Color
	{
		get => _color;
		private set
		{
			_color = value;
			OnPropertyChanged(nameof(Color));
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DiagramSocketType"/>.
	/// </summary>
	/// <param name="type">Data type.</param>
	/// <param name="name">The name of the connection type.</param>
	/// <param name="color">The connection color.</param>
	private DiagramSocketType(Type type, string name, Color color)
	{
		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		Type = type ?? throw new ArgumentNullException(nameof(type));
		Name = name;
		Color = color;
	}

	private static readonly CachedSynchronizedDictionary<string, DiagramSocketType> _allTypes = [];

	/// <summary>
	/// All available connection types for elements.
	/// </summary>
	public static IEnumerable<DiagramSocketType> AllTypes => _allTypes.CachedValues;

	/// <summary>
	/// To register the connection type.
	/// </summary>
	/// <typeparam name="T">Data type.</typeparam>
	/// <param name="name">The name of the connection type.</param>
	/// <param name="color">The connection color.</param>
	/// <returns>Connection type.</returns>
	public static DiagramSocketType RegisterType<T>(string name, Color color)
	{
		if (name == null)
			throw new ArgumentNullException(nameof(name));

		return _allTypes.SafeAdd(name, s => new DiagramSocketType(typeof(T), name, color));
	}

	#region INotifyPropertyChanged

	/// <summary>
	/// The connection properties value change event.
	/// </summary>
	public event PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	/// To call the connection property value change event.
	/// </summary>
	/// <param name="propertyName">Property name.</param>
	protected virtual void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, propertyName);
	}

	#endregion

	#region Equatable

	/// <summary>
	/// Create a copy of <see cref="DiagramSocketType"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override DiagramSocketType Clone()
	{
		return new DiagramSocketType(Type, Name, Color);
	}

	/// <summary>
	/// Compare <see cref="DiagramSocketType"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	protected override bool OnEquals(DiagramSocketType other)
	{
		return Type == other.Type;
	}

	#endregion

	#region IPersistable

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		Type = storage.GetValue<string>(nameof(Type)).To<Type>();
		Name = storage.GetValue<string>(nameof(Name));
		Color = storage.GetValue<Color>(nameof(Color));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Type), Type.GetTypeName(false));
		storage.SetValue(nameof(Name), Name);
		storage.SetValue(nameof(Color), Color);
	}

	#endregion

	/// <inheritdoc />
	public override string ToString() => Name;

	/// <summary>
	/// Unknown data type.
	/// </summary>
	public static readonly DiagramSocketType Any = RegisterType<object>(LocalizedStrings.AnyData, Color.Black);

	/// <summary>
	/// Security.
	/// </summary>
	public static readonly DiagramSocketType Security = RegisterType<Security>(LocalizedStrings.Security, Color.DarkGreen);

	/// <summary>
	/// Market depth.
	/// </summary>
	public static readonly DiagramSocketType MarketDepth = RegisterType<IOrderBookMessage>(LocalizedStrings.MarketDepth, Color.DarkCyan);

	/// <summary>
	/// Quote.
	/// </summary>
	public static readonly DiagramSocketType Quote = RegisterType<QuoteChange>(LocalizedStrings.Quote	, Color.Cyan);

	/// <summary>
	/// Candle.
	/// </summary>
	public static readonly DiagramSocketType Candle = RegisterType<ICandleMessage>(LocalizedStrings.Candles, Color.OrangeRed);

	/// <summary>
	/// Indicator value.
	/// </summary>
	public static readonly DiagramSocketType IndicatorValue = RegisterType<IIndicatorValue>(LocalizedStrings.IndicatorValue, Color.DarkGoldenrod);

	/// <summary>
	/// Order.
	/// </summary>
	public static readonly DiagramSocketType Order = RegisterType<Order>(LocalizedStrings.Order, Color.Olive);

	/// <summary>
	/// Order fail.
	/// </summary>
	public static readonly DiagramSocketType OrderFail = RegisterType<OrderFail>(LocalizedStrings.OrderFail, Color.PaleVioletRed);

	/// <summary>
	/// Own trade.
	/// </summary>
	public static readonly DiagramSocketType MyTrade = RegisterType<MyTrade>(LocalizedStrings.OwnTrade, Color.DarkOliveGreen);

	/// <summary>
	/// Flag.
	/// </summary>
	public static readonly DiagramSocketType Bool = RegisterType<bool>(LocalizedStrings.Flag, Color.DodgerBlue);

	/// <summary>
	/// Numeric value.
	/// </summary>
	public static readonly DiagramSocketType Unit = RegisterType<Unit>(LocalizedStrings.NumericValue, Color.MediumSeaGreen);

	/// <summary>
	/// Comparable values.
	/// </summary>
	public static readonly DiagramSocketType Comparable = RegisterType<IComparable>(LocalizedStrings.Comparison, Color.DarkSlateBlue);

	/// <summary>
	/// Portfolio.
	/// </summary>
	public static readonly DiagramSocketType Portfolio = RegisterType<Portfolio>(LocalizedStrings.Portfolio, Color.Brown);

	/// <summary>
	/// Options.
	/// </summary>
	public static readonly DiagramSocketType Options = RegisterType<IEnumerable<Security>>(LocalizedStrings.Options, Color.DeepPink);

	/// <summary>
	/// Side.
	/// </summary>
	public static readonly DiagramSocketType Side = RegisterType<Sides>(LocalizedStrings.Side, Color.Beige);

	/// <summary>
	/// Candle state.
	/// </summary>
	public static readonly DiagramSocketType CandleStates = RegisterType<CandleStates>(LocalizedStrings.CandleState, Color.OrangeRed);

	/// <summary>
	/// Trade.
	/// </summary>
	public static readonly DiagramSocketType Trade = RegisterType<ITickTradeMessage>(LocalizedStrings.Trade, Color.DarkKhaki);

	/// <summary>
	/// Strategy.
	/// </summary>
	public static readonly DiagramSocketType Strategy = RegisterType<Strategy>(LocalizedStrings.Strategy, Color.DarkBlue);

	/// <summary>
	/// Strategy.
	/// </summary>
	public static readonly DiagramSocketType Date = RegisterType<DateTimeOffset>(LocalizedStrings.Date, Color.Chocolate);

	/// <summary>
	/// Connector.
	/// </summary>
	public static readonly DiagramSocketType Time = RegisterType<TimeSpan>(LocalizedStrings.Time, Color.Coral);

	/// <summary>
	/// Position.
	/// </summary>
	public static readonly DiagramSocketType Position = RegisterType<Position>(LocalizedStrings.Position, Color.SaddleBrown);

	/// <summary>
	/// Order state.
	/// </summary>
	public static readonly DiagramSocketType OrderState = RegisterType<OrderStates>(LocalizedStrings.OrderState, Color.Chartreuse);

	/// <summary>
	/// Black scholes.
	/// </summary>
	public static readonly DiagramSocketType BlackScholes = RegisterType<IBlackScholes>(LocalizedStrings.BlackScholes, Color.Gainsboro);

	/// <summary>
	/// Black scholes.
	/// </summary>
	public static readonly DiagramSocketType BasketBlackScholes = RegisterType<BasketBlackScholes>(LocalizedStrings.Basket, Color.Tan);

	/// <summary>
	/// Text string.
	/// </summary>
	public static readonly DiagramSocketType String = RegisterType<string>(LocalizedStrings.Text, Color.Purple);

	/// <summary>
	/// Get <see cref="DiagramSocketType"/> for <see cref="System.Type"/>.
	/// </summary>
	/// <param name="parameterType">Type.</param>
	/// <returns>Diagram socket type.</returns>
	public static DiagramSocketType GetSocketType(Type parameterType)
	{
		if (parameterType == null)
			throw new ArgumentNullException(nameof(parameterType));

		if (parameterType == typeof(bool))
			return Bool;

		if (parameterType == typeof(string))
			return String;

		if (parameterType.Is<Security>())
			return Security;

		if (parameterType.Is<ICandleMessage>())
			return Candle;

		if (parameterType.Is<IIndicatorValue>())
			return IndicatorValue;

		if (parameterType.Is<IOrderBookMessage>())
			return MarketDepth;

		if (parameterType.Is<QuoteChange>())
			return Quote;

		if (parameterType.Is<ITickTradeMessage>())
			return Trade;

		if (parameterType == typeof(MyTrade))
			return MyTrade;

		if (parameterType == typeof(Order))
			return Order;

		if (parameterType == typeof(Portfolio))
			return Portfolio;

		if (parameterType == typeof(Position))
			return Position;

		if (parameterType == typeof(DateTimeOffset))
			return Date;

		if (parameterType == typeof(TimeSpan))
			return Time;

		if (parameterType == typeof(Unit) ||
			(parameterType.IsNumeric() && !parameterType.IsEnum()) ||
			(parameterType.IsNullable() && parameterType.GetUnderlyingType().IsNumeric() && !parameterType.GetUnderlyingType().IsEnum()))
			return Unit;

		if (parameterType.Is<IEnumerable<Security>>())
			return Options;

		return Any;
	}
}