namespace StockSharp.Messages;

/// <summary>
/// Security legs request message.
/// </summary>
[Serializable]
[DataContract]
public class SecurityLegsRequestMessage : BaseRequestMessage
{
	/// <summary>
	/// Initialize <see cref="SecurityLegsRequestMessage"/>.
	/// </summary>
	public SecurityLegsRequestMessage()
		: base(MessageTypes.SecurityLegsRequest)
	{
	}

	/// <inheritdoc />
	public override DataType DataType => DataType.SecurityLegs;

	/// <summary>
	/// The filter for securities search.
	/// </summary>
	[DataMember]
	public string Like { get; set; }

	/// <summary>
	/// Create a copy of <see cref="SecurityLegsRequestMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone()
	{
		var clone = new SecurityLegsRequestMessage
		{
			Like = Like,
		};

		CopyTo(clone);

		return clone;
	}
}