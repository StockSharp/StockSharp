#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Derivatives.Algo
File: DerivativesHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Derivatives
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Text.RegularExpressions;

	using Ecng.Collections;
	using Ecng.Common;

	using MathNet.Numerics.Distributions;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Extension class for derivatives.
	/// </summary>
	public static class DerivativesHelper
	{
		private static readonly Regex _futureNameRegex = new Regex(@"(?<code>[A-Z,a-z]+)-(?<expiryMonth>[0-9]{1,2})\.(?<expiryYear>[0-9]{1,2})", RegexOptions.Compiled);
		private static readonly Regex _optionNameRegex = new Regex(@"(?<code>\w+-[0-9]{1,2}\.[0-9]{1,2})(?<isMargin>[M_])(?<expiryDate>[0-9]{6,6})(?<optionType>[CP])(?<region>\w)\s(?<strike>\d*\.*\d*)", RegexOptions.Compiled);
		private static readonly Regex _optionCodeRegex = new Regex(@"(?<code>[A-Z,a-z]+)(?<strike>\d*\.*\d*)(?<optionType>[BA])(?<expiryMonth>[A-X]{1})(?<expiryYear>[0-9]{1})", RegexOptions.Compiled);

		private static readonly SynchronizedPairSet<int, char> _futureMonthCodes = new SynchronizedPairSet<int, char>();
		private static readonly SynchronizedPairSet<int, char> _optionCallMonthCodes = new SynchronizedPairSet<int, char>();
		private static readonly SynchronizedPairSet<int, char> _optionPutMonthCodes = new SynchronizedPairSet<int, char>();
		private static readonly Normal _normalDistribution = new Normal();

		static DerivativesHelper()
		{
			// http://www.rts.ru/s193
			_futureMonthCodes.Add(1, 'F');
			_futureMonthCodes.Add(2, 'G');
			_futureMonthCodes.Add(3, 'H');
			_futureMonthCodes.Add(4, 'J');
			_futureMonthCodes.Add(5, 'K');
			_futureMonthCodes.Add(6, 'M');
			_futureMonthCodes.Add(7, 'N');
			_futureMonthCodes.Add(8, 'Q');
			_futureMonthCodes.Add(9, 'U');
			_futureMonthCodes.Add(10, 'V');
			_futureMonthCodes.Add(11, 'X');
			_futureMonthCodes.Add(12, 'Z');

			_optionCallMonthCodes.Add(1, 'A');
			_optionCallMonthCodes.Add(2, 'B');
			_optionCallMonthCodes.Add(3, 'C');
			_optionCallMonthCodes.Add(4, 'D');
			_optionCallMonthCodes.Add(5, 'E');
			_optionCallMonthCodes.Add(6, 'F');
			_optionCallMonthCodes.Add(7, 'G');
			_optionCallMonthCodes.Add(8, 'H');
			_optionCallMonthCodes.Add(9, 'I');
			_optionCallMonthCodes.Add(10, 'J');
			_optionCallMonthCodes.Add(11, 'K');
			_optionCallMonthCodes.Add(12, 'L');

			_optionPutMonthCodes.Add(1, 'M');
			_optionPutMonthCodes.Add(2, 'N');
			_optionPutMonthCodes.Add(3, 'O');
			_optionPutMonthCodes.Add(4, 'P');
			_optionPutMonthCodes.Add(5, 'Q');
			_optionPutMonthCodes.Add(6, 'R');
			_optionPutMonthCodes.Add(7, 'S');
			_optionPutMonthCodes.Add(8, 'T');
			_optionPutMonthCodes.Add(9, 'U');
			_optionPutMonthCodes.Add(10, 'V');
			_optionPutMonthCodes.Add(11, 'W');
			_optionPutMonthCodes.Add(12, 'X');
		}

		private static readonly SynchronizedDictionary<Security, Security> _underlyingSecurities = new SynchronizedDictionary<Security, Security>();

		/// <summary>
		/// To get the underlying asset by the derivative.
		/// </summary>
		/// <param name="derivative">The derivative.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <returns>Underlying asset.</returns>
		public static Security GetUnderlyingAsset(this Security derivative, ISecurityProvider provider)
		{
			if (derivative == null)
				throw new ArgumentNullException(nameof(derivative));

			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (derivative.Type == SecurityTypes.Option)
			{
				derivative.CheckOption();

				return _underlyingSecurities.SafeAdd(derivative, key =>
				{
					var underlyingSecurity = provider.LookupById(key.UnderlyingSecurityId);

					if (underlyingSecurity == null)
						throw new InvalidOperationException(LocalizedStrings.Str704Params.Put(key.UnderlyingSecurityId));

					return underlyingSecurity;
				});
			}
			else
			{
				return provider.LookupById(derivative.UnderlyingSecurityId);
			}
		}

		/// <summary>
		/// To filter options by the strike <see cref="Security.Strike"/>.
		/// </summary>
		/// <param name="options">Options to be filtered.</param>
		/// <param name="strike">The strike price.</param>
		/// <returns>Filtered options.</returns>
		public static IEnumerable<Security> Filter(this IEnumerable<Security> options, decimal strike)
		{
			return options.Where(o => o.Strike == strike);
		}

		/// <summary>
		/// To filter options by type <see cref="Security.OptionType"/>.
		/// </summary>
		/// <param name="options">Options to be filtered.</param>
		/// <param name="type">Option type.</param>
		/// <returns>Filtered options.</returns>
		public static IEnumerable<Security> Filter(this IEnumerable<Security> options, OptionTypes type)
		{
			return options.Where(o => o.OptionType == type);
		}

		/// <summary>
		/// To filter instruments by the underlying asset.
		/// </summary>
		/// <param name="securities">Instruments to be filtered.</param>
		/// <param name="asset">Underlying asset.</param>
		/// <returns>Instruments filtered.</returns>
		public static IEnumerable<Security> FilterByUnderlying(this IEnumerable<Security> securities, Security asset)
		{
			if (asset == null)
				throw new ArgumentNullException(nameof(asset));

			return securities.Where(s => s.UnderlyingSecurityId == asset.Id);
		}

		/// <summary>
		/// To filter instruments by the expiration date <see cref="Security.ExpiryDate"/>.
		/// </summary>
		/// <param name="securities">Instruments to be filtered.</param>
		/// <param name="expirationDate">The expiration date.</param>
		/// <returns>Instruments filtered.</returns>
		public static IEnumerable<Security> Filter(this IEnumerable<Security> securities, DateTimeOffset? expirationDate)
		{
			if (expirationDate == null)
				return securities;

			return securities.Where(s => s.ExpiryDate == expirationDate);
		}

		/// <summary>
		/// To get derivatives by the underlying asset.
		/// </summary>
		/// <param name="asset">Underlying asset.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="expirationDate">The expiration date.</param>
		/// <returns>The list of derivatives.</returns>
		/// <remarks>
		/// It returns an empty list if derivatives are not found.
		/// </remarks>
		public static IEnumerable<Security> GetDerivatives(this Security asset, ISecurityProvider provider, DateTimeOffset? expirationDate = null)
		{
			return provider.Lookup(new Security
			{
				UnderlyingSecurityId = asset.Id,
				ExpiryDate = expirationDate,
			});
		}

		/// <summary>
		/// To get the underlying asset.
		/// </summary>
		/// <param name="derivative">The derivative.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <returns>Underlying asset.</returns>
		public static Security GetAsset(this Security derivative, ISecurityProvider provider)
		{
			var asset = provider.LookupById(derivative.UnderlyingSecurityId);

			if (asset == null)
				throw new ArgumentException(LocalizedStrings.Str705Params.Put(derivative));

			return asset;
		}

		/// <summary>
		/// To change the option type for opposite.
		/// </summary>
		/// <param name="type">The initial value.</param>
		/// <returns>The opposite value.</returns>
		public static OptionTypes Invert(this OptionTypes type)
		{
			return type == OptionTypes.Call ? OptionTypes.Put : OptionTypes.Call;
		}

		/// <summary>
		/// To get opposite option (for Call to get Put, for Put to get Call).
		/// </summary>
		/// <param name="option">Options contract.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <returns>The opposite option.</returns>
		public static Security GetOppositeOption(this Security option, ISecurityProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			option.CheckOption();

			var oppositeOption = provider
				.Lookup(new Security
				{
					OptionType = option.OptionType == OptionTypes.Call ? OptionTypes.Put : OptionTypes.Call,
					Strike = option.Strike,
					ExpiryDate = option.ExpiryDate,
					UnderlyingSecurityId = option.UnderlyingSecurityId,
				})
				.FirstOrDefault();

			if (oppositeOption == null)
				throw new ArgumentException(LocalizedStrings.Str706Params.Put(option.Id), nameof(option));

			return oppositeOption;
		}

		/// <summary>
		/// To get Call for the underlying futures.
		/// </summary>
		/// <param name="future">Underlying futures.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="strike">Strike.</param>
		/// <param name="expirationDate">The date of the option expiration.</param>
		/// <returns>The Call option.</returns>
		public static Security GetCall(this Security future, ISecurityProvider provider, decimal strike, DateTimeOffset expirationDate)
		{
			return future.GetOption(provider, strike, expirationDate, OptionTypes.Call);
		}

		/// <summary>
		/// To get Put for the underlying futures.
		/// </summary>
		/// <param name="future">Underlying futures.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="strike">Strike.</param>
		/// <param name="expirationDate">The date of the option expiration.</param>
		/// <returns>The Put option.</returns>
		public static Security GetPut(this Security future, ISecurityProvider provider, decimal strike, DateTimeOffset expirationDate)
		{
			return future.GetOption(provider, strike, expirationDate, OptionTypes.Put);
		}

		/// <summary>
		/// To get an option for the underlying futures.
		/// </summary>
		/// <param name="future">Underlying futures.</param>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="strike">Strike.</param>
		/// <param name="expirationDate">The options expiration date.</param>
		/// <param name="optionType">Option type.</param>
		/// <returns>Options contract.</returns>
		public static Security GetOption(this Security future, ISecurityProvider provider, decimal strike, DateTimeOffset expirationDate, OptionTypes optionType)
		{
			if (future == null)
				throw new ArgumentNullException(nameof(future));

			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			var option = provider
				.Lookup(new Security
				{
					Strike = strike,
					OptionType = optionType,
					ExpiryDate = expirationDate,
					UnderlyingSecurityId = future.Id
				})
				.FirstOrDefault();

			if (option == null)
				throw new ArgumentException(LocalizedStrings.Str707Params.Put(future.Id), nameof(future));

			return option;
		}

		/// <summary>
		/// To get the main strike.
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <param name="expirationDate">The options expiration date.</param>
		/// <param name="optionType">Option type.</param>
		/// <returns>The main strike.</returns>
		public static Security GetCentralStrike(this Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, DateTimeOffset expirationDate, OptionTypes optionType)
		{
			return underlyingAsset.GetCentralStrike(dataProvider, underlyingAsset.GetDerivatives(securityProvider, expirationDate).Filter(optionType));
		}

		/// <summary>
		/// To get the main strike.
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="allStrikes">All strikes.</param>
		/// <returns>The main strike. If it is impossible to get the current market price of the asset then the <see langword="null" /> will be returned.</returns>
		public static Security GetCentralStrike(this Security underlyingAsset, IMarketDataProvider provider, IEnumerable<Security> allStrikes)
		{
			var assetPrice = underlyingAsset.GetCurrentPrice(provider);

			return assetPrice == null
				? null
				: allStrikes
					.Where(s => s.Strike != null)
					.OrderBy(s => Math.Abs((decimal)(s.Strike.Value - assetPrice)))
					.FirstOrDefault();
		}

		/// <summary>
		/// To get the strike step size.
		/// </summary>
		/// <param name="provider">The provider of information about instruments.</param>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="expirationDate">The options expiration date (to specify a particular series).</param>
		/// <returns>The strike step size.</returns>
		public static decimal GetStrikeStep(this Security underlyingAsset, ISecurityProvider provider, DateTimeOffset? expirationDate = null)
		{
			var group = underlyingAsset
				.GetDerivatives(provider, expirationDate)
				.Filter(OptionTypes.Call)
				.Where(s => s.Strike != null)
				.GroupBy(s => s.ExpiryDate)
				.FirstOrDefault();

			if (group == null)
				throw new InvalidOperationException(LocalizedStrings.Str708);

			var orderedStrikes = group.OrderBy(s => s.Strike).Take(2).ToArray();
			return orderedStrikes[1].Strike.Value - orderedStrikes[0].Strike.Value;
		}

		/// <summary>
		/// To get out of the money options (OTM).
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <returns>Out of the money options.</returns>
		public static IEnumerable<Security> GetOutOfTheMoney(this Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			return underlyingAsset.GetOutOfTheMoney(dataProvider, underlyingAsset.GetDerivatives(securityProvider));
		}

		/// <summary>
		/// To get out of the money options (OTM).
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="allStrikes">All strikes.</param>
		/// <returns>Out of the money options.</returns>
		public static IEnumerable<Security> GetOutOfTheMoney(this Security underlyingAsset, IMarketDataProvider provider, IEnumerable<Security> allStrikes)
		{
			if (underlyingAsset == null)
				throw new ArgumentNullException(nameof(underlyingAsset));

			allStrikes = allStrikes.ToArray();

			var cs = underlyingAsset.GetCentralStrike(provider, allStrikes);

			if (cs == null)
				return Enumerable.Empty<Security>();

			return allStrikes.Where(s => s.Strike != null && s.OptionType == OptionTypes.Call ? s.Strike > cs.Strike : s.Strike < cs.Strike);
		}

		/// <summary>
		/// To get in the money options (ITM).
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <returns>In the money options.</returns>
		public static IEnumerable<Security> GetInTheMoney(this Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			return underlyingAsset.GetInTheMoney(dataProvider, underlyingAsset.GetDerivatives(securityProvider));
		}

		/// <summary>
		/// To get in the money options (ITM).
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="allStrikes">All strikes.</param>
		/// <returns>In the money options.</returns>
		public static IEnumerable<Security> GetInTheMoney(this Security underlyingAsset, IMarketDataProvider provider, IEnumerable<Security> allStrikes)
		{
			if (underlyingAsset == null)
				throw new ArgumentNullException(nameof(underlyingAsset));

			allStrikes = allStrikes.ToArray();

			var cs = underlyingAsset.GetCentralStrike(provider, allStrikes);

			if (cs == null)
				return Enumerable.Empty<Security>();

			return allStrikes.Where(s => s.Strike != null && s.OptionType == OptionTypes.Call ? s.Strike < cs.Strike : s.Strike > cs.Strike);
		}

		/// <summary>
		/// To get at the money options (ATM).
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <returns>At the money options.</returns>
		public static IEnumerable<Security> GetAtTheMoney(this Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			return underlyingAsset.GetAtTheMoney(dataProvider, underlyingAsset.GetDerivatives(securityProvider));
		}

		/// <summary>
		/// To get at the money options (ATM).
		/// </summary>
		/// <param name="underlyingAsset">Underlying asset.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="allStrikes">All strikes.</param>
		/// <returns>At the money options.</returns>
		public static IEnumerable<Security> GetAtTheMoney(this Security underlyingAsset, IMarketDataProvider provider, IEnumerable<Security> allStrikes)
		{
			if (underlyingAsset == null)
				throw new ArgumentNullException(nameof(underlyingAsset));

			allStrikes = allStrikes.ToArray();

			var centralStrikes = new List<Security>();

			var cs = underlyingAsset.GetCentralStrike(provider, allStrikes.Filter(OptionTypes.Call));

			if (cs != null)
				centralStrikes.Add(cs);

			cs = underlyingAsset.GetCentralStrike(provider, allStrikes.Filter(OptionTypes.Put));

			if (cs != null)
				centralStrikes.Add(cs);

			return centralStrikes;
		}

		/// <summary>
		/// To get the internal option value.
		/// </summary>
		/// <param name="option">Options contract.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <returns>The internal value. If it is impossible to get the current market price of the asset then the <see langword="null" /> will be returned.</returns>
		public static decimal? GetIntrinsicValue(this Security option, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			if (securityProvider == null)
				throw new ArgumentNullException(nameof(securityProvider));
			
			option.CheckOption();

			if (option.Strike == null)
				return null;

			var assetPrice = option.GetUnderlyingAsset(securityProvider).GetCurrentPrice(dataProvider);

			if (assetPrice == null)
				return null;

			return ((decimal)(option.OptionType == OptionTypes.Call ? assetPrice - option.Strike : option.Strike - assetPrice)).Max(0);
		}

		/// <summary>
		/// To get the timed option value.
		/// </summary>
		/// <param name="option">Options contract.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <returns>The timed value. If it is impossible to get the current market price of the asset then the <see langword="null" /> will be returned.</returns>
		public static decimal? GetTimeValue(this Security option, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			if (securityProvider == null)
				throw new ArgumentNullException(nameof(securityProvider));

			option.CheckOption();

			var price = option.GetCurrentPrice(dataProvider);
			var intrinsic = option.GetIntrinsicValue(securityProvider, dataProvider);

			if (price == null || intrinsic == null)
				return null;

			return (decimal)(price - intrinsic);
		}

		internal static DateTimeOffset GetExpirationTime(this Security security)
		{
			if (security.ExpiryDate == null)
				throw new ArgumentException(LocalizedStrings.Str709Params.Put(security.Id), nameof(security));

			var expDate = security.ExpiryDate.Value;

			if (expDate.TimeOfDay == TimeSpan.Zero)
				expDate += security.CheckExchangeBoard().ExpiryTime;

			return expDate;
		}

		/// <summary>
		/// To check whether the instrument has finished the action.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="currentTime">The current time.</param>
		/// <returns><see langword="true" /> if the instrument has finished its action.</returns>
		public static bool IsExpired(this Security security, DateTimeOffset currentTime)
		{
			return security.GetExpirationTime() <= currentTime;
		}

		/// <summary>
		/// To get the information about the option from its name (underlying asset, strike, expiration date, etc.).
		/// </summary>
		/// <param name="optionName">The option name.</param>
		/// <param name="board">Board info.</param>
		/// <returns>Information about the option.</returns>
		public static Security GetOptionInfo(this string optionName, ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			if (optionName.IsEmpty())
				throw new ArgumentNullException(nameof(optionName));

			var matches = _optionNameRegex.Matches(optionName);

			if (matches.Count != 1)
				return null;

			var groups = matches[0].Groups;

			if (groups.Count == 7)
			{
				return new Security
				{
					UnderlyingSecurityId = groups["code"].Value,
					ExpiryDate = groups["expiryDate"].Value.ToDateTime("ddMMyy").ApplyTimeZone(board.TimeZone),
					OptionType = groups["optionType"].Value == "C" ? OptionTypes.Call : OptionTypes.Put,
					Strike = decimal.Parse(groups["strike"].Value, CultureInfo.InvariantCulture),
				};
			}

			return null;
		}

		/// <summary>
		/// To get the information about the futures from its name (underlying asset, expiration date, etc.).
		/// </summary>
		/// <param name="futureName">The futures name.</param>
		/// <param name="optionCode">The option code.</param>
		/// <param name="board">Board info.</param>
		/// <returns>Information about futures.</returns>
		public static SecurityMessage GetFutureInfo(this string futureName, string optionCode, ExchangeBoard board)
		{
			if (board == null)
				throw new ArgumentNullException(nameof(board));

			if (futureName.IsEmpty())
				throw new ArgumentNullException(nameof(futureName));

			if (optionCode.IsEmpty())
				throw new ArgumentNullException(nameof(optionCode));

			var matches = _futureNameRegex.Matches(futureName);

			if (matches.Count != 1)
				return null;

			var groups = matches[0].Groups;

			if (groups.Count != 4)
				return null;

			var yearStr = groups["expiryYear"].Value;
			var month = groups["expiryMonth"].Value.To<int>();

			var optionMatch = _optionCodeRegex.Match(optionCode);

			return new SecurityMessage
			{
				//Name = groups["code"].Value,
				SecurityId = new SecurityId
				{
					SecurityCode = optionMatch.Groups["code"].Value + _futureMonthCodes[month] + yearStr.Last(),
				},
				ExpiryDate = new DateTime(2000 + yearStr.To<int>(), month, 1).ApplyTimeZone(board.TimeZone),
				Name = futureName,
			};
		}

		/// <summary>
		/// To create the volatility order book from usual order book.
		/// </summary>
		/// <param name="depth">The order book quotes of which will be changed to volatility quotes.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="dataProvider">The market data provider.</param>
		/// <param name="currentTime">The current time.</param>
		/// <param name="riskFree">The risk free interest rate.</param>
		/// <param name="dividend">The dividend amount on shares.</param>
		/// <returns>The order book volatility.</returns>
		public static MarketDepth ImpliedVolatility(this MarketDepth depth, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, DateTimeOffset currentTime, decimal riskFree = 0, decimal dividend = 0)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			return depth.ImpliedVolatility(new BlackScholes(depth.Security, securityProvider, dataProvider) { RiskFree = riskFree, Dividend = dividend }, currentTime);
		}

		/// <summary>
		/// To create the volatility order book from usual order book.
		/// </summary>
		/// <param name="depth">The order book quotes of which will be changed to volatility quotes.</param>
		/// <param name="model">The model for calculating Greeks values by the Black-Scholes formula.</param>
		/// <param name="currentTime">The current time.</param>
		/// <returns>The order book volatility.</returns>
		public static MarketDepth ImpliedVolatility(this MarketDepth depth, BlackScholes model, DateTimeOffset currentTime)
		{
			if (depth == null)
				throw new ArgumentNullException(nameof(depth));

			if (model == null)
				throw new ArgumentNullException(nameof(model));

			Quote Convert(Quote quote)
			{
				quote = quote.Clone();
				quote.Price = model.ImpliedVolatility(currentTime, quote.Price) ?? 0;
				return quote;
			}

			return new MarketDepth(depth.Security).Update(depth.Bids.Select(Convert), depth.Asks.Select(Convert), true, depth.LastChangeTime);
		}

		/// <summary>
		/// To get the option period before expiration.
		/// </summary>
		/// <param name="expirationTime">The option expiration time.</param>
		/// <param name="currentTime">The current time.</param>
		/// <returns>The option period before expiration. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public static double? GetExpirationTimeLine(DateTimeOffset expirationTime, DateTimeOffset currentTime)
		{
			return GetExpirationTimeLine(expirationTime, currentTime, TimeSpan.FromDays(365));
		}

		/// <summary>
		/// To get the option period before expiration.
		/// </summary>
		/// <param name="expirationTime">The option expiration time.</param>
		/// <param name="currentTime">The current time.</param>
		/// <param name="timeLine">The length of the total period.</param>
		/// <returns>The option period before expiration. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public static double? GetExpirationTimeLine(DateTimeOffset expirationTime, DateTimeOffset currentTime, TimeSpan timeLine)
		{
			var retVal = expirationTime - currentTime;

			if (retVal <= TimeSpan.Zero)
				return null;
				//throw new InvalidOperationException(LocalizedStrings.Str710Params.Put(expirationTime, currentTime));

			return (double)retVal.Ticks / timeLine.Ticks;
		}

		//private const int _dayInYear = 365; // Количество дней в году (расчет временного распада)

		private static double InvertD1(double d1)
		{
			// http://ru.wikipedia.org/wiki/Нормальное_распределение (сигма=1 и мю=0)
			return Math.Exp(-d1 * d1 / 2.0) / Math.Sqrt(2 * Math.PI);
		}

		/// <summary>
		/// To calculate the time exhibitor.
		/// </summary>
		/// <param name="riskFree">The risk free interest rate.</param>
		/// <param name="timeToExp">The option period before the expiration.</param>
		/// <returns>The time exhibitor.</returns>
		public static double ExpRate(decimal riskFree, double timeToExp)
		{
			return riskFree == 0 ? 1 : Math.Exp(-(double)riskFree * timeToExp);
		}

		/// <summary>
		/// To calculate the d1 parameter of the option fulfilment probability estimating.
		/// </summary>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <param name="strike">The strike price.</param>
		/// <param name="riskFree">The risk free interest rate.</param>
		/// <param name="dividend">The dividend amount on shares.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="timeToExp">The option period before the expiration.</param>
		/// <returns>The d1 parameter of the option fulfilment probability estimating.</returns>
		public static double D1(decimal assetPrice, decimal strike, decimal riskFree, decimal dividend, decimal deviation, double timeToExp)
		{
			if (deviation < 0)
				throw new ArgumentOutOfRangeException(nameof(deviation), deviation, LocalizedStrings.Str711);

			return ((double)(assetPrice / strike).Log() +
				(double)(riskFree - dividend + deviation * deviation / 2.0m) * timeToExp) / ((double)deviation * timeToExp.Sqrt());
		}

		/// <summary>
		/// To calculate the d2 parameter of the option fulfilment probability estimating.
		/// </summary>
		/// <param name="d1">The d1 parameter of the option fulfilment probability estimating.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="timeToExp">The option period before the expiration.</param>
		/// <returns>The d2 parameter of the option fulfilment probability estimating.</returns>
		public static double D2(double d1, decimal deviation, double timeToExp)
		{
			return d1 - (double)deviation * timeToExp.Sqrt();
		}

		/// <summary>
		/// To calculate the option premium.
		/// </summary>
		/// <param name="optionType">Option type.</param>
		/// <param name="strike">The strike price.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <param name="riskFree">The risk free interest rate.</param>
		/// <param name="dividend">The dividend amount on shares.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="timeToExp">The option period before the expiration.</param>
		/// <param name="d1">The d1 parameter of the option fulfilment probability estimating.</param>
		/// <returns>The option premium.</returns>
		public static decimal Premium(OptionTypes optionType, decimal strike, decimal assetPrice, decimal riskFree, decimal dividend, decimal deviation, double timeToExp, double d1)
		{
			var sign = (optionType == OptionTypes.Call) ? 1 : -1;

			var expDiv = ExpRate(dividend, timeToExp);
			var expRate = ExpRate(riskFree, timeToExp);

			return (assetPrice * (decimal)(expDiv * NormalDistr(d1 * sign)) -
					strike * (decimal)(expRate * NormalDistr(D2(d1, deviation, timeToExp) * sign))) * sign;
		}

		/// <summary>
		/// To calculate the option delta.
		/// </summary>
		/// <param name="optionType">Option type.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <param name="d1">The d1 parameter of the option fulfilment probability estimating.</param>
		/// <returns>Option delta.</returns>
		public static decimal Delta(OptionTypes optionType, decimal assetPrice, double d1)
		{
			var delta = (decimal)NormalDistr(d1);

			if (optionType == OptionTypes.Put)
				delta -= 1;

			return delta;
		}

		/// <summary>
		/// To calculate the option gamma.
		/// </summary>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="timeToExp">The option period before the expiration.</param>
		/// <param name="d1">The d1 parameter of the option fulfilment probability estimating.</param>
		/// <returns>Option gamma.</returns>
		public static decimal Gamma(decimal assetPrice, decimal deviation, double timeToExp, double d1)
		{
			if (deviation == 0)
				return 0;
			//throw new ArgumentOutOfRangeException(nameof(deviation), deviation, "Стандартное отклонение имеет недопустимое значение.");

			if (assetPrice == 0)
				return 0;

			return (decimal)InvertD1(d1) / (assetPrice * deviation * (decimal)timeToExp.Sqrt());
		}

		/// <summary>
		/// To calculate the option vega.
		/// </summary>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <param name="timeToExp">The option period before the expiration.</param>
		/// <param name="d1">The d1 parameter of the option fulfilment probability estimating.</param>
		/// <returns>Option vega.</returns>
		public static decimal Vega(decimal assetPrice, double timeToExp, double d1)
		{
			return assetPrice * (decimal)(0.01 * InvertD1(d1) * timeToExp.Sqrt());
		}

		/// <summary>
		/// To calculate the option theta.
		/// </summary>
		/// <param name="optionType">Option type.</param>
		/// <param name="strike">The strike price.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <param name="riskFree">The risk free interest rate.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="timeToExp">The option period before the expiration.</param>
		/// <param name="d1">The d1 parameter of the option fulfilment probability estimating.</param>
		/// <param name="daysInYear">Days per year.</param>
		/// <returns>Option theta.</returns>
		public static decimal Theta(OptionTypes optionType, decimal strike, decimal assetPrice, decimal riskFree, decimal deviation, double timeToExp, double d1, decimal daysInYear = 365)
		{
			var nd1 = InvertD1(d1);

			var expRate = ExpRate(riskFree, timeToExp);

			var sign = optionType == OptionTypes.Call ? 1 : -1;

			return
				(-(assetPrice * deviation * (decimal)nd1) / (2 * (decimal)timeToExp.Sqrt()) -
				sign * (strike * riskFree * (decimal)(expRate * NormalDistr(sign * D2(d1, deviation, timeToExp))))) / daysInYear;
		}

		/// <summary>
		/// To calculate the option rho.
		/// </summary>
		/// <param name="optionType">Option type.</param>
		/// <param name="strike">The strike price.</param>
		/// <param name="assetPrice">Underlying asset price.</param>
		/// <param name="riskFree">The risk free interest rate.</param>
		/// <param name="deviation">Standard deviation.</param>
		/// <param name="timeToExp">The option period before the expiration.</param>
		/// <param name="d1">The d1 parameter of the option fulfilment probability estimating.</param>
		/// <returns>Option rho.</returns>
		public static decimal Rho(OptionTypes optionType, decimal strike, decimal assetPrice, decimal riskFree, decimal deviation, double timeToExp, double d1)
		{
			var expRate = ExpRate(riskFree, timeToExp);

			var sign = optionType == OptionTypes.Call ? 1 : -1;

			return sign * (0.01m * strike * (decimal)(timeToExp * expRate * NormalDistr(sign * D2(d1, deviation, timeToExp))));
		}

		/// <summary>
		/// To calculate the implied volatility.
		/// </summary>
		/// <param name="premium">The option premium.</param>
		/// <param name="getPremium">To calculate the premium by volatility.</param>
		/// <returns>The implied volatility. If the value is equal to <see langword="null" />, then the value calculation currently is impossible.</returns>
		public static decimal? ImpliedVolatility(decimal premium, Func<decimal, decimal?> getPremium)
		{
			if (getPremium == null)
				throw new ArgumentNullException(nameof(getPremium));

			const decimal min = 0.00001m;

			var deviation = min;

			//Если Премия оказывается меньше чем премия с нулевой волатильностью, то выходим
			if (premium <= getPremium(deviation))
				return null;

			var high = 2m;
			var low = 0m;

			while ((high - low) > min)
			{
				deviation = (high + low) / 2;

				if (getPremium(deviation) > premium)
					high = deviation;
				else
					low = deviation;
			}

			return ((high + low) / 2) * 100;
		}

		private static double NormalDistr(double x)
		{
			return _normalDistribution.CumulativeDistribution(x);
		}
	}
}