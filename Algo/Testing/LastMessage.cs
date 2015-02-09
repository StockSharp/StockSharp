namespace StockSharp.Algo.Testing
{
	using StockSharp.Messages;

	/// <summary>
	/// Сообщение, информирующее об окончании поступления данных.
	/// </summary>
	class LastMessage : Message
	{
		/// <summary>
		/// Передача данных завершена из-за ошибки.
		/// </summary>
		public bool IsError { get; set; }

		/// <summary>
		/// Создать <see cref="LastMessage"/>.
		/// </summary>
		public LastMessage()
			: base(ExtendedMessageTypes.Last)
		{
		}
	}
}