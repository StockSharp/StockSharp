#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Derivatives.Algo
File: BasketStrike.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			UnderlyingAsset = underlyingAsset ?? throw new ArgumentNullException(nameof(underlyingAsset));
			SecurityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
		}

		/// <summary>
		/// The provider of information about instruments.
		/// </summary>
		public ISecurityProvider SecurityProvider { get; }

		/// <summary>
		/// The market data provider.
		/// </summary>
		public virtual IMarketDataProvider DataProvider { get; }

		/// <summary>
		/// Underlying asset.
		/// </summary>
		public Security UnderlyingAsset { get; }

		/// <inheritdoc />
		public override IEnumerable<SecurityId> InnerSecurityIds
		{
			get
			{
				var derivatives = UnderlyingAsset.GetDerivatives(SecurityProvider, ExpiryDate);

				var type = OptionType;

				if (type != null)
					derivatives = derivatives.Filter((OptionTypes)type);

				return FilterStrikes(derivatives).Select(s => s.ToSecurityId());
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
		private Range<int> _strikeOffset;
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
			_strikeOffset = strikeOffset ?? throw new ArgumentNullException(nameof(strikeOffset));
		}

		/// <inheritdoc />
		protected override IEnumerable<Security> FilterStrikes(IEnumerable<Security> allStrikes)
		{
			if (_strikeStep == 0)
				_strikeStep = UnderlyingAsset.GetStrikeStep(SecurityProvider, ExpiryDate);

			allStrikes = allStrikes.ToArray();

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

		/// <inheritdoc />
		protected override string ToSerializedString()
		{
			return _strikeOffset.ToString();
		}

		/// <inheritdoc />
		protected override void FromSerializedString(string text)
		{
			_strikeOffset = Range<int>.Parse(text);
		}
	}

	/// <summary>
	/// The virtual strike including strikes of the specified volatility boundary.
	/// </summary>
	public class VolatilityBasketStrike : BasketStrike
	{
		private Range<decimal> _volatilityRange;

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
			_volatilityRange = volatilityRange ?? throw new ArgumentNullException(nameof(volatilityRange));
		}

		/// <inheritdoc />
		protected override IEnumerable<Security> FilterStrikes(IEnumerable<Security> allStrikes)
		{
			return allStrikes.Where(s =>
			{
				var iv = (decimal?)DataProvider.GetSecurityValue(s, Level1Fields.ImpliedVolatility);
				return iv != null && _volatilityRange.Contains(iv.Value);
			});
		}

		/// <inheritdoc />
		protected override string ToSerializedString()
		{
			return _volatilityRange.ToString();
		}

		/// <inheritdoc />
		protected override void FromSerializedString(string text)
		{
			_volatilityRange = Range<decimal>.Parse(text);
		}
	}
}