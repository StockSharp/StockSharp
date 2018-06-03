#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Derivatives.Algo
File: BlackScholes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Derivatives
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The model for calculating Greeks values by the Black-Scholes formula.
	/// </summary>
	public class BlackScholes : IBlackScholes
	{
		/// <summary>
		/// Initialize <see cref="BlackScholes"/>.
		/// </summary>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		protected BlackScholes(ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			SecurityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BlackScholes"/>.
		/// </summary>
		/// <param name="option">Options contract.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		public BlackScholes(Security option, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
			: this(securityProvider, dataProvider)
		{
			Option = option ?? throw new ArgumentNullException(nameof(option));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BlackScholes"/>.
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="dataProvider">The market data provider.</param>
		protected BlackScholes(Security underlyingAsset, IMarketDataProvider dataProvider)
		{
			_underlyingAsset = underlyingAsset ?? throw new ArgumentNullException(nameof(underlyingAsset));
			DataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BlackScholes"/>.
		/// </summary>
		/// <param name="option">Options contract.</param>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="dataProvider">The market data provider.</param>
		public BlackScholes(Security option, Security underlyingAsset, IMarketDataProvider dataProvider)
			: this(underlyingAsset, dataProvider)
		{
			Option = option ?? throw new ArgumentNullException(nameof(option));
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
		/// Options contract.
		/// </summary>
		public virtual Security Option { get; }

		/// <summary>
		/// The risk free interest rate.
		/// </summary>
		public decimal RiskFree { get; set; }

		/// <summary>
		/// The dividend amount on shares.
		/// </summary>
		public virtual decimal Dividend { get; set; }

		private int _roundDecimals = -1;

		/// <summary>
		/// The number of decimal places at calculated values. The default is -1, which means no values rounding.
		/// </summary>
		public virtual int RoundDecimals
		{
			get => _roundDecimals;
			set
			{
				if (value < -1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str702);

				_roundDecimals = value;
			}
		}

		private Security _underlyingAsset;

		/// <summary>
		/// Underlying asset.
		/// </summary>
		public virtual Security UnderlyingAsset
		{
			get => _underlyingAsset ?? (_underlyingAsset = Option.GetUnderlyingAsset(SecurityProvider));
			set => _underlyingAsset = value;
		}

		/// <summary>
		/// The standard deviation by default.
		/// </summary>
		public decimal DefaultDeviation => ((decimal?)DataProvider.GetSecurityValue(Option, Level1Fields.ImpliedVolatility) ?? 0) / 100;

		/// <summary>
		/// The time before expiration calculation.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <returns>The time remaining until expiration. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public virtual double? GetExpirationTimeLine(DateTimeOffset currentTime)
		{
			return DerivativesHelper.GetExpirationTimeLine(Option.GetExpirationTime(), currentTime);
		}

		/// <summary>
		/// To get the price of the underlying asset.
		/// </summary>
		/// <param name="assetPrice">The price of the underlying asset if it is specified.</param>
		/// <returns>The price of the underlying asset. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public decimal? GetAssetPrice(decimal? assetPrice = null)
		{
			if (assetPrice != null)
				return (decimal)assetPrice;

			return (decimal?)DataProvider.GetSecurityValue(UnderlyingAsset, Level1Fields.LastTradePrice);
		}

		/// <summary>
		/// Option type.
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
		/// To round to <see cref="BlackScholes.RoundDecimals"/>.
		/// </summary>
		/// <param name="value">The initial value.</param>
		/// <returns>The rounded value.</returns>
		protected decimal? TryRound(decimal? value)
		{
			if (value != null && RoundDecimals >= 0)
				value = Math.Round(value.Value, RoundDecimals);

			return value;
		}

		/// <summary>
		/// To calculate the option premium.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option premium. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public virtual decimal? Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			deviation = deviation ?? DefaultDeviation;
			assetPrice = GetAssetPrice(assetPrice);

			if (assetPrice == null)
				return null;

			var timeToExp = GetExpirationTimeLine(currentTime);

			if (timeToExp == null)
				return null;

			return TryRound(DerivativesHelper.Premium(OptionType, GetStrike(), assetPrice.Value, RiskFree, Dividend, deviation.Value, timeToExp.Value, D1(deviation.Value, assetPrice.Value, timeToExp.Value)));
		}

		/// <summary>
		/// To calculate the option delta.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option delta. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public virtual decimal? Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			assetPrice = GetAssetPrice(assetPrice);

			if (assetPrice == null)
				return null;

			var timeToExp = GetExpirationTimeLine(currentTime);

			if (timeToExp == null)
				return null;

			return TryRound(DerivativesHelper.Delta(OptionType, assetPrice.Value, D1(deviation ?? DefaultDeviation, assetPrice.Value, timeToExp.Value)));
		}

		/// <summary>
		/// To calculate the option gamma.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option gamma. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public virtual decimal? Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			deviation = deviation ?? DefaultDeviation;
			assetPrice = GetAssetPrice(assetPrice);

			if (assetPrice == null)
				return null;

			var timeToExp = GetExpirationTimeLine(currentTime);

			if (timeToExp == null)
				return null;

			return TryRound(DerivativesHelper.Gamma(assetPrice.Value, deviation.Value, timeToExp.Value, D1(deviation.Value, assetPrice.Value, timeToExp.Value)));
		}

		/// <summary>
		/// To calculate the option vega.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option vega. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public virtual decimal? Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			assetPrice = GetAssetPrice(assetPrice);

			if (assetPrice == null)
				return null;

			var timeToExp = GetExpirationTimeLine(currentTime);

			if (timeToExp == null)
				return null;

			return TryRound(DerivativesHelper.Vega(assetPrice.Value, timeToExp.Value, D1(deviation ?? DefaultDeviation, assetPrice.Value, timeToExp.Value)));
		}

		/// <summary>
		/// To calculate the option theta.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option theta. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public virtual decimal? Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			deviation = deviation ?? DefaultDeviation;
			assetPrice = GetAssetPrice(assetPrice);

			if (assetPrice == null)
				return null;

			var timeToExp = GetExpirationTimeLine(currentTime);

			if (timeToExp == null)
				return null;

			return TryRound(DerivativesHelper.Theta(OptionType, GetStrike(), assetPrice.Value, RiskFree, deviation.Value, timeToExp.Value, D1(deviation.Value, assetPrice.Value, timeToExp.Value)));
		}

		/// <summary>
		/// To calculate the option rho.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option rho. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public virtual decimal? Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			deviation = deviation ?? DefaultDeviation;
			assetPrice = GetAssetPrice(assetPrice);

			if (assetPrice == null)
				return null;

			var timeToExp = GetExpirationTimeLine(currentTime);

			if (timeToExp == null)
				return null;

			return TryRound(DerivativesHelper.Rho(OptionType, GetStrike(), assetPrice.Value, RiskFree, deviation.Value, timeToExp.Value, D1(deviation.Value, assetPrice.Value, timeToExp.Value)));
		}

		/// <summary>
		/// To calculate the implied volatility.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="premium">The option premium.</param>
		/// <returns>The implied volatility. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public virtual decimal? ImpliedVolatility(DateTimeOffset currentTime, decimal premium)
		{
			//var timeToExp = GetExpirationTimeLine();
			return TryRound(DerivativesHelper.ImpliedVolatility(premium, diviation => Premium(currentTime, diviation)));
		}

		/// <summary>
		/// To calculate the d1 parameter of the option fulfilment probability estimating.
		/// </summary>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <param name="timeToExp">The option period before the expiration.</param>
		/// <returns>The d1 parameter.</returns>
		protected virtual double D1(decimal deviation, decimal assetPrice, double timeToExp)
		{
			return DerivativesHelper.D1(assetPrice, GetStrike(), RiskFree, Dividend, deviation, timeToExp);
		}
		
		/// <summary>
		/// To create the order book of volatility.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <returns>The order book volatility.</returns>
		public virtual MarketDepth ImpliedVolatility(DateTimeOffset currentTime)
		{
			return DataProvider.GetMarketDepth(Option).ImpliedVolatility(this, currentTime);
		}

		internal decimal GetStrike()
		{
			return Option.Strike.Value;
		}
	}
}