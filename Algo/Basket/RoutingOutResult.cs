namespace StockSharp.Algo.Basket;

/// <summary>
/// Result of processing an outgoing message from an inner adapter.
/// </summary>
public readonly struct RoutingOutResult
{
	/// <summary>
	/// Empty result (message was filtered out or handled internally).
	/// </summary>
	public static readonly RoutingOutResult Empty = new()
	{
		TransformedMessage = null,
		ExtraMessages = [],
		LoopbackMessages = [],
	};

	/// <summary>
	/// The transformed message to send to the outer handler.
	/// Can be null if the message was filtered or aggregated.
	/// </summary>
	public Message TransformedMessage { get; init; }

	/// <summary>
	/// Additional messages to send to the outer handler (e.g., ConnectMessage after all adapters connected).
	/// </summary>
	public IReadOnlyList<Message> ExtraMessages { get; init; }

	/// <summary>
	/// Messages to loop back to the basket for further processing.
	/// </summary>
	public IReadOnlyList<Message> LoopbackMessages { get; init; }

	/// <summary>
	/// Create a result that passes through the message unchanged.
	/// </summary>
	public static RoutingOutResult PassThrough(Message message) => new()
	{
		TransformedMessage = message,
		ExtraMessages = [],
		LoopbackMessages = [],
	};

	/// <summary>
	/// Create a result with a transformed message.
	/// </summary>
	public static RoutingOutResult WithMessage(Message message) => new()
	{
		TransformedMessage = message,
		ExtraMessages = [],
		LoopbackMessages = [],
	};

	/// <summary>
	/// Create a result with extra messages (no primary message).
	/// </summary>
	public static RoutingOutResult WithExtraMessages(IReadOnlyList<Message> messages) => new()
	{
		TransformedMessage = null,
		ExtraMessages = messages ?? [],
		LoopbackMessages = [],
	};

	/// <summary>
	/// Create a result with a loopback message for retry.
	/// </summary>
	public static RoutingOutResult WithLoopback(Message message) => new()
	{
		TransformedMessage = null,
		ExtraMessages = [],
		LoopbackMessages = [message],
	};

	/// <summary>
	/// Create a result with transformed message and extra messages.
	/// </summary>
	public static RoutingOutResult WithMessageAndExtras(Message message, IReadOnlyList<Message> extras) => new()
	{
		TransformedMessage = message,
		ExtraMessages = extras ?? [],
		LoopbackMessages = [],
	};
}
