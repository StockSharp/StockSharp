#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: RepoOrderInfo.cs
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
    /// REPO info.
    /// </summary>
    [Serializable]
    [System.Runtime.Serialization.DataContract]
	public class RepoOrderInfo : Cloneable<RepoOrderInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepoOrderInfo"/>.
        /// </summary>
        public RepoOrderInfo()
        {
        }

		/// <summary>
		/// Partner-organization.
		/// </summary>
		[DataMember]
		public string Partner { get; set; }

		/// <summary>
		/// REPO expiration.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? Term { get; set; }

		/// <summary>
		/// Repo rate, in percentage.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? Rate { get; set; }

		/// <summary>
		/// Blocking code.
		/// </summary>
		[DataMember]
		[Nullable]
		public bool? BlockSecurities { get; set; }

		/// <summary>
		/// The rate of fixed compensation payable in the event that the second part of the repo, the percentage.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? RefundRate { get; set; }

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
		/// REPO second price part.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? SecondPrice { get; set; }

		/// <summary>
		/// Execution date OTC.
		/// </summary>
		[DataMember]
		[Nullable]
		public DateTimeOffset? SettleDate { get; set; }

		/// <summary>
		/// REPO-M the begin value of the discount.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? StartDiscount { get; set; }

		/// <summary>
		/// REPO-M the lower limit value of the discount.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? LowerDiscount { get; set; }

		/// <summary>
		/// REPO-M the upper limit value of the discount.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? UpperDiscount { get; set; }

		/// <summary>
		/// REPO-M volume.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? Value { get; set; }

		/// <summary>
		/// Create a copy of <see cref="RepoOrderInfo"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override RepoOrderInfo Clone()
		{
			return new RepoOrderInfo
			{
				MatchRef = MatchRef,
				Partner = Partner,
				SettleCode = SettleCode,
				SettleDate = SettleDate,
				Value = Value,
				BlockSecurities = BlockSecurities,
				LowerDiscount = LowerDiscount,
				Rate = Rate,
				RefundRate = RefundRate,
				SecondPrice = SecondPrice,
				StartDiscount = StartDiscount,
				Term = Term,
				UpperDiscount = UpperDiscount
			};
		}
    }
}
