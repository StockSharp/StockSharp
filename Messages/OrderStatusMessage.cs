namespace StockSharp.Messages;

/// <summary>
/// A message requesting current registered orders and trades.
/// </summary>
[DataContract]
[Serializable]
public class OrderStatusMessage : OrderCancelMessage, ISubscriptionMessage
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FromKey,
		Description = LocalizedStrings.StartDateDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset? From { get; set; }

	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UntilKey,
		Description = LocalizedStrings.ToDateDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public DateTimeOffset? To { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long? Skip { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long? Count { get; set; }

	/// <inheritdoc />
	[DataMember]
	public FillGapsDays? FillGaps { get; set; }

	/// <inheritdoc />
	[DataMember]
	public bool IsSubscribe { get; set; }

	private OrderStates[] _states = [];

	/// <summary>
	/// Filter order by the specified states.
	/// </summary>
	[DataMember]
	public OrderStates[] States
	{
		get => _states;
		set => _states = value ?? throw new ArgumentNullException(nameof(value));
	}

	bool ISubscriptionMessage.FilterEnabled
		=>
		States.Length != 0 || SecurityId != default ||
		!PortfolioName.IsEmpty() || Side != null || Volume != null;

	bool ISubscriptionMessage.SpecificItemRequest => this.HasOrderId();

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderStatusMessage"/>.
	/// </summary>
	public OrderStatusMessage()
		: base(MessageTypes.OrderStatus)
	{
	}

	DataType ISubscriptionMessage.DataType => DataType.Transactions;

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	protected void CopyTo(OrderStatusMessage destination)
	{
		base.CopyTo(destination);

		destination.From = From;
		destination.To = To;
		destination.Skip = Skip;
		destination.Count = Count;
		destination.FillGaps = FillGaps;
		destination.IsSubscribe = IsSubscribe;
		destination.States = [.. States];
	}

	/// <summary>
	/// Create a copy of <see cref="OrderStatusMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new OrderStatusMessage();
		CopyTo(clone);
		return clone;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString();

		str += $",IsSubscribe={IsSubscribe}";

		if (From != default)
			str += $",From={From}";

		if (To != default)
			str += $",To={To}";

		if (Skip != default)
			str += $",Skip={Skip}";

		if (Count != default)
			str += $",Count={Count}";

		if (FillGaps != default)
			str += $",Gaps={FillGaps}";

		if (States.Length > 0)
			str += $",States={States.Select(s => s.To<string>()).JoinComma()}";

		return str;
	}
}