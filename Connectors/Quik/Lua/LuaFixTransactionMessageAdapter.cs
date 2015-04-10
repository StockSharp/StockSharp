namespace StockSharp.Quik.Lua
{
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Fix;
	using StockSharp.Fix.Native;
	using StockSharp.Messages;

	[DisplayName("LuaFixTransactionMessageAdapter")]
	class LuaFixTransactionMessageAdapter : FixMessageAdapter
	{
		public LuaFixTransactionMessageAdapter(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
		}

		/// <summary>
		/// <see langword="true"/>, если сессия используется для получения маркет-данных, иначе, <see langword="false"/>.
		/// </summary>
		public override bool IsMarketDataEnabled
		{
			get { return false; }
		}

		/// <summary>
		/// <see langword="true"/>, если сессия используется для отправки транзакций, иначе, <see langword="false"/>.
		/// </summary>
		public override bool IsTransactionEnabled
		{
			get { return true; }
		}

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