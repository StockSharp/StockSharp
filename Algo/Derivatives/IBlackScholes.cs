#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Derivatives.Algo
File: IBlackScholes.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Derivatives
{
	using System;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface of the model for calculating Greeks values by the Black-Scholes formula.
	/// </summary>
	public interface IBlackScholes
	{
		/// <summary>
		/// Options contract.
		/// </summary>
		Security Option { get; }

		/// <summary>
		/// The risk free interest rate.
		/// </summary>
		decimal RiskFree { get; set; }

		/// <summary>
		/// The dividend amount on shares.
		/// </summary>
		decimal Dividend { get; set; }

		/// <summary>
		/// To calculate the option premium.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <returns>The option premium. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		decimal? Premium(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// To calculate the option delta.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <returns>The option delta. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		decimal? Delta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// To calculate the option gamma.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <returns>The option gamma. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		decimal? Gamma(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// To calculate the option vega.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <returns>The option vega. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		decimal? Vega(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// To calculate the option theta.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <returns>The option theta. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		decimal? Theta(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// To calculate the option rho.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <returns>The option rho. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		decimal? Rho(DateTimeOffset currentTime, decimal? deviation = null, decimal? assetPrice = null);

		/// <summary>
		/// To calculate the implied volatility.
		/// </summary>
		/// <param name="currentTime">The current time.</param>
		/// <param name="premium">The option premium.</param>
		/// <returns>The implied volatility. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		decimal? ImpliedVolatility(DateTimeOffset currentTime, decimal premium);
	}
}