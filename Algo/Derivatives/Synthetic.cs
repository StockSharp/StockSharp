#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Derivatives.Algo
File: Synthetic.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Derivatives
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The synthetic positions builder.
	/// </summary>
	public class Synthetic
	{
		private readonly Security _security;
		private readonly ISecurityProvider _provider;

		/// <summary>
		/// Initializes a new instance of the <see cref="Synthetic"/>.
		/// </summary>
		/// <param name="security">The instrument (the option or the underlying asset).</param>
		/// <param name="provider">The provider of information about instruments.</param>
		public Synthetic(Security security, ISecurityProvider provider)
		{
			_security = security ?? throw new ArgumentNullException(nameof(security));
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		private Security Option
		{
			get
			{
				_security.CheckOption();
				return _security;
			}
		}

		/// <summary>
		/// To get the synthetic position to buy the option.
		/// </summary>
		/// <returns>The synthetic position.</returns>
		public KeyValuePair<Security, Sides>[] Buy()
		{
			return Position(Sides.Buy);
		}

		/// <summary>
		/// To get the synthetic position to sale the option.
		/// </summary>
		/// <returns>The synthetic position.</returns>
		public KeyValuePair<Security, Sides>[] Sell()
		{
			return Position(Sides.Sell);
		}

		/// <summary>
		/// To get the synthetic position for the option.
		/// </summary>
		/// <param name="side">The main position direction.</param>
		/// <returns>The synthetic position.</returns>
		public KeyValuePair<Security, Sides>[] Position(Sides side)
		{
			var asset = Option.GetUnderlyingAsset(_provider);

			return new[]
			{
				new KeyValuePair<Security, Sides>(asset, Option.OptionType == OptionTypes.Call ? side : side.Invert()),
				new KeyValuePair<Security, Sides>(Option.GetOppositeOption(_provider), side)
			};
		}

		/// <summary>
		/// To get the option position for the underlying asset synthetic buy.
		/// </summary>
		/// <param name="strike">Strike.</param>
		/// <returns>The option position.</returns>
		public KeyValuePair<Security, Sides>[] Buy(decimal strike)
		{
			return Buy(strike, GetExpiryDate());
		}

		/// <summary>
		/// To get the option position for the underlying asset synthetic buy.
		/// </summary>
		/// <param name="strike">Strike.</param>
		/// <param name="expiryDate">The date of the option expiration.</param>
		/// <returns>The option position.</returns>
		public KeyValuePair<Security, Sides>[] Buy(decimal strike, DateTimeOffset expiryDate)
		{
			return Position(strike, expiryDate, Sides.Buy);
		}

		/// <summary>
		/// To get the option position for synthetic sale of the base asset.
		/// </summary>
		/// <param name="strike">Strike.</param>
		/// <returns>The option position.</returns>
		public KeyValuePair<Security, Sides>[] Sell(decimal strike)
		{
			return Sell(strike, GetExpiryDate());
		}

		/// <summary>
		/// To get the option position for synthetic sale of the base asset.
		/// </summary>
		/// <param name="strike">Strike.</param>
		/// <param name="expiryDate">The date of the option expiration.</param>
		/// <returns>The option position.</returns>
		public KeyValuePair<Security, Sides>[] Sell(decimal strike, DateTimeOffset expiryDate)
		{
			return Position(strike, expiryDate, Sides.Sell);
		}

		/// <summary>
		/// To get the option position for the synthetic base asset.
		/// </summary>
		/// <param name="strike">Strike.</param>
		/// <param name="expiryDate">The date of the option expiration.</param>
		/// <param name="side">The main position direction.</param>
		/// <returns>The option position.</returns>
		public KeyValuePair<Security, Sides>[] Position(decimal strike, DateTimeOffset expiryDate, Sides side)
		{
			var call = _security.GetCall(_provider, strike, expiryDate);
			var put = _security.GetPut(_provider, strike, expiryDate);

			return new[]
			{
				new KeyValuePair<Security, Sides>(call, side),
				new KeyValuePair<Security, Sides>(put, side.Invert())
			};
		}

		private DateTimeOffset GetExpiryDate()
		{
			if (_security.ExpiryDate == null)
				throw new InvalidOperationException(LocalizedStrings.Str712);

			return _security.ExpiryDate.Value;
		}
	}
}