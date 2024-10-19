namespace StockSharp.BusinessEntities;

using System.Xml;
using System.Runtime.CompilerServices;

/// <summary>
/// Information about electronic board.
/// </summary>
[Serializable]
[DataContract]
public partial class ExchangeBoard : Equatable<ExchangeBoard>, IPersistable, INotifyPropertyChanged
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ExchangeBoard"/>.
	/// </summary>
	public ExchangeBoard()
	{
	}

	private string _code = string.Empty;

	/// <summary>
	/// Board code.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CodeKey,
		Description = LocalizedStrings.BoardCodeKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.GeneralKey
	)]
	public string Code
	{
		get => _code;
		set
		{
			if (Code == value)
				return;

			_code = value ?? throw new ArgumentNullException(nameof(value));
			Notify();
		}
	}

	private TimeSpan _expiryTime;

	/// <summary>
	/// Securities expiration times.
	/// </summary>
	//[TimeSpan]
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExpiryDateKey,
		Description = LocalizedStrings.SecExpirationTimeKey,
		GroupName = LocalizedStrings.GeneralKey
	)]
	[XmlIgnore]
	public TimeSpan ExpiryTime
	{
		get => _expiryTime;
		set
		{
			if (ExpiryTime == value)
				return;

			_expiryTime = value;
			Notify();
		}
	}

	/// <summary>
	/// Reserved.
	/// </summary>
	[Browsable(false)]
	[XmlElement(DataType = "duration", ElementName = nameof(ExpiryTime))]
	public string ExpiryTimeStr
	{
		// XmlSerializer does not support TimeSpan, so use this property for
		// serialization instead.
		get => XmlConvert.ToString(ExpiryTime);
		set => ExpiryTime = value.IsEmpty() ? TimeSpan.Zero : XmlConvert.ToTimeSpan(value);
	}

	/// <summary>
	/// Exchange, where board is situated.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExchangeInfoKey,
		Description = LocalizedStrings.BoardExchangeKey,
		GroupName = LocalizedStrings.GeneralKey
	)]
	public Exchange Exchange { get; set; }

	private WorkingTime _workingTime = new() { IsEnabled = true };

	/// <summary>
	/// Board working hours.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.WorkingTimeKey,
		Description = LocalizedStrings.WorkingHoursKey,
		GroupName = LocalizedStrings.GeneralKey
	)]
	public WorkingTime WorkingTime
	{
		get => _workingTime;
		set
		{
			if (WorkingTime == value)
				return;

			_workingTime = value ?? throw new ArgumentNullException(nameof(value));
			Notify();
		}
	}

	[field: NonSerialized]
	private TimeZoneInfo _timeZone = TimeZoneInfo.Utc;

	/// <summary>
	/// Information about the time zone where the exchange is located.
	/// </summary>
	[XmlIgnore]
	//[DataMember]
	public TimeZoneInfo TimeZone
	{
		get => _timeZone;
		set
		{
			if (TimeZone == value)
				return;

			_timeZone = value ?? throw new ArgumentNullException(nameof(value));
			Notify();
		}
	}

	/// <summary>
	/// Reserved.
	/// </summary>
	[Browsable(false)]
	[DataMember]
	public string TimeZoneStr
	{
		get => TimeZone.To<string>();
		set => TimeZone = value.To<TimeZoneInfo>();
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
	public override string ToString()
	{
		return "{0} ({1})".Put(Code, Exchange);
	}

	/// <summary>
	/// Compare <see cref="ExchangeBoard"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	protected override bool OnEquals(ExchangeBoard other)
	{
		return Code == other.Code && Exchange == other.Exchange;
	}

	private int _hashCode;

	/// <summary>
	/// Get the hash code of the object <see cref="ExchangeBoard"/>.
	/// </summary>
	/// <returns>A hash code.</returns>
	public override int GetHashCode()
	{
		if (_hashCode == 0)
			_hashCode = Code.GetHashCode() ^ (Exchange == null ? 0 : Exchange.GetHashCode());

		return _hashCode;
	}

	/// <summary>
	/// Create a copy of <see cref="ExchangeBoard"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override ExchangeBoard Clone()
	{
		return new ExchangeBoard
		{
			Exchange = Exchange,
			Code = Code,
			ExpiryTime = ExpiryTime,
			WorkingTime = WorkingTime.Clone(),
			TimeZone = TimeZone,
		};
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		Exchange = storage.GetValue<SettingsStorage>(nameof(Exchange))?.Load<Exchange>();
		Code = storage.GetValue<string>(nameof(Code));
		ExpiryTime = storage.GetValue<TimeSpan>(nameof(ExpiryTime));
		WorkingTime = storage.GetValue<SettingsStorage>(nameof(WorkingTime)).Load<WorkingTime>();
		TimeZone = storage.GetValue(nameof(TimeZone), TimeZone);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Exchange), Exchange?.Save());
		storage.SetValue(nameof(Code), Code);
		//storage.SetValue(nameof(IsSupportMarketOrders), IsSupportMarketOrders);
		//storage.SetValue(nameof(IsSupportAtomicReRegister), IsSupportAtomicReRegister);
		storage.SetValue(nameof(ExpiryTime), ExpiryTime);
		storage.SetValue(nameof(WorkingTime), WorkingTime.Save());
		storage.SetValue(nameof(TimeZone), TimeZone);
	}
}