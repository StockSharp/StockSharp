namespace StockSharp.Messages
{
	/// <summary>
	/// Reset state message.
	/// </summary>
	public sealed class ResetMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ResetMessage"/>.
		/// </summary>
		public ResetMessage()
			: base(MessageTypes.Reset)
		{
		}
	}
}