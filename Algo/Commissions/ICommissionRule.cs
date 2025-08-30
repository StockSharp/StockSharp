namespace StockSharp.Algo.Commissions;

/// <summary>
/// The commission calculating rule interface.
/// </summary>
public interface ICommissionRule : IPersistable
{
	/// <summary>
	/// Title.
	/// </summary>
	string Title { get; }

	/// <summary>
	/// Commission value.
	/// </summary>
	Unit Value { get; }

	/// <summary>
	/// To reset the state.
	/// </summary>
	void Reset();

	/// <summary>
	/// To calculate commission.
	/// </summary>
	/// <param name="message">The message containing the information about the order or own trade.</param>
	/// <returns>The commission. If the commission cannot be calculated then <see langword="null" /> will be returned.</returns>
	decimal? Process(ExecutionMessage message);
}

/// <summary>
/// The commission calculating rule.
/// </summary>
[DataContract]
public abstract class CommissionRule : NotifiableObject, ICommissionRule
{
	/// <summary>
	/// Initialize <see cref="CommissionRule"/>.
	/// </summary>
	protected CommissionRule()
	{
		UpdateTitle();
	}

	private Unit _value = new();

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CommissionKey,
		Description = LocalizedStrings.CommissionValueKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public Unit Value
	{
		get => _value;
		set
		{
			_value = value ?? throw new ArgumentNullException(nameof(value));
			NotifyChanged();
		}
	}

	/// <summary>
	/// Get title.
	/// </summary>
	protected virtual string GetTitle() => string.Empty;

	/// <summary>
	/// Update title.
	/// </summary>
	protected void UpdateTitle() => Title = GetTitle();

	private string _title;

	/// <inheritdoc />
	[Browsable(false)]
	public string Title
	{
		get => _title;
		private set
		{
			_title = value;
			NotifyChanged();
		}
	}

	/// <inheritdoc />
	public virtual void Reset()
	{
	}

	/// <inheritdoc />
	public abstract decimal? Process(ExecutionMessage message);

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		Value = storage.GetValue<Unit>(nameof(Value));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Value), Value);
	}

	/// <summary>
	/// Get commission value using price and volume. For percent units, uses turnover (price * volume).
	/// Absolute units return the absolute value.
	/// </summary>
	/// <param name="price">Price base (order or trade).</param>
	/// <param name="volume">Volume base (order or trade).</param>
	/// <returns>Commission value or null if cannot be calculated.</returns>
	protected decimal? GetValue(decimal? price, decimal? volume)
	{
		if (Value.Type != UnitTypes.Percent)
			return (decimal)Value;

		if (price == null)
			return null;

		var vol = volume ?? 1m;
		var turnover = price.Value * vol;
		return (turnover * Value.Value) / 100m;
	}
}