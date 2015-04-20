namespace StockSharp.Algo.Testing
{
	using StockSharp.Messages;

	/// <summary>
	/// Сообщение о подписке или отписки на генератор маркет-данных.
	/// </summary>
	public class GeneratorMarketDataMessage : MarketDataMessage
	{
		/// <summary>
		/// Создать <see cref="GeneratorMarketDataMessage"/>.
		/// </summary>
		public GeneratorMarketDataMessage()
		{

		}

		/// <summary>
		/// Генератор маркет-данных.
		/// </summary>
		public MarketDataGenerator Generator { get; set; }
	}
}