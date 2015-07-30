namespace StockSharp.Quik.Lua
{
	using System;

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
			this.RemoveMarketDataSupport();
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено <see langword="null"/>.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new QuikOrderCondition();
		}

		/// <summary>
		/// Финальная инициализация условие заявки.
		/// </summary>
		/// <param name="ordType">Тип заявки.</param>
		/// <param name="condition">Условие заявки.</param>
		protected override void PostInitCondition(char ordType, OrderCondition condition)
		{
		}

		/// <summary>
		/// Записать данные по условию заявки.
		/// </summary>
		/// <param name="writer">Писатель FIX данных.</param>
		/// <param name="regMsg">Сообщение, содержащее информацию для регистрации заявки.</param>
		protected override void WriteOrderCondition(IFixWriter writer, OrderRegisterMessage regMsg)
		{
			writer.WriteOrderCondition((QuikOrderCondition)regMsg.Condition, DateTimeFormat);
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
			return reader.ReadOrderCondition(tag, UtcOffset, DateTimeFormat, () => (QuikOrderCondition)getCondition());
		}
	}
}