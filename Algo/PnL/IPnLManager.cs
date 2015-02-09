namespace StockSharp.Algo.PnL
{
	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс менеджера расчета прибыли-убытка.
	/// </summary>
	public interface IPnLManager
	{
		/// <summary>
		/// Суммарное значение прибыли-убытка.
		/// </summary>
		decimal PnL { get; }

		/// <summary>
		/// Значение реализованной прибыли-убытка.
		/// </summary>
		decimal RealizedPnL { get; }

		/// <summary>
		/// Значение нереализованной прибыли-убытка.
		/// </summary>
		decimal UnrealizedPnL { get; }

		/// <summary>
		/// Обнулить <see cref="PnL"/>.
		/// </summary>
		void Reset();

		/// <summary>
		/// Рассчитать прибыльность сделки. Если сделка уже ранее была обработана, то возвращается предыдущая информация.
		/// </summary>
		/// <param name="trade">Сделка.</param>
		/// <returns>Информация о новой сделке.</returns>
		PnLInfo ProcessMyTrade(ExecutionMessage trade);

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		void ProcessMessage(Message message);
	}
}