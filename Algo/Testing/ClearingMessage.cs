namespace StockSharp.Algo.Testing
{
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Сообщение о проведении клиринга на бирже.
	/// </summary>
	class ClearingMessage : Message
	{
		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Проводить ли очистку стакана.
		/// </summary>
		public bool ClearMarketDepth { get; set; }

		/// <summary>
		/// Создать <see cref="ClearingMessage"/>.
		/// </summary>
		public ClearingMessage()
			: base(ExtendedMessageTypes.Clearing)
		{
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Sec={0}".Put(SecurityId);
		}
	}
}