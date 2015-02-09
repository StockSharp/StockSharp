namespace StockSharp.Logging
{
	using System;

	using Ecng.Common;

	/// <summary>
	/// Отладочное сообщение.
	/// </summary>
	public class LogMessage
	{
		internal bool IsDispose;

		private Func<string> _getMessage;

		/// <summary>
		/// Создать <see cref="LogMessage"/>.
		/// </summary>
		/// <param name="source">Источник логов.</param>
		/// <param name="time">Время создания сообщения.</param>
		/// <param name="level">Уровень лог-сообщения.</param>
		/// <param name="message">Текстовое сообщение.</param>
		/// <param name="args">Параметры текстового сообщения.
		/// Используются в случае, если message является форматирующей строкой.
		/// Подробнее, <see cref="string.Format(string,object[])"/>.</param>
		public LogMessage(ILogSource source, DateTimeOffset time, LogLevels level, string message, params object[] args)
			: this(source, time, level, () => message.Put(args))
		{
		}

		/// <summary>
		/// Создать <see cref="LogMessage"/>.
		/// </summary>
		/// <param name="source">Источник логов.</param>
		/// <param name="time">Время создания сообщения.</param>
		/// <param name="level">Уровень лог-сообщения.</param>
		/// <param name="getMessage">Функция, возвращающая текст для <see cref="LogMessage.Message"/>.</param>
		public LogMessage(ILogSource source, DateTimeOffset time, LogLevels level, Func<string> getMessage)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			if (getMessage == null)
				throw new ArgumentNullException("getMessage");

			_getMessage = getMessage;

			Source = source;
			Time = time;
			Level = level;
		}

		/// <summary>
		/// Источник логов.
		/// </summary>
		public ILogSource Source { get; private set; }

		/// <summary>
		/// Время создания сообщения.
		/// </summary>
		public DateTimeOffset Time { get; private set; }

		/// <summary>
		/// Уровень лог-сообщения.
		/// </summary>
		public LogLevels Level { get; private set; }

		private string _message;

		/// <summary>
		/// Сообщение.
		/// </summary>
		public string Message
		{
			get
			{
				if (_message != null)
					return _message;

				_message = _getMessage();

				// делегат может захватить из внешнего кода лишние данные, что не будут удаляться GC
				// в случае, если LogMessage будет храниться где-то (например, в LogControl)
				_getMessage = null;

				return _message;
			}
		}

		/// <summary>
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{0} {1}".Put(Time, Message);
		}
	}
}