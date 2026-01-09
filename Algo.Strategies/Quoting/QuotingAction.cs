namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// Recommended action types.
/// </summary>
public enum QuotingActionType
{
	/// <summary>
	/// No action needed.
	/// </summary>
	None,

	/// <summary>
	/// Register new order.
	/// </summary>
	Register,

	/// <summary>
	/// Cancel current order.
	/// </summary>
	Cancel,

	/// <summary>
	/// Modify current order (cancel and register new).
	/// </summary>
	Modify,

	/// <summary>
	/// Finish quoting (target reached or timeout).
	/// </summary>
	Finish
}

/// <summary>
/// Recommended action output from the engine.
/// </summary>
public class QuotingAction
{
	/// <summary>
	/// Type of action.
	/// </summary>
	public QuotingActionType ActionType { get; set; }

	/// <summary>
	/// Recommended price for new order (null for market orders).
	/// </summary>
	public decimal? Price { get; set; }

	/// <summary>
	/// Recommended volume for new order.
	/// </summary>
	public decimal? Volume { get; set; }

	/// <summary>
	/// Order type for new order.
	/// </summary>
	public OrderTypes? OrderType { get; set; }

	/// <summary>
	/// Reason for the action (for logging).
	/// </summary>
	public string Reason { get; set; }

	/// <summary>
	/// Whether the quoting was successful (for Finish action).
	/// </summary>
	public bool IsSuccess { get; set; }

	/// <summary>
	/// Create "no action" result.
	/// </summary>
	public static QuotingAction None(string reason = null) => new()
	{
		ActionType = QuotingActionType.None,
		Reason = reason
	};

	/// <summary>
	/// Create "register order" result.
	/// </summary>
	public static QuotingAction Register(decimal? price, decimal volume, OrderTypes orderType = OrderTypes.Limit, string reason = null) => new()
	{
		ActionType = QuotingActionType.Register,
		Price = price,
		Volume = volume,
		OrderType = orderType,
		Reason = reason
	};

	/// <summary>
	/// Create "cancel order" result.
	/// </summary>
	public static QuotingAction Cancel(string reason = null) => new()
	{
		ActionType = QuotingActionType.Cancel,
		Reason = reason
	};

	/// <summary>
	/// Create "modify order" result.
	/// </summary>
	public static QuotingAction Modify(decimal? price, decimal volume, OrderTypes orderType = OrderTypes.Limit, string reason = null) => new()
	{
		ActionType = QuotingActionType.Modify,
		Price = price,
		Volume = volume,
		OrderType = orderType,
		Reason = reason
	};

	/// <summary>
	/// Create "finish quoting" result.
	/// </summary>
	public static QuotingAction Finish(bool isSuccess, string reason = null) => new()
	{
		ActionType = QuotingActionType.Finish,
		IsSuccess = isSuccess,
		Reason = reason
	};
}
