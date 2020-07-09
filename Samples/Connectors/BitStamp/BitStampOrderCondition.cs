namespace StockSharp.BitStamp
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// <see cref="BitStamp"/> order condition.
	/// </summary>
	[Serializable]
	[DataContract]
	[DisplayNameLoc(LocalizedStrings.Str2264Key, LocalizedStrings.BitStampKey)]
	public class BitStampOrderCondition : BaseWithdrawOrderCondition, IStopLossOrderCondition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BitStampOrderCondition"/>.
		/// </summary>
		public BitStampOrderCondition()
		{
		}

		/// <summary>
		/// Activation price, when reached an order will be placed.
		/// </summary>
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str2455Key,
			Description = LocalizedStrings.Str3460Key,
			GroupName = LocalizedStrings.StopLossKey,
			Order = 0)]
		public decimal? StopPrice
		{
			get => (decimal?)Parameters.TryGetValue(nameof(StopPrice));
			set => Parameters[nameof(StopPrice)] = value;
		}

		decimal? IStopLossOrderCondition.ClosePositionPrice
		{
			get => null;
			set { }
		}
		decimal? IStopLossOrderCondition.ActivationPrice
		{
			get => StopPrice;
			set => StopPrice = value;
		}
		bool IStopLossOrderCondition.IsTrailing
		{
			get => false;
			set { }
		}
	}
}