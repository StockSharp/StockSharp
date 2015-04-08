namespace StockSharp.Messages
{
	/// <summary>
	/// Состояния подключений.
	/// </summary>
	public enum ConnectionStates
	{
		/// <summary>
		/// Не активно.
		/// </summary>
		Disconnected,

		/// <summary>
		/// В процессе отключения.
		/// </summary>
		Disconnecting,

		/// <summary>
		/// В процессе подключения.
		/// </summary>
		Connecting,

		/// <summary>
		/// Подключение активно.
		/// </summary>
		Connected,

		/// <summary>
		/// Ошибка подключения.
		/// </summary>
		Failed,
	}
}
