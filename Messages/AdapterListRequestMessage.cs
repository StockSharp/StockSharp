namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Adapters list request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class AdapterListRequestMessage : BaseSubscriptionMessage
	{
		/// <summary>
		/// Initialize <see cref="AdapterListRequestMessage"/>.
		/// </summary>
		public AdapterListRequestMessage()
			: base(MessageTypes.AdapterListRequest)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.Create(typeof(AdapterResponseMessage), null);

		/// <summary>
		/// Create a copy of <see cref="AdapterListRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new AdapterListRequestMessage();
			CopyTo(clone);
			return clone;
		}
	}
}