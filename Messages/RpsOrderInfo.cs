namespace StockSharp.Messages
{
    using System;
    using System.Runtime.Serialization;

    using Ecng.Common;
    using Ecng.Serialization;

    /// <summary>
    /// RPS order info.
    /// </summary>
    [Serializable]
    [System.Runtime.Serialization.DataContract]
	public class RpsOrderInfo : Cloneable<RpsOrderInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RpsOrderInfo"/>.
        /// </summary>
        public RpsOrderInfo()
        {
        }

		/// <summary>
		/// Partner-organization.
		/// </summary>
		[DataMember]
		public string Partner { get; set; }

		/// <summary>
		/// Execution date OTC.
		/// </summary>
		[DataMember]
		[Nullable]
		public DateTimeOffset? SettleDate { get; set; }

		/// <summary>
		/// REPO RPS reference.
		/// </summary>
		[DataMember]
		public string MatchRef { get; set; }

		/// <summary>
		/// Settlement code.
		/// </summary>
		[DataMember]
		public string SettleCode { get; set; }

		/// <summary>
		/// Owner of transaction (OTC trade).
		/// </summary>
		[DataMember]
		public string ForAccount { get; set; }

		/// <summary>
		/// Currency code in ISO 4217 standard (OTC trade). Параметр внебиржевой сделки.
		/// </summary>
		[DataMember]
		public CurrencyTypes CurrencyType { get; set; }

		/// <summary>
		/// Create a copy of <see cref="RpsOrderInfo"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override RpsOrderInfo Clone()
		{
			return new RpsOrderInfo
			{
				CurrencyType = CurrencyType,
				ForAccount = ForAccount,
				MatchRef = MatchRef,
				Partner = Partner,
				SettleCode = SettleCode,
				SettleDate = SettleDate
			};
		}
    }
}
