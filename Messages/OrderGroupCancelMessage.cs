namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// The message containing the order group cancel filter.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderGroupCancelMessage : OrderMessage
	{
		///// <summary>
		///// Тип инструмента. Если значение <see langword="null"/>, то отмена идет по всем типам инструментов.
		///// </summary>
		//[DataMember]
		//[DisplayName("Тип")]
		//[Description("Тип инструмента.")]
		//[MainCategory]
		//public SecurityTypes? SecurityType { get; set; }

		/// <summary>
		/// Order cancellation transaction id.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// <see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str226Key)]
		[DescriptionLoc(LocalizedStrings.Str227Key)]
		[MainCategory]
		public bool? IsStop { get; set; }

		/// <summary>
		/// Order side. If the value is <see langword="null" />, the direction does not use.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str228Key)]
		[MainCategory]
		public Sides? Side { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderGroupCancelMessage"/>.
		/// </summary>
		public OrderGroupCancelMessage()
			: base(MessageTypes.OrderGroupCancel)
		{
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",IsStop={0},Side={1},SecType={2}".Put(IsStop, Side, SecurityType);
		}

		/// <summary>
		/// Create a copy of <see cref="OrderGroupCancelMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderGroupCancelMessage
			{
				LocalTime = LocalTime,
				SecurityId = SecurityId,
				IsStop = IsStop,
				OrderType = OrderType,
				PortfolioName = PortfolioName,
				//SecurityType = SecurityType,
				Side = Side,
				TransactionId = TransactionId,
				ClientCode = ClientCode,
				BrokerCode = BrokerCode,
			};

			CopyTo(clone);

			return clone;
		}
	}
}