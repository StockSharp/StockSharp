namespace StockSharp.Quik.Lua
{
	using StockSharp.Messages;

	/// <summary>
	/// Пользовательский запрос.
	/// </summary>
	public class LuaRequest
	{
		/// <summary>
		/// Создать <see cref="LuaRequest"/>.
		/// </summary>
		public LuaRequest()
		{
		}

		/// <summary>
		/// Тип сообщения.
		/// </summary>
		public MessageTypes MessageType { get; set; }

		/// <summary>
		/// Номер транзакции.
		/// </summary>
		public long TransactionId { get; set; }

		/// <summary>
		/// Значение запроса.
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		public SecurityId? SecurityId { get; set; }

		/// <summary>
		/// Тип заявки.
		/// </summary>
		public OrderTypes? OrderType { get; set; }
	}
}