namespace StockSharp.Algo.Indicators;

/// <summary>
/// The full class of linear regression, calculates LinearReg, LinearRegSlope, RSquared and StandardError at the same time.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LinearRegressionKey,
	Description = LocalizedStrings.LinearRegressionDescKey)]
[Browsable(false)]
public class LinearRegression : BaseComplexIndicator<LinearRegressionValue>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="LinearRegression"/>.
	/// </summary>
	public LinearRegression()
		: this(new(), new(), new(), new())
	{
		Length = 11;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LinearRegression"/>.
	/// </summary>
	/// <param name="linearReg">Linear regression.</param>
	/// <param name="rSquared">Regression R-squared.</param>
	/// <param name="regSlope">Coefficient with independent variable, slope of a straight line.</param>
	/// <param name="standardError">Standard error.</param>
	public LinearRegression(LinearReg linearReg, LinearRegRSquared rSquared, LinearRegSlope regSlope, StandardError standardError)
		: base(linearReg, rSquared, regSlope, standardError)
	{
		LinearReg = linearReg;
		RSquared = rSquared;
		LinearRegSlope = regSlope;
		StandardError = standardError;

		Mode = ComplexIndicatorModes.Parallel;
	}

	/// <summary>
	/// Period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => LinearReg.Length;
		set
		{
			LinearReg.Length = RSquared.Length = LinearRegSlope.Length = StandardError.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Linear regression.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LinearRegressionKey,
		Description = LocalizedStrings.LinearRegressionKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public LinearReg LinearReg { get; }

	/// <summary>
	/// Regression R-squared.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RSquaredKey,
		Description = LocalizedStrings.RSquaredKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public LinearRegRSquared RSquared { get; }

	/// <summary>
	/// Standard error.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StandardErrorKey,
		Description = LocalizedStrings.StandardErrorLinearRegKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public StandardError StandardError { get; }

	/// <summary>
	/// Coefficient with independent variable, slope of a straight line.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LRSKey,
		Description = LocalizedStrings.LinearRegSlopeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public LinearRegSlope LinearRegSlope { get; }

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;

	/// <inheritdoc />
	protected override LinearRegressionValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// <see cref="LinearRegression"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LinearRegressionValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="LinearRegression"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class LinearRegressionValue(LinearRegression indicator, DateTimeOffset time) : ComplexIndicatorValue<LinearRegression>(indicator, time)
{
	/// <summary>
	/// Gets the <see cref="LinearRegression.LinearReg"/> value.
	/// </summary>
	public IIndicatorValue LinearRegValue => this[TypedIndicator.LinearReg];

	/// <summary>
	/// Gets the <see cref="LinearRegression.LinearReg"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? LinearReg => LinearRegValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="LinearRegression.RSquared"/> value.
	/// </summary>
	public IIndicatorValue RSquaredValue => this[TypedIndicator.RSquared];

	/// <summary>
	/// Gets the <see cref="LinearRegression.RSquared"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? RSquared => RSquaredValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="LinearRegression.LinearRegSlope"/> value.
	/// </summary>
	public IIndicatorValue LinearRegSlopeValue => this[TypedIndicator.LinearRegSlope];

	/// <summary>
	/// Gets the <see cref="LinearRegression.LinearRegSlope"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? LinearRegSlope => LinearRegSlopeValue.ToNullableDecimal();

	/// <summary>
	/// Gets the <see cref="LinearRegression.StandardError"/> value.
	/// </summary>
	public IIndicatorValue StandardErrorValue => this[TypedIndicator.StandardError];

	/// <summary>
	/// Gets the <see cref="LinearRegression.StandardError"/> value.
	/// </summary>
	[Browsable(false)]
	public decimal? StandardError => StandardErrorValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"LinearReg={LinearReg}, RSquared={RSquared}, LinearRegSlope={LinearRegSlope}, StandardError={StandardError}";
}
