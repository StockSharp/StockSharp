namespace StockSharp.Algo.Commissions
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Тарифный план (биржевой, брокерский или агентский).
	/// </summary>
	public interface ICommissionProfile
	{
		/// <summary>
		/// Получить комиссию.
		/// </summary>
		/// <param name="trade">Сделка.</param>
		/// <returns>Комиссия.</returns>
		decimal GetCommission(MyTrade trade);

		/// <summary>
		/// Получить комиссию.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <returns>Комиссия.</returns>
		decimal GetCommission(Order order);
	}
}