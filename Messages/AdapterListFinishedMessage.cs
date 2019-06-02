namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Adapters result message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class AdapterListFinishedMessage : BaseResultMessage<AdapterListFinishedMessage>
	{
		/// <summary>
		/// Initialize <see cref="AdapterListFinishedMessage"/>.
		/// </summary>
		public AdapterListFinishedMessage()
			: base(MessageTypes.AdapterListFinished)
		{
		}
	}
}