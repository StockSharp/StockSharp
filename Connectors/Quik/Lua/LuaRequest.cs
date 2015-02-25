namespace StockSharp.Quik.Lua
{
	using Ecng.Common;

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

		/// <summary>
		/// Является ли сообщение подпиской на маркет-данные.
		/// </summary>
		public bool IsSubscribe { get; set; }

		/// <summary>
		/// Тип маркет-данных.
		/// </summary>
		public MarketDataTypes DataType { get; set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		public override string ToString()
		{
			return "Type = {0} TrId = {1} Value = {2} SecId = {3} OrdType = {4} IsSubscribe = {5} DataType = {6}"
				.Put(MessageType, TransactionId, Value, SecurityId, OrderType, IsSubscribe, DataType);
		}
	}
}