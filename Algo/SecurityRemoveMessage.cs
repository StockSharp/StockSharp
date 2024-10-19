namespace StockSharp.Algo;

/// <summary>
/// The message, containing security id to remove.
/// </summary>
[DataContract]
[Serializable]
public class SecurityRemoveMessage : Message, ISecurityIdMessage
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SecurityRemoveMessage"/>.
	/// </summary>
	public SecurityRemoveMessage()
		: base(ExtendedMessageTypes.RemoveSecurity)
	{
	}

	/// <inheritdoc />
	[DataMember]
	public SecurityId SecurityId { get; set; }

	/// <summary>
	/// Create a copy of <see cref="SecurityRemoveMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		return new SecurityRemoveMessage
		{
			SecurityId = SecurityId,
		};
	}
}