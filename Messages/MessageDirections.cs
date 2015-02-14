namespace StockSharp.Messages
{
	/// <summary>
	/// Направления сообщений.
	/// </summary>
	public enum MessageDirections
	{
		/// <summary>
		/// Входящее в <see cref="IMessageAdapter"/> сообщение.
		/// </summary>
		In,

		/// <summary>
		/// Исходящее из <see cref="IMessageAdapter"/> сообщение.
		/// </summary>
		Out
	}
}
