namespace StockSharp.Algo.Indicators;

/// <summary>
/// Ichimoku.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/ichimoku.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.IchimokuKey,
	Description = LocalizedStrings.IchimokuKey)]
[Doc("topics/api/indicators/list_of_indicators/ichimoku.html")]
[IndicatorOut(typeof(IchimokuValue))]
public class Ichimoku : BaseComplexIndicator<IchimokuValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Ichimoku"/>.
	/// </summary>
	public Ichimoku()
		: this(new() { Length = 9 }, new() { Length = 26 })
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Ichimoku"/>.
	/// </summary>
	/// <param name="tenkan">Tenkan line.</param>
	/// <param name="kijun">Kijun line.</param>
	public Ichimoku(IchimokuLine tenkan, IchimokuLine kijun)
	{
		AddInner(Tenkan = tenkan ?? throw new ArgumentNullException(nameof(tenkan)));
		AddInner(Kijun = kijun ?? throw new ArgumentNullException(nameof(kijun)));
		AddInner(SenkouA = new(Tenkan, Kijun));
		AddInner(SenkouB = new(Kijun) { Length = 52 });
		AddInner(Chinkou = new() { Length = kijun.Length });
	}

	/// <summary>
	/// Tenkan line.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TenkanKey,
		Description = LocalizedStrings.TenkanLineKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public IchimokuLine Tenkan { get; }

	/// <summary>
	/// Kijun line.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.KijunKey,
		Description = LocalizedStrings.KijunLineKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public IchimokuLine Kijun { get; }

	/// <summary>
	/// Senkou (A) line.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SenkouAKey,
		Description = LocalizedStrings.SenkouADescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public IchimokuSenkouALine SenkouA { get; }

	/// <summary>
	/// Senkou (B) line.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SenkouBKey,
		Description = LocalizedStrings.SenkouBDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public IchimokuSenkouBLine SenkouB { get; }

	/// <summary>
	/// Chinkou line.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ChinkouKey,
		Description = LocalizedStrings.ChinkouLineKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public IchimokuChinkouLine Chinkou { get; }

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" T={Tenkan.Length} K={Kijun.Length} A={SenkouA.Length} B={SenkouB.Length} C={Chinkou.Length}";

	/// <inheritdoc />
	protected override IchimokuValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="Ichimoku"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="IchimokuValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="Ichimoku"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class IchimokuValue(Ichimoku indicator, DateTimeOffset time) : ComplexIndicatorValue<Ichimoku>(indicator, time)
{
	/// <summary>
	/// Gets the <see cref="Ichimoku.Tenkan"/> value.
	/// </summary>
	public IIndicatorValue TenkanValue => this[TypedIndicator.Tenkan];

	/// <summary>
	/// Gets the <see cref="Ichimoku.Tenkan"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Tenkan => TenkanValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="Ichimoku.Kijun"/> value.
	/// </summary>
	public IIndicatorValue KijunValue => this[TypedIndicator.Kijun];

	/// <summary>
	/// Gets the <see cref="Ichimoku.Kijun"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Kijun => KijunValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="Ichimoku.SenkouA"/> value.
	/// </summary>
	public IIndicatorValue SenkouAValue => this[TypedIndicator.SenkouA];

	/// <summary>
	/// Gets the <see cref="Ichimoku.SenkouA"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? SenkouA => SenkouAValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="Ichimoku.SenkouB"/> value.
	/// </summary>
	public IIndicatorValue SenkouBValue => this[TypedIndicator.SenkouB];

	/// <summary>
	/// Gets the <see cref="Ichimoku.SenkouB"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? SenkouB => SenkouBValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="Ichimoku.Chinkou"/> value.
	/// </summary>
	public IIndicatorValue ChinkouValue => this[TypedIndicator.Chinkou];

	/// <summary>
	/// Gets the <see cref="Ichimoku.Chinkou"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? Chinkou => ChinkouValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Tenkan={Tenkan}, Kijun={Kijun}, SenkouA={SenkouA}, SenkouB={SenkouB}, Chinkou={Chinkou}";
}
