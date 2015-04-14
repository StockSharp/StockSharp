namespace StockSharp.Quik.Lua
{
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Fix;
	using StockSharp.Fix.Native;
	using StockSharp.Messages;

	/// <summary>
	/// Адаптер сообщений для Quik LUA FIX.
	/// </summary>
	public class LuaFixTransactionMessageAdapter : FixMessageAdapter
	{
		/// <summary>
		/// Создать <see cref="LuaFixTransactionMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public LuaFixTransactionMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			IsMarketDataEnabled = false;
		}

		/// <summary>
		/// Записать данные по условию заявки.
		/// </summary>
		/// <param name="writer">Писатель FIX данных.</param>
		/// <param name="regMsg">Сообщение, содержащее информацию для регистрации заявки.</param>
		protected override void WriteFixOrderCondition(IFixWriter writer, OrderRegisterMessage regMsg)
		{
			writer.WriteOrderCondition((QuikOrderCondition)regMsg.Condition);
		}

		/// <summary>
		/// Метод вызывается при обработке полученного сообщения.
		/// </summary>
		/// <param name="msgType">Тип FIX сообщения.</param>
		/// <param name="reader">Читатель данных, записанных в формате FIX протокола.</param>
		/// <returns>Успешно ли обработаны данные.</returns>
		protected override bool? ProcessTransactionMessage(string msgType, IFixReader reader)
		{
			switch (msgType)
			{
				case QuikFixMessages.StopOrderExecutionReport:
				{
					var condition = new QuikOrderCondition();

					var executions = reader.ReadExecutionMessage(this,
						tag => reader.ReadOrderCondition(tag, UtcOffset, condition));

					if (executions == null)
						return null;

					var exec = executions.First();
					exec.Condition = condition;

					SendOutMessage(exec);

					return true;
				}
			}

			return base.ProcessTransactionMessage(msgType, reader);
		}
	}
}