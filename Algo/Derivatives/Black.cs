#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Derivatives.Algo
File: Black.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Derivatives
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// The Greeks values calculating model by the Black formula.
	/// </summary>
	public class Black : BlackScholes
	{
		// http://riskencyclopedia.com/articles/black_1976/

		/// <summary>
		/// Initializes a new instance of the <see cref="Black"/>.
		/// </summary>
		/// <param name="option">Options contract.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		public Black(Security option, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
			: base(option, securityProvider, dataProvider)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Black"/>.
		/// </summary>
		/// <param name="option">Options contract.</param>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="dataProvider">The market data provider.</param>
		public Black(Security option, Security underlyingAsset, IMarketDataProvider dataProvider)
			: base(option, underlyingAsset, dataProvider)
		{
		}

		/// <summary>
		/// The dividend amount on shares.
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

		private decimal? GetExpRate(DateTimeOffset currentTime)
		{
			var timeLine = GetExpirationTimeLine(currentTime);

			if (timeLine == null)
				return null;

			return (decimal)DerivativesHelper.ExpRate(RiskFree, timeLine.Value);
		}

		/// <summary>
		/// To calculate the option premium.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option premium. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Premium(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// To calculate the option delta.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option delta. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Delta(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// To calculate the option gamma.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option gamma. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Gamma(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// To calculate the option vega.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option vega. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Vega(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// To calculate the option theta.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option theta. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Theta(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// To calculate the option rho.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">The standard deviation. If it is not specified, then <see cref="BlackScholes.DefaultDeviation"/> is used.</param>
		/// <param name="assetPrice">The price of the underlying asset. If the price is not specified, then the last trade price getting from <see cref="BlackScholes.UnderlyingAsset"/>.</param>
		/// <returns>The option rho. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public override decimal? Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null)
		{
			return GetExpRate(currentTime) * base.Rho(currentTime, deviation, assetPrice);
		}

		/// <summary>
		/// To calculate the d1 parameter of the option fulfilment probability estimating.
		/// </summary>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <param name="timeToExp">The option period before the expiration.</param>
		/// <returns>The d1 parameter.</returns>
		protected override double D1(decimal deviation, decimal assetPrice, double timeToExp)
		{
			return DerivativesHelper.D1(assetPrice, GetStrike(), 0, 0, deviation, timeToExp);
		}
	}
}