namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	
	using StockSharp.Localization;

	/// <summary>
	/// A message containing info about the order.
	/// </summary>
	[DataContract]
	[Serializable]
	public abstract class OrderMessage : SecurityMessage
	{
		/// <summary>
		/// Portfolio name, for which an order must be placed/cancelled.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str229Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Order type.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str132Key)]
		[DescriptionLoc(LocalizedStrings.Str133Key)]
		[MainCategory]
		public OrderTypes OrderType { get; set; }

		/// <summary>
		/// User's order ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str165Key)]
		[DescriptionLoc(LocalizedStrings.Str166Key)]
		[MainCategory]
		public string UserOrderId { get; set; }

		/// <summary>
		/// Broker firm code.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str2593Key)]
		[DisplayNameLoc(LocalizedStrings.BrokerKey)]
		[DescriptionLoc(LocalizedStrings.Str2619Key)]
		public string BrokerCode { get; set; }

		/// <summary>
		/// Client code assigned by the broker.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str2593Key)]
		[DisplayNameLoc(LocalizedStrings.ClientCodeKey)]
		[DescriptionLoc(LocalizedStrings.ClientCodeDescKey)]
		public string ClientCode { get; set; }

		/// <summary>
		/// Initialize <see cref="OrderMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected OrderMessage(MessageTypes type)
			: base(type)
		{
		}
	}
}