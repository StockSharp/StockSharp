namespace StockSharp.Algo.Latency
{
	using System;

	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс менеджера расчета задержки регистрации заявок.
	/// </summary>
	public interface ILatencyManager
	{
		/// <summary>
		/// Обнулить расчеты.
		/// </summary>
		void Reset();

		/// <summary>
		/// Суммарное значение задержки регистрации по всем заявкам.
		/// </summary>
		TimeSpan LatencyRegistration { get; }

		/// <summary>
		/// Суммарное значение задержки отмены по всем заявкам.
		/// </summary>
		TimeSpan LatencyCancellation { get; }

		/// <summary>
		/// Обработать сообщение для вычисления задержки транзакции. Принимаются сообщения типа
		/// <see cref="OrderRegisterMessage"/>, <see cref="OrderReplaceMessage"/>,
		/// <see cref="OrderCancelMessage"/> и <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Задержка транзакции. В случае невозможности вычислить задержку будет возвращено <see langword="null"/>.</returns>
		TimeSpan? ProcessMessage(Message message);
	}
}