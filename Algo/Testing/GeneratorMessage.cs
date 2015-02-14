namespace StockSharp.Algo.Testing
{
	using StockSharp.Messages;

	/// <summary>
	/// Сообщение о создании или удалении генератора маркет-данных.
	/// </summary>
	class GeneratorMessage : MarketDataMessage
	{
		/// <summary>
		/// Генератор маркет-данных.
		/// </summary>
		public MarketDataGenerator Generator { get; set; }

		/// <summary>
		/// Создать <see cref="GeneratorMessage"/>.
		/// </summary>
		public GeneratorMessage()
			: base(ExtendedMessageTypes.Generator)
		{
		}
	}
}