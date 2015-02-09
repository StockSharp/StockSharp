namespace StockSharp.Logging
{
	/// <summary>
	/// Интерфейс получателя логов.
	/// </summary>
	public interface ILogReceiver : ILogSource
	{
		/// <summary>
		/// Записать сообщение в лог.
		/// </summary>
		/// <param name="message">Отладочное сообщение.</param>
		void AddLog(LogMessage message);
	}

	/// <summary>
	/// Базовая реализация <see cref="ILogReceiver"/>.
	/// </summary>
	public abstract class BaseLogReceiver : BaseLogSource, ILogReceiver
	{
		/// <summary>
		/// Инициализировать <see cref="BaseLogReceiver"/>.
		/// </summary>
		protected BaseLogReceiver()
		{
		}

		void ILogReceiver.AddLog(LogMessage message)
		{
			RaiseLog(message);
		}
	}
}