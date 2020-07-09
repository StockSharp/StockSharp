namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Portfolio route response message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class PortfolioRouteMessage : BaseRouteMessage<PortfolioRouteMessage>, IPortfolioNameMessage
	{
		/// <summary>
		/// Initialize <see cref="PortfolioRouteMessage"/>.
		/// </summary>
		public PortfolioRouteMessage()
			: base(MessageTypes.PortfolioRoute)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.PortfolioRoute;

		/// <inheritdoc />
		[DataMember]
		public string PortfolioName { get; set; }

		/// <inheritdoc />
		public override void CopyTo(PortfolioRouteMessage destination)
		{
			base.CopyTo(destination);

			destination.PortfolioName = PortfolioName;
		}
	}
}