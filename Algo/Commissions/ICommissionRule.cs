namespace StockSharp.Algo.Commissions
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Интерфейс правила вычисления комиссии.
	/// </summary>
	public interface ICommissionRule : IPersistable
	{
		/// <summary>
		/// Заголовок.
		/// </summary>
		string Title { get; }

		/// <summary>
		/// Суммарное значение комиссии.
		/// </summary>
		decimal Commission { get; }

		/// <summary>
		/// Значение комиссии.
		/// </summary>
		Unit Value { get; }

		/// <summary>
		/// Сбросить состояние.
		/// </summary>
		void Reset();

		/// <summary>
		/// Рассчитать комиссию.
		/// </summary>
		/// <param name="message">Сообщение, содержащее информацию по заявке или собственной сделке.</param>
		/// <returns>Комиссия. Если комиссию рассчитать невозможно, то будет возвращено <see langword="null"/>.</returns>
		decimal? ProcessExecution(ExecutionMessage message);
	}
}