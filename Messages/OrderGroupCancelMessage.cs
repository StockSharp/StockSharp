namespace StockSharp.Messages;

/// <summary>
/// The message containing the order group cancel filter.
/// </summary>
[DataContract]
[Serializable]
public class OrderGroupCancelMessage : OrderMessage, ISecurityTypesMessage
{
	/// <summary>
	/// <see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StopOrdersKey,
		Description = LocalizedStrings.StopOrdersDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public bool? IsStop { get; set; }

	/// <summary>
	/// Order side. If the value is <see langword="null" />, the direction does not use.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DirectionKey,
		Description = LocalizedStrings.CancelOrdersSideKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public Sides? Side { get; set; }

	/// <summary>
	/// Securities types.
	/// </summary>
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TypeKey,
		Description = LocalizedStrings.SecurityTypeDescKey,
		GroupName = LocalizedStrings.OptionsKey)]
	public SecurityTypes[] SecurityTypes { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderGroupCancelMessage"/>.
	/// </summary>
	public OrderGroupCancelMessage()
		: base(MessageTypes.OrderGroupCancel)
	{
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + $",IsStop={IsStop},Side={Side}";
	}

	/// <summary>
	/// Create a copy of <see cref="OrderGroupCancelMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new OrderGroupCancelMessage();

		CopyTo(clone);

		return clone;
	}

	/// <summary>
	/// Copy the message into the <paramref name="destination" />.
	/// </summary>
	/// <param name="destination">The object, to which copied information.</param>
	public void CopyTo(OrderGroupCancelMessage destination)
	{
		base.CopyTo(destination);

		destination.IsStop = IsStop;
		destination.Side = Side;
		destination.SecurityTypes = SecurityTypes?.ToArray();
	}
}