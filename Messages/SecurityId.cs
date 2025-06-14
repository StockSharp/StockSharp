namespace StockSharp.Messages;

using System.Globalization;

/// <summary>
/// Security ID.
/// </summary>
[DataContract]
[Serializable]
public struct SecurityId : IEquatable<SecurityId>, IPersistable
{
	static SecurityId()
	{
		Money.EnsureGetHashCode();
		News.EnsureGetHashCode();
	}

	private string _securityCode;

	/// <summary>
	/// Security code.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecCodeKey,
		Description = LocalizedStrings.SecCodeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string SecurityCode
	{
		readonly get => _securityCode;
		set
		{
			CheckImmutable();
			_securityCode = value;
			_hashCode = default;
		}
	}

	private string _boardCode;

	/// <summary>
	/// Electronic board code.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BoardKey,
		Description = LocalizedStrings.BoardCodeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string BoardCode
	{
		readonly get => _boardCode;
		set
		{
			CheckImmutable();
			_boardCode = value;
			_hashCode = default;
		}
	}

	private object _native;

	/// <summary>
	/// Native (internal) trading system security id.
	/// </summary>
	public object Native
	{
		readonly get => _nativeAsInt != 0 ? _nativeAsInt : _native;
		set
		{
			CheckImmutable();

			_native = value;

			_nativeAsInt = 0;

			if (value is long l)
				_nativeAsInt = l;

			_hashCode = default;
		}
	}

	private long _nativeAsInt;

	/// <summary>
	/// Native (internal) trading system security id represented as integer.
	/// </summary>
	public long NativeAsInt
	{
		readonly get => _nativeAsInt;
		set
		{
			CheckImmutable();

			_nativeAsInt = value;
			_hashCode = default;
		}
	}

	/// <summary>
	/// ID in SEDOL format (Stock Exchange Daily Official List).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SedolKey,
		Description = LocalizedStrings.SedolDescKey)]
	public string Sedol { get; set; }

	/// <summary>
	/// ID in CUSIP format (Committee on Uniform Securities Identification Procedures).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CusipKey,
		Description = LocalizedStrings.CusipDescKey)]
	public string Cusip { get; set; }

	/// <summary>
	/// ID in ISIN format (International Securities Identification Number).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IsinKey,
		Description = LocalizedStrings.IsinDescKey)]
	public string Isin { get; set; }

	/// <summary>
	/// ID in RIC format (Reuters Instrument Code).
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RicKey,
		Description = LocalizedStrings.RicDescKey)]
	public string Ric { get; set; }

	/// <summary>
	/// ID in Bloomberg format.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BloombergKey,
		Description = LocalizedStrings.BloombergDescKey)]
	public string Bloomberg { get; set; }

	/// <summary>
	/// ID in IQFeed format.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IQFeedKey,
		Description = LocalizedStrings.IQFeedDescKey)]
	public string IQFeed { get; set; }

	/// <summary>
	/// ID in Interactive Brokers format.
	/// </summary>
	[DataMember]
	//[Nullable]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.InteractiveBrokersKey,
		Description = LocalizedStrings.InteractiveBrokersDescKey)]
	public int? InteractiveBrokers { get; set; }

	/// <summary>
	/// ID in Plaza format.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PlazaKey,
		Description = LocalizedStrings.PlazaDescKey)]
	public string Plaza { get; set; }

	private int _hashCode;

	/// <summary>
	/// Get the hash code of the object.
	/// </summary>
	/// <returns>A hash code.</returns>
	public override int GetHashCode()
	{
		return EnsureGetHashCode();
	}

	private int EnsureGetHashCode()
	{
		if (_hashCode == 0)
		{
			if (_nativeAsInt == default && _native == default && _securityCode == default && _boardCode == default)
				return 0;

			_hashCode = (_nativeAsInt != 0 ? _nativeAsInt.GetHashCode() : _native?.GetHashCode())
			            ?? ((_securityCode?.ToUpperInvariant() ?? string.Empty).GetHashCode() ^ (_boardCode?.ToUpperInvariant() ?? string.Empty).GetHashCode());
		}

		return _hashCode;
	}

	/// <summary>
	/// Compare <see cref="SecurityId"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	public override bool Equals(object other)
	{
		return other is SecurityId secId && Equals(secId);
	}

	/// <summary>
	/// Compare <see cref="SecurityId"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	public bool Equals(SecurityId other)
	{
		if (EnsureGetHashCode() != other.EnsureGetHashCode())
			return false;

		if (_nativeAsInt != 0)
			return _nativeAsInt.Equals(other._nativeAsInt);

		if (_native != null)
			return _native.Equals(other._native);

		return _securityCode.EqualsIgnoreCase(other._securityCode) && _boardCode.EqualsIgnoreCase(other._boardCode);
	}

	/// <summary>
	/// Compare the inequality of two identifiers.
	/// </summary>
	/// <param name="left">Left operand.</param>
	/// <param name="right">Right operand.</param>
	/// <returns><see langword="true" />, if identifiers are equal, otherwise, <see langword="false" />.</returns>
	public static bool operator !=(SecurityId left, SecurityId right)
	{
		return !(left == right);
	}

	/// <summary>
	/// Compare two identifiers for equality.
	/// </summary>
	/// <param name="left">Left operand.</param>
	/// <param name="right">Right operand.</param>
	/// <returns><see langword="true" />, if the specified identifiers are equal, otherwise, <see langword="false" />.</returns>
	public static bool operator ==(SecurityId left, SecurityId right)
	{
		return left.Equals(right);
	}

	/// <inheritdoc />
	public override readonly string ToString()
	{
		var id = $"{SecurityCode}@{BoardCode}";

		if (Native != null)
			id += $",Native:{Native}";

		//if (SecurityType != null)
		//	id += $",Type:{SecurityType.Value}";

		if (!Isin.IsEmpty())
			id += $",ISIN:{Isin}";

		if (!IQFeed.IsEmpty())
			id += $",IQFeed:{IQFeed}";

		if (InteractiveBrokers != null)
			id += $",IB:{InteractiveBrokers}";

		return id;
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		SecurityCode = storage.GetValue<string>(nameof(SecurityCode));
		BoardCode = storage.GetValue<string>(nameof(BoardCode));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public readonly void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(SecurityCode), SecurityCode);
		storage.SetValue(nameof(BoardCode), BoardCode);
	}

	/// <summary>
	/// Board code for combined security.
	/// </summary>
	public const string AssociatedBoardCode = "ALL";

	/// <summary>
	/// Create security id with board code set as <see cref="AssociatedBoardCode"/>.
	/// </summary>
	/// <param name="securityCode">Security code.</param>
	/// <returns>Security ID.</returns>
	public static SecurityId CreateAssociated(string securityCode)
	{
		return new SecurityId
		{
			SecurityCode = securityCode,
			BoardCode = AssociatedBoardCode,
		};
	}

	/// <summary>
	/// "Money" security id.
	/// </summary>
	public static readonly SecurityId Money = new SecurityId
	{
		SecurityCode = "MONEY",
		BoardCode = AssociatedBoardCode,
		IsSpecial = true,
	}.Immutable();

	/// <summary>
	/// "News" security id.
	/// </summary>
	public static readonly SecurityId News = new SecurityId
	{
		SecurityCode = "NEWS",
		BoardCode = AssociatedBoardCode,
		IsSpecial = true,
	}.Immutable();

	/// <summary>
	/// Determines the id is <see cref="Money"/> or <see cref="News"/>.
	/// </summary>
	public bool IsSpecial { get; private set; }

	private bool _immutable;

	/// <summary>
	/// Make immutable.
	/// </summary>
	/// <returns><see cref="SecurityId"/>.</returns>
	public SecurityId Immutable()
	{
		_immutable = true;
		return this;
	}

	private readonly void CheckImmutable()
	{
		if (_immutable)
			throw new InvalidOperationException(LocalizedStrings.CannotBeModified);
	}
}

/// <summary>
/// Converter to use with <see cref="SecurityId"/> properties.
/// </summary>
public class StringToSecurityIdTypeConverter : TypeConverter
{
	/// <inheritdoc />
	public override bool CanConvertFrom(ITypeDescriptorContext ctx, Type sourceType)
		=> sourceType == typeof(string) || base.CanConvertFrom(ctx, sourceType);

	/// <inheritdoc />
	public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo culture, object value)
	{
		if (value is not string securityId)
			return base.ConvertFrom(ctx, culture, value);

		var isNullable = ctx.PropertyDescriptor?.PropertyType.IsNullable() == true;

		const string delimiter = "@";

		var index = securityId.LastIndexOfIgnoreCase(delimiter);

		return index < 0 ?
			isNullable ? (SecurityId?)null : default(SecurityId) :
			new SecurityId { SecurityCode = securityId.Substring(0, index), BoardCode = securityId.Substring(index + delimiter.Length, securityId.Length - index - delimiter.Length) };
	}
}