namespace StockSharp.Algo.Derivatives
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Виртуальный страйк, созданный из комбинации других страйков.
	/// </summary>
	public abstract class BasketStrike : BasketSecurity
	{
		/// <summary>
		/// Инициализировать <see cref="BasketStrike"/>.
		/// </summary>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		protected BasketStrike(Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			if (underlyingAsset == null)
				throw new ArgumentNullException("underlyingAsset");

			if (securityProvider == null)
				throw new ArgumentNullException("securityProvider");

			if (dataProvider == null)
				throw new ArgumentNullException("dataProvider");

			UnderlyingAsset = underlyingAsset;
			SecurityProvider = securityProvider;
			DataProvider = dataProvider;
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
		/// Базовый актив.
		/// </summary>
		public Security UnderlyingAsset { get; private set; }

		/// <summary>
		/// Инструменты, из которых создана данная корзина.
		/// </summary>
		public override IEnumerable<Security> InnerSecurities
		{
			get
			{
				var derivatives = UnderlyingAsset.GetDerivatives(SecurityProvider, ExpiryDate);

				var type = OptionType;

				if (type != null)
					derivatives = derivatives.Filter((OptionTypes)type);

				return FilterStrikes(derivatives);
			}
		}

		/// <summary>
		/// Получить отфильтрованные страйки.
		/// </summary>
		/// <param name="allStrikes">Все страйки.</param>
		/// <returns>Отфильтрованные страйки.</returns>
		protected abstract IEnumerable<Security> FilterStrikes(IEnumerable<Security> allStrikes);
	}

	/// <summary>
	/// Виртуальный страйк, включающий в себя страйки заданной границы сдвига.
	/// </summary>
	public class OffsetBasketStrike : BasketStrike
	{
		private readonly Range<int> _strikeOffset;
		private decimal _strikeStep;

		/// <summary>
		/// Создать <see cref="OffsetBasketStrike"/>.
		/// </summary>
		/// <param name="underlyingSecurity">Базовый актив.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		/// <param name="strikeOffset">Границы сдвига от центрального страйка (отрицательное значение задает сдвиг в опционы в деньгах, положительное - вне денег).</param>
		public OffsetBasketStrike(Security underlyingSecurity, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, Range<int> strikeOffset)
			: base(underlyingSecurity, securityProvider, dataProvider)
		{
			if (strikeOffset == null)
				throw new ArgumentNullException("strikeOffset");

			_strikeOffset = strikeOffset;
		}

		/// <summary>
		/// Получить отфильтрованные страйки.
		/// </summary>
		/// <param name="allStrikes">Все страйки.</param>
		/// <returns>Отфильтрованные страйки.</returns>
		protected override IEnumerable<Security> FilterStrikes(IEnumerable<Security> allStrikes)
		{
			if (_strikeStep == 0)
				_strikeStep = UnderlyingAsset.GetStrikeStep(SecurityProvider, ExpiryDate);

			var centralStrike = UnderlyingAsset.GetCentralStrike(DataProvider, allStrikes);

			var callStrikeFrom = centralStrike.Strike + _strikeOffset.Min * _strikeStep;
			var callStrikeTo = centralStrike.Strike + _strikeOffset.Max * _strikeStep;

			var putStrikeFrom = centralStrike.Strike - _strikeOffset.Max * _strikeStep;
			var putStrikeTo = centralStrike.Strike - _strikeOffset.Min * _strikeStep;

			return allStrikes.Where(s =>
							(s.OptionType == OptionTypes.Call && s.Strike >= callStrikeFrom && s.Strike <= callStrikeTo)
							||
							(s.OptionType == OptionTypes.Put && s.Strike >= putStrikeFrom && s.Strike <= putStrikeTo)
						)
						.OrderBy(s => s.Strike);
		}
	}

	/// <summary>
	/// Виртуальный страйк, включающий в себя страйки заданной границы волатильности.
	/// </summary>
	public class VolatilityBasketStrike : BasketStrike
	{
		private readonly Range<decimal> _volatilityRange;

		/// <summary>
		/// Создать <see cref="VolatilityBasketStrike"/>.
		/// </summary>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		/// <param name="volatilityRange">Границы волатильности.</param>
		public VolatilityBasketStrike(Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, Range<decimal> volatilityRange)
			: base(underlyingAsset, securityProvider, dataProvider)
		{
			if (volatilityRange == null)
				throw new ArgumentNullException("volatilityRange");

			_volatilityRange = volatilityRange;
		}

		/// <summary>
		/// Получить отфильтрованные страйки.
		/// </summary>
		/// <param name="allStrikes">Все страйки.</param>
		/// <returns>Отфильтрованные страйки.</returns>
		protected override IEnumerable<Security> FilterStrikes(IEnumerable<Security> allStrikes)
		{
			return allStrikes.Where(s =>
			{
				var iv = (decimal?)DataProvider.GetSecurityValue(s, Level1Fields.ImpliedVolatility);
				return iv != null && _volatilityRange.Contains(iv.Value);
			});
		}
	}
}