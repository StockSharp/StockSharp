namespace StockSharp.Algo.Indicators;

/// <summary>
/// The indicator type description.
/// </summary>
public class IndicatorType : Equatable<IndicatorType>, IDisposable
{
	/// <summary>
	/// Identifier.
	/// </summary>
	public virtual string Id => Indicator?.GetTypeName(false) ?? string.Empty;

	/// <summary>
	/// Indicator name.
	/// </summary>
	public string Name { get; private set; }

	/// <summary>
	/// The indicator description.
	/// </summary>
	public string Description { get; private set; }

	private void RefreshNameDesc(Type indicator)
	{
		if (indicator is null)
			throw new ArgumentNullException(nameof(indicator));

		Name = indicator.GetDisplayName();
		Description = indicator.GetDescription();
	}

	private Type _indicator;

	/// <summary>
	/// Indicator type.
	/// </summary>
	public Type Indicator
	{
		get => _indicator;
		protected set
		{
			if (_indicator == value)
				return;

			_indicator = value ?? throw new ArgumentNullException(nameof(value));

			RefreshNameDesc(value);
			IsObsolete = value.IsObsolete();
			DocUrl = value.GetDocUrl() ?? string.Empty;
			IsComplex = value.Is<IComplexIndicator>();

			InputValue = value.GetValueType(true);
			OutputValue = value.GetValueType(false);

			IndicatorChanged?.Invoke();
		}
	}

	/// <summary>
	/// <see cref="Indicator"/> changed event.
	/// </summary>
	public event Action IndicatorChanged;

	/// <summary>
	/// Input values type.
	/// </summary>
	public Type InputValue { get; private set; }

	/// <summary>
	/// Result values type.
	/// </summary>
	public Type OutputValue { get; private set; }

	/// <summary>
	/// The <see cref="IndicatorType"/> is obsolete.
	/// </summary>
	public bool IsObsolete { get; private set; }

	/// <summary>
	/// Documentation url.
	/// </summary>
	public string DocUrl { get; private set; }

	/// <summary>
	/// <see cref="IComplexIndicator"/>.
	/// </summary>
	public bool IsComplex { get; private set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorType"/>.
	/// </summary>
	protected IndicatorType()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="IndicatorType"/>.
	/// </summary>
	/// <param name="indicator">Indicator type.</param>
	public IndicatorType(Type indicator)
	{
		Indicator = indicator;

		LocalizedStrings.ActiveLanguageChanged += OnActiveLanguageChanged;
	}

	/// <inheritdoc />
	public virtual void Dispose()
	{
		LocalizedStrings.ActiveLanguageChanged -= OnActiveLanguageChanged;
		GC.SuppressFinalize(this);
	}

	private void OnActiveLanguageChanged()
	{
		if (Indicator is Type t)
			RefreshNameDesc(t);
	}

	/// <summary>
	/// Create a copy of <see cref="IndicatorType"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IndicatorType Clone() => new(Indicator);

	/// <summary>
	/// Compare <see cref="IndicatorType"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	protected override bool OnEquals(IndicatorType other)
		=> Id == other.Id;

	/// <inheritdoc />
	public override int GetHashCode()
		=> Id.GetHashCode();
}