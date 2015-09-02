namespace StockSharp.Messages
{
	/// <summary>
	/// Base interface for order book builder.
	/// </summary>
	public interface IOrderLogMarketDepthBuilder
	{
		/// <summary>
		/// Market depth.
		/// </summary>
		QuoteChangeMessage Depth { get; }

		/// <summary>
		/// Process order log item.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns>Order book was changed.</returns>
		bool Update(ExecutionMessage item);
	}
}