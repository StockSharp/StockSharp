namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Security routes result message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class SecurityRouteListFinishedMessage : BaseResultMessage<SecurityRouteListFinishedMessage>
	{
		/// <summary>
		/// Initialize <see cref="SecurityRouteListFinishedMessage"/>.
		/// </summary>
		public SecurityRouteListFinishedMessage()
			: base(MessageTypes.SecurityRouteListFinished)
		{
		}
	}
}