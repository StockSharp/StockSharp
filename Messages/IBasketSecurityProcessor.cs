namespace StockSharp.Messages;

/// <summary>
/// The interface of market data processor for basket securities.
/// </summary>
public interface IBasketSecurityProcessor
{
	/// <summary>
	/// Security ID.
	/// </summary>
	SecurityId SecurityId { get; }

	/// <summary>
	/// Basket security expression.
	/// </summary>
	string BasketExpression { get; }

	/// <summary>
	/// Basket security legs.
	/// </summary>
	SecurityId[] BasketLegs { get; }

	/// <summary>
	/// Prepare the processor for <see cref="Process"/>: whatever it needs that has to be waited for.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	ValueTask InitAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Process message.
	/// </summary>
	/// <param name="message">Input message.</param>
	/// <returns>Output messages.</returns>
	IEnumerable<Message> Process(Message message);
}