namespace StockSharp.BusinessEntities;

using System.Runtime.CompilerServices;

/// <summary>
/// Exchange info.
/// </summary>
[Serializable]
[DataContract]
[KnownType(typeof(TimeZoneInfo))]
[KnownType(typeof(TimeZoneInfo.AdjustmentRule))]
[KnownType(typeof(TimeZoneInfo.AdjustmentRule[]))]
[KnownType(typeof(TimeZoneInfo.TransitionTime))]
[KnownType(typeof(DayOfWeek))]
public partial class Exchange : Equatable<Exchange>, IPersistable, INotifyPropertyChanged
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Exchange"/>.
	/// </summary>
	public Exchange()
	{
	}

	private string _name;

	/// <summary>
	/// Exchange code name.
	/// </summary>
	[DataMember]
	public string Name
	{
		get => _name;
		set
		{
			if (Name == value)
				return;

			_name = value;
			Notify();
		}
	}

	private string GetLocName(string language) => FullNameLoc.IsEmpty() ? null : LocalizedStrings.GetString(FullNameLoc, language);

	/// <summary>
	/// Full name.
	/// </summary>
	public string FullName => GetLocName(null);

	private string _fullNameLoc;

	/// <summary>
	/// Full name (localization key).
	/// </summary>
	[DataMember]
	public string FullNameLoc
	{
		get => _fullNameLoc;
		set
		{
			if (FullNameLoc == value)
				return;

			_fullNameLoc = value;
			Notify();
		}
	}

	private CountryCodes? _countryCode;

	/// <summary>
	/// ISO country code.
	/// </summary>
	[DataMember]
	public CountryCodes? CountryCode
	{
		get => _countryCode;
		set
		{
			if (CountryCode == value)
				return;

			_countryCode = value;
			Notify();
		}
	}

	[field: NonSerialized]
	private PropertyChangedEventHandler _propertyChanged;

	event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
	{
		add => _propertyChanged += value;
		remove => _propertyChanged -= value;
	}

	private void Notify([CallerMemberName]string propertyName = null)
	{
		_propertyChanged?.Invoke(this, propertyName);
	}

	/// <inheritdoc />
	public override string ToString() => Name;

	/// <summary>
	/// Compare <see cref="Exchange"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	protected override bool OnEquals(Exchange other)
	{
		return Name == other.Name;
	}

	/// <summary>Serves as a hash function for a particular type. </summary>
	/// <returns>A hash code for the current <see cref="T:System.Object" />.</returns>
	public override int GetHashCode() => Name?.GetHashCode() ?? 0;

	/// <summary>
	/// Create a copy of <see cref="Exchange"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Exchange Clone()
	{
		return new Exchange
		{
			Name = Name,
			FullNameLoc = FullNameLoc,
			CountryCode = CountryCode,
		};
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		Name = storage.GetValue<string>(nameof(Name));
		FullNameLoc = storage.GetValue<string>(nameof(FullNameLoc));
		CountryCode = storage.GetValue<CountryCodes?>(nameof(CountryCode));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Name), Name);
		storage.SetValue(nameof(FullNameLoc), FullNameLoc);
		storage.SetValue(nameof(CountryCode), CountryCode.To<string>());
	}
}