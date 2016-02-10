namespace StockSharp.Quik.Lua
{
	using System;
	using System.IO;
	using System.Security;
	using System.Text;

	using Ecng.Common;

	using StockSharp.Fix.Dialects;
	using StockSharp.Fix.Native;
	using StockSharp.Messages;

	class QuikLuaDialect : DefaultDialect
	{
		public QuikLuaDialect(string senderCompId, string targetCompId, Stream stream, Encoding encoding, IncrementalIdGenerator idGenerator, TimeSpan heartbeatInterval, bool isResetCounter, string login, SecureString password, Func<OrderCondition> createOrderCondition)
			: base(senderCompId, targetCompId, stream, encoding, idGenerator, heartbeatInterval, isResetCounter, login, password, TimeHelper.Moscow, createOrderCondition)
		{
		}

		/// <summary>
		/// Записать данные по условию заявки.
		/// </summary>
		/// <param name="writer">Писатель FIX данных.</param>
		/// <param name="regMsg">Сообщение, содержащее информацию для регистрации заявки.</param>
		protected override void WriteOrderCondition(IFixWriter writer, OrderRegisterMessage regMsg)
		{
			writer.WriteOrderCondition((QuikOrderCondition)regMsg.Condition, TimeStampFormat);
		}

		/// <summary>
		/// Прочитать условие регистрации заявки <see cref="OrderRegisterMessage.Condition"/>.
		/// </summary>
		/// <param name="reader">Читатель данных.</param>
		/// <param name="tag">Тэг.</param>
		/// <param name="getCondition">Функция, возвращающая условие заявки.</param>
		/// <returns>Успешно ли обработаны данные.</returns>
		protected override bool ReadOrderCondition(IFixReader reader, FixTags tag, Func<OrderCondition> getCondition)
		{
			return reader.ReadOrderCondition(tag, TimeZone, TimeStampFormat, () => (QuikOrderCondition)getCondition());
		}
	}
}