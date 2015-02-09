namespace StockSharp.Algo.Derivatives
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	using StockSharp.Localization;

	/// <summary>
	/// Модель расчета значений "греков" по формуле Блэка.
	/// </summary>
	public class Black : BlackScholes
	{
		// http://riskencyclopedia.com/articles/black_1976/

		/// <summary>
		/// Создать <see cref="Black"/>.
		/// </summary>
		/// <param name="option">Опцион.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		public Black(Security option, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
			: base(option, securityProvider, dataProvider)
		{
		}

		/// <summary>
		/// Размер дивиденда по акциям.
		/// </summary>
		public override decimal Dividend
		{
			set
			{
				if (value != 0)
					throw new ArgumentOutOfRangeException(LocalizedStrings.Str701Params.Put(UnderlyingAsset));

				base.Dividend = value;
			}
		}

		private decimal GetExpRate(DateTimeOffset currentTime)
		{
			return (decimal)DerivativesHelper.ExpRate(RiskFree, GetExpirationTimeLine(currentTime));
		}

		/// <summary>
		/// Рассчитать премию опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Премия опциона.</returns>
		public override decimal Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Premium(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// Рассчитать дельту опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Дельта опциона.</returns>
		public override decimal Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Delta(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// Рассчитать гамму опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Гамма опциона.</returns>
		public override decimal Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Gamma(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// Рассчитать вегу опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Вега опциона.</returns>
		public override decimal Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Vega(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// Рассчитать тету опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Тета опциона.</returns>
		public override decimal Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Theta(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// Рассчитать ро опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="BlackScholes.DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>Ро опциона.</returns>
		public override decimal Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Rho(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// Рассчитать параметр d1 определения вероятности исполнения опциона.
		/// </summary>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <param name="timeToExp">Период опциона до экспирации.</param>
		/// <returns>Параметр d1.</returns>
		protected override double D1(decimal deviation, decimal assetPrice, double timeToExp)
		{
			return DerivativesHelper.D1(assetPrice, Option.Strike, 0, 0, deviation, timeToExp);
		}
	}
}