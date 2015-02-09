namespace StockSharp.Algo.Derivatives
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Модель расчета значений "греков" по формуле Блэка-Шоулза.
	/// </summary>
	public class BlackScholes : IBlackScholes
	{
		/// <summary>
		/// Инициализировать <see cref="BlackScholes"/>.
		/// </summary>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		protected BlackScholes(ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			if (securityProvider == null)
				throw new ArgumentNullException("securityProvider");

			if (dataProvider == null)
				throw new ArgumentNullException("dataProvider");

			SecurityProvider = securityProvider;
			DataProvider = dataProvider;
		}

		/// <summary>
		/// Создать <see cref="BlackScholes"/>.
		/// </summary>
		/// <param name="option">Опцион.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		public BlackScholes(Security option, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
			: this(securityProvider, dataProvider)
		{
			if (option == null)
				throw new ArgumentNullException("option");

			Option = option;
		}

		/// <summary>
		/// Поставщик информации об инструментах.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; private set; }

		/// <summary>
		/// Поставщик маркет-данных.
		/// </summary>
		public virtual IMarketDataProvider DataProvider { get; private set; }

		/// <summary>
		/// Опцион.
		/// </summary>
		public virtual Security Option { get; private set; }

		/// <summary>
		/// Безрисковая процентная ставка.
		/// </summary>
		public decimal RiskFree { get; set; }

		/// <summary>
		/// Размер дивиденда по акциям.
		/// </summary>
		public virtual decimal Dividend { get; set; }

		private int _roundDecimals = -1;

		/// <summary>
		/// Количество знаков после запятой у вычисляемых значений. По-умолчанию равно -1, что означает не округлять значения.
		/// </summary>
		public virtual int RoundDecimals
		{
			get { return _roundDecimals; }
			set
			{
				if (value < -1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str702);

				_roundDecimals = value;
			}
		}

		private Security _underlyingAsset;

		/// <summary>
		/// Базовый актив.
		/// </summary>
		public virtual Security UnderlyingAsset
		{
			get { return _underlyingAsset ?? (_underlyingAsset = Option.GetUnderlyingAsset(SecurityProvider)); }
		}

		/// <summary>
		/// Стандартное отклонение по-умолчанию.
		/// </summary>
		public decimal DefaultDeviation
		{
			get { return ((decimal?)DataProvider.GetSecurityValue(Option, Level1Fields.ImpliedVolatility) ?? 0) / 100; }
		}

		/// <summary>
		/// Расчет времени до экспирации.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <returns>Время, оставшееся до экспирации.</returns>
		public virtual double GetExpirationTimeLine(DateTimeOffset currentTime)
		{
			return DerivativesHelper.GetExpirationTimeLine(Option.GetExpirationTime(), currentTime);
		}

		/// <summary>
		/// Получить цену базового актива.
		/// </summary>
		/// <param name="assetPrice">Цена базового актива, если она задана.</param>
		/// <returns>Цена базового актива.</returns>
		public decimal GetAssetPrice(decimal? assetPrice = null)
		{
			if (assetPrice != null)
				return (decimal)assetPrice;

			var price = (decimal?)DataProvider.GetSecurityValue(UnderlyingAsset, Level1Fields.LastTradePrice);
			return price ?? 0;
		}

		/// <summary>
		/// Тип опциона.
		/// </summary>
		protected OptionTypes OptionType
		{
			get
			{
				var type = Option.OptionType;

				if (type == null)
					throw new InvalidOperationException(LocalizedStrings.Str703Params.Put(Option));

				return type.Value;
			}
		}

		/// <summary>
		/// Округлить до <see cref="RoundDecimals"/>.
		/// </summary>
		/// <param name="value">Исходное значение.</param>
		/// <returns>Округленное значение.</returns>
		protected decimal TryRound(decimal value)
		{
			if (RoundDecimals >= 0)
				value = Math.Round(value, RoundDecimals);

			return value;
		}

		/// <summary>
		/// Рассчитать премию опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="UnderlyingAsset"/>.</param>
		/// <returns>Премия опциона.</returns>
		public virtual decimal Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			deviation = deviation ?? DefaultDeviation;
			assetPrice = GetAssetPrice(assetPrice);

			var timeToExp = GetExpirationTimeLine(currentTime);
			return TryRound(DerivativesHelper.Premium(OptionType, Option.Strike, assetPrice.Value, RiskFree, Dividend, deviation.Value, timeToExp, D1(deviation.Value, assetPrice.Value, timeToExp)));
		}

		/// <summary>
		/// Рассчитать дельту опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="UnderlyingAsset"/>.</param>
		/// <returns>Дельта опциона.</returns>
		public virtual decimal Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			assetPrice = GetAssetPrice(assetPrice);
			return TryRound(DerivativesHelper.Delta(OptionType, assetPrice.Value, D1(deviation ?? DefaultDeviation, assetPrice.Value, GetExpirationTimeLine(currentTime))));
		}

		/// <summary>
		/// Рассчитать гамму опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="UnderlyingAsset"/>.</param>
		/// <returns>Гамма опциона.</returns>
		public virtual decimal Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			deviation = deviation ?? DefaultDeviation;
			assetPrice = GetAssetPrice(assetPrice);
			var timeToExp = GetExpirationTimeLine(currentTime);
			return TryRound(DerivativesHelper.Gamma(assetPrice.Value, deviation.Value, timeToExp, D1(deviation.Value, assetPrice.Value, timeToExp)));
		}

		/// <summary>
		/// Рассчитать вегу опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="UnderlyingAsset"/>.</param>
		/// <returns>Вега опциона.</returns>
		public virtual decimal Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			assetPrice = GetAssetPrice(assetPrice);
			var timeToExp = GetExpirationTimeLine(currentTime);
			return TryRound(DerivativesHelper.Vega(assetPrice.Value, timeToExp, D1(deviation ?? DefaultDeviation, assetPrice.Value, timeToExp)));
		}

		/// <summary>
		/// Рассчитать тету опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="UnderlyingAsset"/>.</param>
		/// <returns>Тета опциона.</returns>
		public virtual decimal Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			deviation = deviation ?? DefaultDeviation;
			assetPrice = GetAssetPrice(assetPrice);
			var timeToExp = GetExpirationTimeLine(currentTime);
			return TryRound(DerivativesHelper.Theta(OptionType, Option.Strike, assetPrice.Value, RiskFree, deviation.Value, timeToExp, D1(deviation.Value, assetPrice.Value, timeToExp)));
		}

		/// <summary>
		/// Рассчитать ро опциона.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="deviation">Стандартное отклонение. Если оно не указано, то используется <see cref="DefaultDeviation"/>.</param>
		/// <param name="assetPrice">Цена базового актива. Если цена не указана, то получается цена последней сделки из <see cref="UnderlyingAsset"/>.</param>
		/// <returns>Ро опциона.</returns>
		public virtual decimal Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			deviation = deviation ?? DefaultDeviation;
			assetPrice = GetAssetPrice(assetPrice);
			var timeToExp = GetExpirationTimeLine(currentTime);
			return TryRound(DerivativesHelper.Rho(OptionType, Option.Strike, assetPrice.Value, RiskFree, deviation.Value, timeToExp, D1(deviation.Value, assetPrice.Value, timeToExp)));
		}

		/// <summary>
		/// Рассчитать подразумеваемую волатильность.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="premium">Премия по опциону.</param>
		/// <returns>Подразумеваевая волатильность.</returns>
		public virtual decimal ImpliedVolatility(DateTimeOffset currentTime, decimal premium)
		{
			//var timeToExp = GetExpirationTimeLine();
			return TryRound(DerivativesHelper.ImpliedVolatility(premium, diviation => Premium(currentTime, diviation)));
		}

		/// <summary>
		/// Рассчитать параметр d1 определения вероятности исполнения опциона.
		/// </summary>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <param name="timeToExp">Период опциона до экспирации.</param>
		/// <returns>Параметр d1.</returns>
		protected virtual double D1(decimal deviation, decimal assetPrice, double timeToExp)
		{
			return DerivativesHelper.D1(assetPrice, Option.Strike, RiskFree, Dividend, deviation, timeToExp);
		}
		
		/// <summary>
		/// Создать стакан волатильности.
		/// </summary>
		/// <param name="currentTime">Текущее время.</param>
		/// <returns>Стакан волатильности.</returns>
		public virtual MarketDepth ImpliedVolatility(DateTimeOffset currentTime)
		{
			return DataProvider.GetMarketDepth(Option).ImpliedVolatility(this, currentTime);
		}
	}
}