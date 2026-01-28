namespace StockSharp.Algo.Basket;

/// <summary>
/// Result of processing an incoming message for routing.
/// </summary>
public readonly struct RoutingInResult
{
	/// <summary>
	/// Empty result.
	/// </summary>
	public static readonly RoutingInResult Empty = new()
	{
		RoutingDecisions = [],
		OutMessages = [],
		LoopbackMessages = [],
		Handled = false,
		IsPended = false,
	};

	/// <summary>
	/// Routing decisions: which adapter should receive which message.
	/// </summary>
	public IReadOnlyList<(IMessageAdapter Adapter, Message Message)> RoutingDecisions { get; init; }

	/// <summary>
	/// Messages to send to the outer handler (e.g., SubscriptionResponse, error messages).
	/// </summary>
	public IReadOnlyList<Message> OutMessages { get; init; }

	/// <summary>
	/// Messages to loop back to the basket (e.g., pending messages after connect).
	/// </summary>
	public IReadOnlyList<Message> LoopbackMessages { get; init; }

	/// <summary>
	/// Whether the message was fully handled (no further processing needed).
	/// </summary>
	public bool Handled { get; init; }

	/// <summary>
	/// Whether the message was pended (waiting for connection).
	/// </summary>
	public bool IsPended { get; init; }

	/// <summary>
	/// Create a handled result with no routing (message was fully processed).
	/// </summary>
	public static RoutingInResult CreateHandled() => new()
	{
		RoutingDecisions = [],
		OutMessages = [],
		LoopbackMessages = [],
		Handled = true,
		IsPended = false,
	};

	/// <summary>
	/// Create a pended result (message was queued for later processing).
	/// </summary>
	public static RoutingInResult Pended() => new()
	{
		RoutingDecisions = [],
		OutMessages = [],
		LoopbackMessages = [],
		Handled = true,
		IsPended = true,
	};

	/// <summary>
	/// Create a result with routing decisions.
	/// </summary>
	public static RoutingInResult WithRouting(IReadOnlyList<(IMessageAdapter Adapter, Message Message)> decisions)
		=> new()
		{
			RoutingDecisions = decisions ?? [],
			OutMessages = [],
			LoopbackMessages = [],
			Handled = true,
			IsPended = false,
		};

	/// <summary>
	/// Create a result with an output message (e.g., not supported response).
	/// </summary>
	public static RoutingInResult WithOutMessage(Message message) => new()
	{
		RoutingDecisions = [],
		OutMessages = [message],
		LoopbackMessages = [],
		Handled = true,
		IsPended = false,
	};
}
