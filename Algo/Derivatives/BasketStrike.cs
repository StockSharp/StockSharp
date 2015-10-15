namespace StockSharp.Algo.Derivatives
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The virtual strike created from a combination of other strikes.
	/// </summary>
	public abstract class BasketStrike : BasketSecurity
	{
		/// <summary>
		/// Initialize <see cref="BasketStrike"/>.
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
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
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; private set; }

		/// <summary>
		/// The market data provider.
		/// </summary>
		public virtual IMarketDataProvider DataProvider { get; private set; }

		/// <summary>
		/// Underlying asset.
		/// </summary>
		public Security UnderlyingAsset { get; private set; }

		/// <summary>
		/// Instruments, from which this basket is created.
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
		/// To get filtered strikes.
		/// </summary>
		/// <param name="allStrikes">All strikes.</param>
		/// <returns>Filtered strikes.</returns>
		protected abstract IEnumerable<Security> FilterStrikes(IEnumerable<Security> allStrikes);
	}

	/// <summary>
	/// The virtual strike including strikes of the specified shift boundary.
	/// </summary>
	public class OffsetBasketStrike : BasketStrike
	{
		private readonly Range<int> _strikeOffset;
		private decimal _strikeStep;

		/// <summary>
		/// Initializes a new instance of the <see cref="OffsetBasketStrike"/>.
		/// </summary>
		/// <param name="underlyingSecurity">Underlying asset.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <param name="strikeOffset">Boundaries of shift from the main strike (a negative value specifies the shift to options in the money, a positive value - out of the money).</param>
		public OffsetBasketStrike(Security underlyingSecurity, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, Range<int> strikeOffset)
			: base(underlyingSecurity, securityProvider, dataProvider)
		{
			if (strikeOffset == null)
				throw new ArgumentNullException("strikeOffset");

			_strikeOffset = strikeOffset;
		}

		/// <summary>
		/// To get filtered strikes.
		/// </summary>
		/// <param name="allStrikes">All strikes.</param>
		/// <returns>Filtered strikes.</returns>
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
	/// The virtual strike including strikes of the specified volatility boundary.
	/// </summary>
	public class VolatilityBasketStrike : BasketStrike
	{
		private readonly Range<decimal> _volatilityRange;

		/// <summary>
		/// Initializes a new instance of the <see cref="VolatilityBasketStrike"/>.
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <param name="volatilityRange">Volatility range.</param>
		public VolatilityBasketStrike(Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, Range<decimal> volatilityRange)
			: base(underlyingAsset, securityProvider, dataProvider)
		{
			if (volatilityRange == null)
				throw new ArgumentNullException("volatilityRange");

			_volatilityRange = volatilityRange;
		}

		/// <summary>
		/// To get filtered strikes.
		/// </summary>
		/// <param name="allStrikes">All strikes.</param>
		/// <returns>Filtered strikes.</returns>
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