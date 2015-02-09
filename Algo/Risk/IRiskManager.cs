namespace StockSharp.Algo.Risk
{
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс, описывающий менеджера контроля рисков.
	/// </summary>
	public interface IRiskManager : IRiskRule
	{
		/// <summary>
		/// Список правил.
		/// </summary>
		SynchronizedSet<IRiskRule> Rules { get; }

		/// <summary>
		/// Обработать торговое сообщение.
		/// </summary>
		/// <param name="message">Торговое сообщение.</param>
		/// <returns>Список правил, которые были активированы сообщением.</returns>
		IEnumerable<IRiskRule> ProcessRules(Message message);
	}
}