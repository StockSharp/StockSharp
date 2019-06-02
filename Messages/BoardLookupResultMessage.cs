namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Boards search result message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class BoardLookupResultMessage : BaseResultMessage<BoardLookupResultMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BoardLookupResultMessage"/>.
		/// </summary>
		public BoardLookupResultMessage()
			: base(MessageTypes.BoardLookupResult)
		{
		}
	}
}