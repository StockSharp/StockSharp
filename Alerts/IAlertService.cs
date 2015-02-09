namespace StockSharp.Alerts
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// Типы сигналов.
	/// </summary>
	public enum AlertTypes
	{
		/// <summary>
		/// Звуковой.
		/// </summary>
		Sound,
		
		/// <summary>
		/// Речевой.
		/// </summary>
		Speech,

		/// <summary>
		/// Всплывающим окном.
		/// </summary>
		Popup,

		/// <summary>
		/// SMS.
		/// </summary>
		Sms,

		/// <summary>
		/// Email.
		/// </summary>
		Email,

		/// <summary>
		/// Логом.
		/// </summary>
		Log,
	}

	/// <summary>
	/// Интерфейс, описывающий сервис сигналов.
	/// </summary>
	public interface IAlertService
	{
		/// <summary>
		/// Добавить сигнал на вывод.
		/// </summary>
		/// <param name="type">Тип сигнала.</param>
		/// <param name="caption">Заголовок сигнала.</param>
		/// <param name="message">Текст сигнала.</param>
		/// <param name="time">Время формирования.</param>
		void PushAlert(AlertTypes type, string caption, string message, DateTime time);

		/// <summary>
		/// Зарегистрировать схему.
		/// </summary>
		/// <param name="schema">Схема.</param>
		void Register(AlertSchema schema);

		/// <summary>
		/// Удалить ранее зарегистрированную через <see cref="Register"/> схему.
		/// </summary>
		/// <param name="schema">Схема.</param>
		void UnRegister(AlertSchema schema);

		/// <summary>
		/// Проверить сообщение на активацию сигнала.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		void Process(Message message);
	}
}