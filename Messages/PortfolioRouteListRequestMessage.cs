namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Portfolio routes list request message.
	/// </summary>
	[Serializable]
	[DataContract]
	public class PortfolioRouteListRequestMessage : BaseRequestMessage
	{
		/// <summary>
		/// Initialize <see cref="PortfolioRouteListRequestMessage"/>.
		/// </summary>
		public PortfolioRouteListRequestMessage()
			: base(MessageTypes.PortfolioRouteListRequest)
		{
		}

		/// <inheritdoc />
		public override DataType DataType => DataType.PortfolioRoute;

		/// <summary>
		/// Create a copy of <see cref="PortfolioRouteListRequestMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new PortfolioRouteListRequestMessage();

			CopyTo(clone);

			return clone;
		}
	}
}