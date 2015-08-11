namespace StockSharp.Messages
{
	/// <summary>
	/// Интерфейс, описывающий построитель стакана из лога заявок.
	/// </summary>
	public interface IOrderLogMarketDepthBuilder
	{
		/// <summary>
		/// Стакан.
		/// </summary>
		QuoteChangeMessage Depth { get; }

		/// <summary>
		/// Добавить новую строчку из лога заявок к стакану.
		/// </summary>
		/// <param name="item">Строчка лога заявок.</param>
		/// <returns>Был ли изменен стакан.</returns>
		bool Update(ExecutionMessage item);
	}
}