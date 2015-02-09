namespace StockSharp.Algo.Derivatives
{
	using System;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Интерфейс модели расчета значений "греков" по формуле Блэка-Шоулза.
	/// </summary>
	public interface IBlackScholes
	{
		/// <summary>
		/// Опцион.
		/// </summary>
		Security Option { get; }

		/// <summary>
		/// Безрисковая процентная ставка.
		/// </summary>
		decimal RiskFree { get; set; }

		/// <summary>
		/// Размер дивиденда по акциям.
		/// </summary>
		decimal Dividend { get; set; }

		/// <summary>
		/// Рассчитать премию опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <returns>Премия опциона.</returns>
		decimal Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// Рассчитать дельту опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <returns>Дельта опциона.</returns>
		decimal Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// Рассчитать гамму опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <returns>Гамма опциона.</returns>
		decimal Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// Рассчитать вегу опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <returns>Вега опциона.</returns>
		decimal Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// Рассчитать тету опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <returns>Тета опциона.</returns>
		decimal Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// Рассчитать ро опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <returns>Ро опциона.</returns>
		decimal Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// Рассчитать подразумеваемую волатильность.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="premium">Премия по опциону.</param>
		/// <returns>Подразумеваевая волатильность.</returns>
		decimal ImpliedVolatility(DateTimeOffset currentTime, decimal premium);
	}
}