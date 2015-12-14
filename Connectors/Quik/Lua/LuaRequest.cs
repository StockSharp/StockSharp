#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.Lua.QuikPublic
File: LuaRequest.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		/// Идентификатор транзакции.
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
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "Type = {0} TrId = {1} Value = {2} SecId = {3} OrdType = {4} IsSubscribe = {5} DataType = {6}"
				.Put(MessageType, TransactionId, Value, SecurityId, OrderType, IsSubscribe, DataType);
		}
	}
}