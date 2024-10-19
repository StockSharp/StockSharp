namespace StockSharp.Messages;

/// <summary>
/// Reset state message.
/// </summary>
[DataContract]
[Serializable]
public sealed class ResetMessage : Message
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ResetMessage"/>.
	/// </summary>
	public ResetMessage()
		: base(MessageTypes.Reset)
	{
	}

	/// <summary>
	/// Create a copy of <see cref="ResetMessage"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override Message Clone() => new ResetMessage();
}