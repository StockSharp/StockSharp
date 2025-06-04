namespace StockSharp.Messages;

/// <summary>
/// Message portfolio lookup for specified criteria.
/// </summary>
[DataContract]
[Serializable]
public class PortfolioLookupMessage : PortfolioMessage, INullableSecurityIdMessage, IStrategyIdMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="PortfolioLookupMessage"/>.
	/// </summary>
	public PortfolioLookupMessage()
		: base(MessageTypes.PortfolioLookup)
	{
	}

	/// <inheritdoc />
	[DataMember]
	public string StrategyId { get; set; }

	/// <summary>
	/// Side.
	/// </summary>
	[DataMember]
	public Sides? Side { get; set; }

	/// <inheritdoc />
	public override DataType DataType => DataType.PositionChanges;

	/// <inheritdoc />
	[TypeConverter(typeof(StringToSecurityIdTypeConverter))]
	public SecurityId? SecurityId { get; set; }

	/// <inheritdoc />
	public override bool SpecificItemRequest => !StrategyId.IsEmpty();

	/// <summary>
	/// Create a copy of <see cref="PortfolioLookupMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new PortfolioLookupMessage();
		CopyTo(clone);
		return clone;
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	protected virtual void CopyTo(PortfolioLookupMessage destination)
	{
		base.CopyTo(destination);

		destination.SecurityId = SecurityId;
		destination.StrategyId = StrategyId;
		destination.Side = Side;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString();

		if (!IsSubscribe)
			str += $",IsSubscribe={IsSubscribe}";

		if (SecurityId != null)
			str += $",Sec={SecurityId}";

		if (!StrategyId.IsEmpty())
			str += $",Strategy={StrategyId}";

		if (Side != null)
			str += $",Side={Side.Value}";

		return str;
	}
}