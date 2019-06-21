namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Portfolio routes result message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class PortfolioRouteListFinishedMessage : BaseResultMessage<PortfolioRouteListFinishedMessage>
	{
		/// <summary>
		/// Initialize <see cref="PortfolioRouteListFinishedMessage"/>.
		/// </summary>
		public PortfolioRouteListFinishedMessage()
			: base(MessageTypes.PortfolioRouteListFinished)
		{
		}
	}
}