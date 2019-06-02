namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Users search result message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class UserLookupResultMessage : BaseResultMessage<UserLookupResultMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UserLookupResultMessage"/>.
		/// </summary>
		public UserLookupResultMessage()
			: base(MessageTypes.UserLookupResult)
		{
		}
	}
}