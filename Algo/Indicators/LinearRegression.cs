namespace StockSharp.Algo.Indicators;

/// <summary>
/// The full class of linear regression, calculates LinearReg, LinearRegSlope, RSquared and StandardError at the same time.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LinearRegressionKey,
	Description = LocalizedStrings.LinearRegressionDescKey)]
[Browsable(false)]
public class LinearRegression : BaseComplexIndicator<ILinearRegressionValue>
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
	protected override ILinearRegressionValue CreateValue(DateTime time)
		=> new LinearRegressionValue(this, time);
}

/// <summary>
/// <see cref="LinearRegression"/> indicator value.
/// </summary>
public interface ILinearRegressionValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the <see cref="LinearRegression.LinearReg"/> value.
	/// </summary>
	IIndicatorValue LinearRegValue { get; }

	/// <summary>
	/// Gets the <see cref="LinearRegression.LinearReg"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? LinearReg { get; }

	/// <summary>
	/// Gets the <see cref="LinearRegression.RSquared"/> value.
	/// </summary>
	IIndicatorValue RSquaredValue { get; }

	/// <summary>
	/// Gets the <see cref="LinearRegression.RSquared"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? RSquared { get; }

	/// <summary>
	/// Gets the <see cref="LinearRegression.LinearRegSlope"/> value.
	/// </summary>
	IIndicatorValue LinearRegSlopeValue { get; }

	/// <summary>
	/// Gets the <see cref="LinearRegression.LinearRegSlope"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? LinearRegSlope { get; }

	/// <summary>
	/// Gets the <see cref="LinearRegression.StandardError"/> value.
	/// </summary>
	IIndicatorValue StandardErrorValue { get; }

	/// <summary>
	/// Gets the <see cref="LinearRegression.StandardError"/> value.
	/// </summary>
	[Browsable(false)]
	decimal? StandardError { get; }
}

/// <summary>
/// LinearRegression indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LinearRegressionValue"/> class.
/// </remarks>
/// <param name="indicator">The parent LinearRegression indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class LinearRegressionValue(LinearRegression indicator, DateTime time) : ComplexIndicatorValue<LinearRegression>(indicator, time), ILinearRegressionValue
{
	/// <inheritdoc />
	public IIndicatorValue LinearRegValue => this[TypedIndicator.LinearReg];
	/// <inheritdoc />
	public decimal? LinearReg => LinearRegValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue RSquaredValue => this[TypedIndicator.RSquared];
	/// <inheritdoc />
	public decimal? RSquared => RSquaredValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue LinearRegSlopeValue => this[TypedIndicator.LinearRegSlope];
	/// <inheritdoc />
	public decimal? LinearRegSlope => LinearRegSlopeValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue StandardErrorValue => this[TypedIndicator.StandardError];
	/// <inheritdoc />
	public decimal? StandardError => StandardErrorValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"LinearReg={LinearReg}, RSquared={RSquared}, LinearRegSlope={LinearRegSlope}, StandardError={StandardError}";
}
