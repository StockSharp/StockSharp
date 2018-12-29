namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Runtime.Serialization;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// The interface describing withdraw funds condition.
	/// </summary>
	public interface IWithdrawOrderCondition
	{
		/// <summary>
		/// Withdraw.
		/// </summary>
		bool IsWithdraw { get; set; }

		/// <summary>
		/// Withdraw info.
		/// </summary>
		WithdrawInfo WithdrawInfo { get; set; }
	}

	/// <summary>
	/// The base implementation <see cref="IWithdrawOrderCondition"/>.
	/// </summary>
	[Serializable]
	[DataContract]
	public abstract class BaseWithdrawOrderCondition : OrderCondition, IWithdrawOrderCondition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BaseWithdrawOrderCondition"/>.
		/// </summary>
		protected BaseWithdrawOrderCondition()
		{
			WithdrawInfo = new WithdrawInfo(); 
		}

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.WithdrawKey,
			Description = LocalizedStrings.WithdrawKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 10)]
		public bool IsWithdraw
		{
			get => (bool?)Parameters.TryGetValue(nameof(IsWithdraw)) ?? false;
			set => Parameters[nameof(IsWithdraw)] = value;
		}

		/// <inheritdoc />
		[DataMember]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.WithdrawInfoKey,
			Description = LocalizedStrings.WithdrawInfoKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.WithdrawKey,
			Order = 11)]
		public WithdrawInfo WithdrawInfo
		{
			get => (WithdrawInfo)Parameters[nameof(WithdrawInfo)];
			set => Parameters[nameof(WithdrawInfo)] = value;
		}
	}

	/// <summary>
	/// The interface describing take-profit order condition.
	/// </summary>
	public interface ITakeProfitOrderCondition
	{
		/// <summary>
		/// Close position price. <see langword="null"/> means close by market.
		/// </summary>
		decimal? ClosePositionPrice { get; set; }

		/// <summary>
		/// The absolute value of the price when the one is reached the protective strategy is activated.
		/// </summary>
		decimal? ActivationPrice { get; set; }

		/// <summary>
		/// Trailing take-profit.
		/// </summary>
		bool IsTrailing { get; set; }
	}

	/// <summary>
	/// The interface describing stop-loss order condition.
	/// </summary>
	public interface IStopLossOrderCondition
	{
		/// <summary>
		/// Close position price. <see langword="null"/> means close by market.
		/// </summary>
		decimal? ClosePositionPrice { get; set; }

		/// <summary>
		/// The absolute value of the price when the one is reached the protective strategy is activated.
		/// </summary>
		decimal? ActivationPrice { get; set; }

		/// <summary>
		/// Trailing stop-loss.
		/// </summary>
		bool IsTrailing { get; set; }
	}

	/// <summary>
	/// Extended interface for specify extra operation with conditional orders.
	/// </summary>
	public interface IMessageAdapterExtension
	{
		/// <summary>
		/// Determines whether the adapter support stop-loss orders.
		/// </summary>
		bool IsSupportStopLoss { get; }

		/// <summary>
		/// Determines whether the adapter support take-profit orders.
		/// </summary>
		bool IsSupportTakeProfit { get; }

		/// <summary>
		/// Determines whether the adapter support withdraw orders.
		/// </summary>
		bool IsSupportWithdraw { get; }

		///// <summary>
		///// Create stop-loss order condition.
		///// </summary>
		///// <returns>Order condition. If the connection does not support the order type, the exception <see cref="NotSupportedException" /> will be thrown.</returns>
		//IStopLossOrderCondition CreateStopLossCondition();

		///// <summary>
		///// Create take-profit order condition.
		///// </summary>
		///// <returns>Order condition. If the connection does not support the order type, the exception <see cref="NotSupportedException" /> will be thrown.</returns>
		//ITakeProfitOrderCondition CreateTakeProfitCondition();

		///// <summary>
		///// Create withdraw order condition.
		///// </summary>
		///// <returns>Order condition. If the connection does not support the order type, the exception <see cref="NotSupportedException" /> will be thrown.</returns>
		//IWithdrawOrderCondition CreateWithdrawCondition();
	}
}