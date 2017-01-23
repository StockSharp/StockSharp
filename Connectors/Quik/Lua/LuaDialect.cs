namespace StockSharp.Quik.Lua
{
	using System;

	using Ecng.Common;

	using StockSharp.Fix.Dialects;
	using StockSharp.Fix.Native;
	using StockSharp.Messages;

	class LuaDialect : DefaultDialect
	{
		public LuaDialect(Func<OrderCondition> createOrderCondition)
			: base(createOrderCondition)
		{
			TimeZone = TimeHelper.Moscow;

			SecuritiesSinglePacket = true;
		}

		/// <summary>
		/// Write the specified message into FIX protocol.
		/// </summary>
		/// <param name="writer">The recorder of data in the FIX protocol format.</param>
		/// <param name="message">The message.</param>
		/// <returns><see cref="FixMessages"/> value.</returns>
		protected override string OnWrite(IFixWriter writer, Message message)
		{
			var msgType = base.OnWrite(writer, message);

			var cancelMsg = message as OrderCancelMessage;

			if (cancelMsg == null)
				return msgType;

			if (cancelMsg.OrderType == null)
				return msgType;

			writer.Write(FixTags.Text);
			writer.Write(cancelMsg.OrderType.Value.To<string>());

			return msgType;
		}

		/// <summary>
		/// Записать данные по условию заявки.
		/// </summary>
		/// <param name="writer">Писатель FIX данных.</param>
		/// <param name="regMsg">Сообщение, содержащее информацию для регистрации заявки.</param>
		protected override void WriteOrderCondition(IFixWriter writer, OrderRegisterMessage regMsg)
		{
			writer.WriteOrderCondition((QuikOrderCondition)regMsg.Condition, TimeStampParser);
		}

		/// <summary>
		/// Прочитать условие заявки <see cref="OrderCondition"/>.
		/// </summary>
		/// <param name="reader">Читатель данных.</param>
		/// <param name="tag">Тэг.</param>
		/// <param name="getCondition">Функция, возвращающая условие заявки.</param>
		/// <returns>Успешно ли обработаны данные.</returns>
		protected override bool ReadOrderCondition(IFixReader reader, FixTags tag, Func<OrderCondition> getCondition)
		{
			return reader.ReadOrderCondition(tag, TimeZone, TimeStampParser, () => (QuikOrderCondition)getCondition());
		}
	}
}