namespace StockSharp.Algo.Strategies.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// The strategy subscription.
	/// </summary>
	[DataContract]
	[Serializable]
	public class StrategySubscriptionInfoMessage : Message
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Identifier of <see cref="StrategyInfoMessage"/>.
		/// </summary>
		[DataMember]
		public long StrategyId { get; set; }

		/// <summary>
		/// Start time.
		/// </summary>
		[DataMember]
		public DateTime Start { get; set; }

		/// <summary>
		/// End time.
		/// </summary>
		[DataMember]
		public DateTime End { get; set; }

		/// <summary>
		/// Amount of the subscription.
		/// </summary>
		[DataMember]
		public decimal Amount { get; set; }

		/// <summary>
		/// Hardware id of the computer for which the license is issued.
		/// </summary>
		[DataMember]
		public string HardwareId { get; set; }

		/// <summary>
		/// The account number for which the license is issued.
		/// </summary>
		[DataMember]
		public string Account { get; set; }

		/// <summary>
		/// Is auto renewable subscription.
		/// </summary>
		[DataMember]
		public bool IsAutoRenew { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategySubscriptionInfoMessage"/>.
		/// </summary>
		public StrategySubscriptionInfoMessage()
			: base(ExtendedMessageTypes.StrategySubscriptionInfo)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="StrategySubscriptionInfoMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new StrategySubscriptionInfoMessage
			{
				Id = Id,
				StrategyId = StrategyId,
				Start = Start,
				End = End,
				Amount = Amount,
				HardwareId = HardwareId,
				Account = Account,
				IsAutoRenew = IsAutoRenew,
			};

			CopyTo(clone);

			return clone;
		}
	}
}