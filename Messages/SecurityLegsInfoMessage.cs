namespace StockSharp.Messages;

/// <summary>
/// Security legs result message.
/// </summary>
[Serializable]
[DataContract]
public class SecurityLegsInfoMessage : BaseSubscriptionIdMessage<SecurityLegsInfoMessage>
{
	/// <summary>
	/// Initialize <see cref="SecurityLegsInfoMessage"/>.
	/// </summary>
	public SecurityLegsInfoMessage()
		: base(MessageTypes.SecurityLegsInfo)
	{
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.SecurityLegs;

	private IDictionary<SecurityId, IEnumerable<SecurityId>> _legs = new Dictionary<SecurityId, IEnumerable<SecurityId>>();

	/// <summary>
	/// Security legs.
	/// </summary>
	[DataMember]
	[XmlIgnore]
	public IDictionary<SecurityId, IEnumerable<SecurityId>> Legs
	{
		get => _legs;
		set => _legs = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <inheritdoc />
	public override void CopyTo(SecurityLegsInfoMessage destination)
	{
		base.CopyTo(destination);

		destination.Legs = Legs.ToDictionary(p => p.Key, p => (IEnumerable<SecurityId>)[.. p.Value]);
	}

	/// <inheritdoc />
	public override string ToString() =>
		base.ToString() + $",Legs={Legs.Select(p => p.ToString()).JoinComma()}";
}