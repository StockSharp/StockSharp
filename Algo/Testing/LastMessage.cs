namespace StockSharp.Algo.Testing
{
	using StockSharp.Messages;

	/// <summary>
	/// The message, informing on end of data occurrence.
	/// </summary>
	class LastMessage : Message
	{
		/// <summary>
		/// The data transfer is completed due to error.
		/// </summary>
		public bool IsError { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LastMessage"/>.
		/// </summary>
		public LastMessage()
			: base(ExtendedMessageTypes.Last)
		{
		}
	}
}