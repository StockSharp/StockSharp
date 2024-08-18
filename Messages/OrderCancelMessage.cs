namespace StockSharp.Messages;

/// <summary>
/// A message containing the data for the cancellation of the order.
/// </summary>
[DataContract]
[Serializable]
public class OrderCancelMessage : OrderMessage
{
	/// <summary>
	/// ID cancellation order.
	/// </summary>
	[DataMember]
	public long? OrderId { get; set; }

	/// <summary>
	/// Cancelling order id (as a string if the electronic board does not use a numeric representation of the identifiers).
	/// </summary>
	[DataMember]
	public string OrderStringId { get; set; }

	/// <summary>
	/// Cancelling balance.
	/// </summary>
	[DataMember]
	public decimal? Balance { get; set; }

	/// <summary>
	/// Cancelling volume. If not specified, then it canceled the entire balance.
	/// </summary>
	[DataMember]
	public decimal? Volume { get; set; }

	/// <summary>
	/// Order side.
	/// </summary>
	[DataMember]
	public Sides? Side { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderCancelMessage"/>.
	/// </summary>
	public OrderCancelMessage()
		: base(MessageTypes.OrderCancel)
	{
	}

	/// <summary>
	/// Initialize <see cref="OrderCancelMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	protected OrderCancelMessage(MessageTypes type)
		: base(type)
	{
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	protected void CopyTo(OrderCancelMessage destination)
	{
		base.CopyTo(destination);

		destination.OrderId = OrderId;
		destination.OrderStringId = OrderStringId;
		destination.Balance = Balance;
		destination.Volume = Volume;
		destination.Side = Side;
	}

	/// <summary>
	/// Create a copy of <see cref="OrderCancelMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new OrderCancelMessage();
		CopyTo(clone);
		return clone;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString();

		if (OrderId != null)
			str += $",OrdId={OrderId.Value}";

		if (!OrderStringId.IsEmpty())
			str += $",OrdStrId={OrderStringId}";

		if (Balance != null)
			str += $",Bal={Balance.Value}";

		if (Volume != null)
			str += $",Vol={Volume.Value}";

		if (Side != null)
			str += $",Side={Side.Value}";

		return str;
	}
}