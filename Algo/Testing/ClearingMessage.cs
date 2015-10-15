namespace StockSharp.Algo.Testing
{
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The message about performing clearing on exchange.
	/// </summary>
	class ClearingMessage : Message
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Shall order book be cleared.
		/// </summary>
		public bool ClearMarketDepth { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ClearingMessage"/>.
		/// </summary>
		public ClearingMessage()
			: base(ExtendedMessageTypes.Clearing)
		{
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Sec={0}".Put(SecurityId);
		}
	}
}