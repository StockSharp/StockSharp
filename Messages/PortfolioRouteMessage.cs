namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Portfolio route response message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class PortfolioRouteMessage : BaseRouteMessage<PortfolioRouteMessage>
	{
		/// <summary>
		/// Initialize <see cref="PortfolioRouteMessage"/>.
		/// </summary>
		public PortfolioRouteMessage()
			: base(MessageTypes.PortfolioRoute)
		{
		}

		/// <summary>
		/// Portfolio.
		/// </summary>
		[DataMember]
		public string PortfolioName { get; set; }

		/// <inheritdoc />
		protected override void CopyTo(PortfolioRouteMessage destination)
		{
			base.CopyTo(destination);

			destination.PortfolioName = PortfolioName;
		}
	}
}