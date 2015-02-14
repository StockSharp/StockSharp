namespace StockSharp.Algo.Slippage
{
	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс для менеджера расчета проскальзывания.
	/// </summary>
	public interface ISlippageManager
	{
		/// <summary>
		/// Суммарное значение проскальзывания.
		/// </summary>
		decimal Slippage { get; }

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		void Reset();

		/// <summary>
		/// Рассчитать проскальзывание.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		/// <returns>Проскальзывание. Если проскальзывание рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		decimal? ProcessMessage(Message message);
	}
}