namespace StockSharp.Messages
{
	using System.Collections.Generic;

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
		/// Process message.
		/// </summary>
		/// <param name="message">Input message.</param>
		/// <returns>Output messages.</returns>
		IEnumerable<Message> Process(Message message);
	}
}