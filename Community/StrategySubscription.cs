namespace StockSharp.Community
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// The strategy subscription.
	/// </summary>
	[DataContract]
	public class StrategySubscription
	{
		/// <summary>
		/// Identifier.
		/// </summary>
		[DataMember]
		public long Id { get; set; }

		/// <summary>
		/// Identifier of <see cref="StrategyData"/>.
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
	}
}