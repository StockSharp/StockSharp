namespace StockSharp.Messages;

/// <summary>
/// Stop order lifecycle states.
/// </summary>
/// <remarks>
/// Distinct from <see cref="OrderStates"/>: a stop waits before it becomes an order at all, so it
/// carries a <see cref="Triggered"/> step of its own and a <see cref="Cancelled"/> end that is not
/// the failure an executed order reaches.
/// </remarks>
[DataContract]
[Serializable]
public enum StopOrderStates
{
	/// <summary>
	/// Waiting for its price.
	/// </summary>
	[EnumMember]
	Active,

	/// <summary>
	/// Its price came and the order it guards was placed.
	/// </summary>
	[EnumMember]
	Triggered,

	/// <summary>
	/// Taken back before it fired.
	/// </summary>
	[EnumMember]
	Cancelled,
}

/// <summary>
/// What a stop order is for, which is also how the price that fires it is read.
/// </summary>
/// <remarks>
/// The same two cases <see cref="IStopLossOrderCondition"/> and <see cref="ITakeProfitOrderCondition"/>
/// describe, as a value - for the places that store or transmit which of the two a stop is, rather
/// than acting on its condition.
/// </remarks>
[DataContract]
[Serializable]
public enum StopOrderConditionTypes
{
	/// <summary>
	/// Fires when the price falls to the level, to cap a loss.
	/// </summary>
	[EnumMember]
	StopLoss,

	/// <summary>
	/// Fires when the price rises to the level, to bank a gain.
	/// </summary>
	[EnumMember]
	TakeProfit,
}
