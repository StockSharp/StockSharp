#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: RpsOrderInfo.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		/// Currency code in ISO 4217 standard (OTC trade). Non-system trade parameter
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
