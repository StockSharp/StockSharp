namespace StockSharp.Algo
{
	using StockSharp.Messages;

	/// <summary>
	/// The message, containing security id to remove.
	/// </summary>
	public class SecurityRemoveMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityRemoveMessage"/>.
		/// </summary>
		public SecurityRemoveMessage()
			: base(ExtendedMessageTypes.RemoveSecurity)
		{
		}

		/// <summary>
		/// Security ID.
		/// </summary>
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
}