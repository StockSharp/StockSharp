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
	/// Вспомогательный класс для работы с производными финансовыми инструментами (деривативами).
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
		/// Получить базовый актив по деривативу.
		/// </summary>
		/// <param name="derivative">Дериватив.</param>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <returns>Базовый актив.</returns>
		public static Security GetUnderlyingAsset(this Security derivative, ISecurityProvider provider)
		{
			if (derivative == null)
				throw new ArgumentNullException("derivative");

			if (provider == null)
				throw new ArgumentNullException("provider");

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
		/// Отфильтровать опционы по страйку <see cref="Security.Strike"/>.
		/// </summary>
		/// <param name="options">Опционы, которые необходимо отфильтровать.</param>
		/// <param name="strike">Цена страйка.</param>
		/// <returns>Отфильтрованные опционы.</returns>
		public static IEnumerable<Security> Filter(this IEnumerable<Security> options, decimal strike)
		{
			return options.Where(o => o.Strike == strike);
		}

		/// <summary>
		/// Отфильтровать опционы по типу <see cref="Security.OptionType"/>.
		/// </summary>
		/// <param name="options">Опционы, которые необходимо отфильтровать.</param>
		/// <param name="type">Тип опциона.</param>
		/// <returns>Отфильтрованные опционы.</returns>
		public static IEnumerable<Security> Filter(this IEnumerable<Security> options, OptionTypes type)
		{
			return options.Where(o => o.OptionType == type);
		}

		/// <summary>
		/// Отфильтровать инструменты по базовому активу.
		/// </summary>
		/// <param name="securities">Инструменты, которые необходимо отфильтровать.</param>
		/// <param name="asset">Базовый актив.</param>
		/// <returns>Отфильтрованные инструменты.</returns>
		public static IEnumerable<Security> FilterByUnderlying(this IEnumerable<Security> securities, Security asset)
		{
			if (asset == null)
				throw new ArgumentNullException("asset");

			return securities.Where(s => s.UnderlyingSecurityId == asset.Id);
		}

		/// <summary>
		/// Отфильтровать инструменты по дате экспирации <see cref="Security.ExpiryDate"/>.
		/// </summary>
		/// <param name="securities">Инструменты, которые необходимо отфильтровать.</param>
		/// <param name="expirationDate">Дата экспирации.</param>
		/// <returns>Отфильтрованные инструменты.</returns>
		public static IEnumerable<Security> Filter(this IEnumerable<Security> securities, DateTime? expirationDate)
		{
			if (expirationDate == null)
				return securities;

			return securities.Where(s => s.ExpiryDate == expirationDate);
		}

		/// <summary>
		/// Получить деривативы по базовому активу.
		/// </summary>
		/// <param name="asset">Базовый актив.</param>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <param name="expirationDate">Дата экспирации.</param>
		/// <returns>Список из деривативов.</returns>
		/// <remarks>Возвращает пустой список, если деривативов не найдено.</remarks>
		public static IEnumerable<Security> GetDerivatives(this Security asset, ISecurityProvider provider, DateTimeOffset? expirationDate = null)
		{
			return provider.Lookup(new Security
			{
				UnderlyingSecurityId = asset.Id,
				ExpiryDate = expirationDate,
			});
		}

		/// <summary>
		/// Получить базовый актив.
		/// </summary>
		/// <param name="derivative">Дериватив.</param>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <returns>Базовый актив.</returns>
		public static Security GetAsset(this Security derivative, ISecurityProvider provider)
		{
			var asset = provider.LookupById(derivative.UnderlyingSecurityId);

			if (asset == null)
				throw new ArgumentException(LocalizedStrings.Str705Params.Put(derivative));

			return asset;
		}

		/// <summary>
		/// Поменять тип опциона на противоположное.
		/// </summary>
		/// <param name="type">Первоначальное значение.</param>
		/// <returns>Противоположное значение.</returns>
		public static OptionTypes Invert(this OptionTypes type)
		{
			return type == OptionTypes.Call ? OptionTypes.Put : OptionTypes.Call;
		}

		/// <summary>
		/// Получить противоположный опцион (для Call получить Put, для Put получить Call).
		/// </summary>
		/// <param name="option">Опцион.</param>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <returns>Противоположный опцион.</returns>
		public static Security GetOppositeOption(this Security option, ISecurityProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");

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
				throw new ArgumentException(LocalizedStrings.Str706Params.Put(option.Id), "option");

			return oppositeOption;
		}

		/// <summary>
		/// Получить Call для базового фьючерса.
		/// </summary>
		/// <param name="future">Базовый фьючерс.</param>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <param name="strike">Страйк.</param>
		/// <param name="expirationDate">Дата экспирации опциона.</param>
		/// <returns>Опцион Call.</returns>
		public static Security GetCall(this Security future, ISecurityProvider provider, decimal strike, DateTimeOffset expirationDate)
		{
			return future.GetOption(provider, strike, expirationDate, OptionTypes.Call);
		}

		/// <summary>
		/// Получить Put для базового фьючерса.
		/// </summary>
		/// <param name="future">Базовый фьючерс.</param>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <param name="strike">Страйк.</param>
		/// <param name="expirationDate">Дата экспирации опциона.</param>
		/// <returns>Опцион Put.</returns>
		public static Security GetPut(this Security future, ISecurityProvider provider, decimal strike, DateTimeOffset expirationDate)
		{
			return future.GetOption(provider, strike, expirationDate, OptionTypes.Put);
		}

		/// <summary>
		/// Получить опцион для базового фьючерса.
		/// </summary>
		/// <param name="future">Базовый фьючерс.</param>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <param name="strike">Страйк.</param>
		/// <param name="expirationDate">Дата экспирации опционов.</param>
		/// <param name="optionType">Тип опциона.</param>
		/// <returns>Опцион.</returns>
		public static Security GetOption(this Security future, ISecurityProvider provider, decimal strike, DateTimeOffset expirationDate, OptionTypes optionType)
		{
			if (future == null)
				throw new ArgumentNullException("future");

			if (provider == null)
				throw new ArgumentNullException("provider");

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
				throw new ArgumentException(LocalizedStrings.Str707Params.Put(future.Id), "future");

			return option;
		}

		/// <summary>
		/// Получить центральный страйк.
		/// </summary>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		/// <param name="expirationDate">Дата экспирации опционов.</param>
		/// <param name="optionType">Тип опциона.</param>
		/// <returns>Центральный страйк.</returns>
		public static Security GetCentralStrike(this Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, DateTime expirationDate, OptionTypes optionType)
		{
			return underlyingAsset.GetCentralStrike(dataProvider, underlyingAsset.GetDerivatives(securityProvider, expirationDate).Filter(optionType));
		}

		/// <summary>
		/// Получить центральный страйк.
		/// </summary>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="provider">Поставщик маркет-данных.</param>
		/// <param name="allStrikes">Все страйки.</param>
		/// <returns>Центральный страйк. Если невозможно получить текущую рыночную цену актива, то будет возвращено <see langword="null"/>.</returns>
		public static Security GetCentralStrike(this Security underlyingAsset, IMarketDataProvider provider, IEnumerable<Security> allStrikes)
		{
			var assetPrice = underlyingAsset.GetCurrentPrice(provider);

			return assetPrice == null
				? null
				: allStrikes.OrderBy(s => Math.Abs((decimal)(s.Strike - assetPrice))).FirstOrDefault();
		}

		/// <summary>
		/// Получить размер шага страйка.
		/// </summary>
		/// <param name="provider">Поставщик информации об инструментах.</param>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="expirationDate">Дата экспирации опционов (для указания конкретной серии).</param>
		/// <returns>Размер шага страйка.</returns>
		public static decimal GetStrikeStep(this Security underlyingAsset, ISecurityProvider provider, DateTimeOffset? expirationDate = null)
		{
			var group = underlyingAsset
				.GetDerivatives(provider, expirationDate)
				.Filter(OptionTypes.Call)
				.GroupBy(s => s.ExpiryDate)
				.FirstOrDefault();

			if (group == null)
				throw new InvalidOperationException(LocalizedStrings.Str708);

			var orderedStrikes = group.OrderBy(s => s.Strike).Take(2).ToArray();
			return orderedStrikes[1].Strike - orderedStrikes[0].Strike;
		}

		/// <summary>
		/// Получить опционы вне денег (OTM).
		/// </summary>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		/// <returns>Опционы вне денег.</returns>
		public static IEnumerable<Security> GetOutOfTheMoney(this Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			return underlyingAsset.GetOutOfTheMoney(dataProvider, underlyingAsset.GetDerivatives(securityProvider));
		}

		/// <summary>
		/// Получить опционы вне денег (OTM).
		/// </summary>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="provider">Поставщик маркет-данных.</param>
		/// <param name="allStrikes">Все страйки.</param>
		/// <returns>Опционы вне денег.</returns>
		public static IEnumerable<Security> GetOutOfTheMoney(this Security underlyingAsset, IMarketDataProvider provider, IEnumerable<Security> allStrikes)
		{
			if (underlyingAsset == null)
				throw new ArgumentNullException("underlyingAsset");

			var cs = underlyingAsset.GetCentralStrike(provider, allStrikes);

			return allStrikes.Where(s => s.OptionType == OptionTypes.Call ? s.Strike < cs.Strike : s.Strike > cs.Strike);
		}

		/// <summary>
		/// Получить опционы в деньгах (ITM).
		/// </summary>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		/// <returns>Опционы в деньгах.</returns>
		public static IEnumerable<Security> GetInTheMoney(this Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			return underlyingAsset.GetInTheMoney(dataProvider, underlyingAsset.GetDerivatives(securityProvider));
		}

		/// <summary>
		/// Получить опционы в деньгах (ITM).
		/// </summary>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="provider">Поставщик маркет-данных.</param>
		/// <param name="allStrikes">Все страйки.</param>
		/// <returns>Опционы в деньгах.</returns>
		public static IEnumerable<Security> GetInTheMoney(this Security underlyingAsset, IMarketDataProvider provider, IEnumerable<Security> allStrikes)
		{
			if (underlyingAsset == null)
				throw new ArgumentNullException("underlyingAsset");

			var cs = underlyingAsset.GetCentralStrike(provider, allStrikes);

			return allStrikes.Where(s => s.OptionType == OptionTypes.Call ? s.Strike > cs.Strike : s.Strike < cs.Strike);
		}

		/// <summary>
		/// Получить опционы на деньгах (ATM).
		/// </summary>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		/// <returns>Опционы на деньгах.</returns>
		public static IEnumerable<Security> GetAtTheMoney(this Security underlyingAsset, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			return underlyingAsset.GetAtTheMoney(dataProvider, underlyingAsset.GetDerivatives(securityProvider));
		}

		/// <summary>
		/// Получить опционы на деньгах (ATM).
		/// </summary>
		/// <param name="underlyingAsset">Базовый актив.</param>
		/// <param name="provider">Поставщик маркет-данных.</param>
		/// <param name="allStrikes">Все страйки.</param>
		/// <returns>Опционы на деньгах.</returns>
		public static IEnumerable<Security> GetAtTheMoney(this Security underlyingAsset, IMarketDataProvider provider, IEnumerable<Security> allStrikes)
		{
			if (underlyingAsset == null)
				throw new ArgumentNullException("underlyingAsset");

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
		/// Получить внутреннюю стоимость опциона.
		/// </summary>
		/// <param name="option">Опцион.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		/// <returns>Внутренняя стоимость. Если невозможно получить текущую рыночную цену актива, то будет возвращено <see langword="null"/>.</returns>
		public static decimal? GetIntrinsicValue(this Security option, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			if (securityProvider == null)
				throw new ArgumentNullException("securityProvider");
			
			option.CheckOption();

			var assetPrice = option.GetUnderlyingAsset(securityProvider).GetCurrentPrice(dataProvider);

			if (assetPrice == null)
				return null;

			return ((decimal)((option.OptionType == OptionTypes.Call) ? assetPrice - option.Strike : option.Strike - assetPrice)).Max(0);
		}

		/// <summary>
		/// Получить временную стоимость опциона.
		/// </summary>
		/// <param name="option">Опцион.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		/// <returns>Временная стоимость. Если невозможно получить текущую рыночную цену актива, то будет возвращено <see langword="null"/>.</returns>
		public static decimal? GetTimeValue(this Security option, ISecurityProvider securityProvider, IMarketDataProvider dataProvider)
		{
			if (securityProvider == null)
				throw new ArgumentNullException("securityProvider");

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
				throw new ArgumentException(LocalizedStrings.Str709Params.Put(security.Id), "security");

			var expDate = security.ExpiryDate.Value;

			if (expDate.TimeOfDay == TimeSpan.Zero)
				expDate += security.CheckExchangeBoard().ExpiryTime;

			return expDate;
		}

		/// <summary>
		/// Проверить, закончил ли действие инструмент.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="currentTime">Текущее время.</param>
		/// <returns><see langword="true"/>, если инструмент закончил свое действие.</returns>
		public static bool IsExpired(this Security security, DateTime currentTime)
		{
			return security.GetExpirationTime() >= currentTime;
		}

		/// <summary>
		/// Получить из названия опциона его информацию (базовый актив, страйк, дата экспирации и т.д.).
		/// </summary>
		/// <param name="optionName">Название опциона.</param>
		/// <returns>Информация об опционе.</returns>
		public static Security GetOptionInfo(this string optionName)
		{
			if (optionName.IsEmpty())
				throw new ArgumentNullException("optionName");

			var matches = _optionNameRegex.Matches(optionName);

			if (matches.Count == 1)
			{
				var groups = matches[0].Groups;

				if (groups.Count == 7)
				{
					return new Security
					{
						UnderlyingSecurityId = groups["code"].Value,
						ExpiryDate = groups["expiryDate"].Value.ToDateTime("ddMMyy"),
						OptionType = groups["optionType"].Value == "C" ? OptionTypes.Call : OptionTypes.Put,
						Strike = decimal.Parse(groups["strike"].Value, CultureInfo.InvariantCulture),
					};
				}
			}

			return null;
		}

		/// <summary>
		/// Получить из названия фьючерса его информацию (базовый актив, дата экспирации и т.д.).
		/// </summary>
		/// <param name="futureName">Название фьючерса.</param>
		/// <param name="optionCode">Код опциона.</param>
		/// <returns>Информация о фьючерсе.</returns>
		public static SecurityMessage GetFutureInfo(this string futureName, string optionCode)
		{
			if (futureName.IsEmpty())
				throw new ArgumentNullException("futureName");

			if (optionCode.IsEmpty())
				throw new ArgumentNullException("optionCode");

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
				ExpiryDate = new DateTime(2000 + yearStr.To<int>(), month, 1),
				Name = futureName,
			};
		}

		/// <summary>
		/// Создать стакан волатильности из обычного стакана.
		/// </summary>
		/// <param name="depth">Стакан, котировки которого будут переведены в котировки с волатильностью.</param>
		/// <param name="securityProvider">Поставщик информации об инструментах.</param>
		/// <param name="dataProvider">Поставщик маркет-данных.</param>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="riskFree">Безрисковая процентная ставка.</param>
		/// <param name="dividend">Размер дивиденда по акциям.</param>
		/// <returns>Стакан волатильности.</returns>
		public static MarketDepth ImpliedVolatility(this MarketDepth depth, ISecurityProvider securityProvider, IMarketDataProvider dataProvider, DateTime currentTime, decimal riskFree = 0, decimal dividend = 0)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			return depth.ImpliedVolatility(new BlackScholes(depth.Security, securityProvider, dataProvider) { RiskFree = riskFree, Dividend = dividend }, currentTime);
		}

		/// <summary>
		/// Создать стакан волатильности из обычного стакана.
		/// </summary>
		/// <param name="depth">Стакан, котировки которого будут переведены в котировки с волатильностью.</param>
		/// <param name="model">Модель расчета значений "греков" по формуле Блэка-Шоулза.</param>
		/// <param name="currentTime">Текущее время.</param>
		/// <returns>Стакан волатильности.</returns>
		public static MarketDepth ImpliedVolatility(this MarketDepth depth, BlackScholes model, DateTimeOffset currentTime)
		{
			if (depth == null)
				throw new ArgumentNullException("depth");

			if (model == null)
				throw new ArgumentNullException("model");

			Func<Quote, Quote> convert = quote =>
			{
				quote = quote.Clone();
				quote.Price = model.ImpliedVolatility(currentTime, quote.Price);
				return quote;
			};

			return new MarketDepth(depth.Security).Update(depth.Bids.Select(convert), depth.Asks.Select(convert), true, depth.LastChangeTime);
		}

		/// <summary>
		/// Получить период опциона до экспирации.
		/// </summary>
		/// <param name="expirationTime">Время экспирации опциона.</param>
		/// <param name="currentTime">Текущее время.</param>
		/// <returns>Период опциона до экспирации.</returns>
		public static double GetExpirationTimeLine(DateTimeOffset expirationTime, DateTimeOffset currentTime)
		{
			return GetExpirationTimeLine(expirationTime, currentTime, TimeSpan.FromDays(365));
		}

		/// <summary>
		/// Получить период опциона до экспирации.
		/// </summary>
		/// <param name="expirationTime">Время экспирации опциона.</param>
		/// <param name="currentTime">Текущее время.</param>
		/// <param name="timeLine">Длина общего периода.</param>
		/// <returns>Период опциона до экспирации.</returns>
		public static double GetExpirationTimeLine(DateTimeOffset expirationTime, DateTimeOffset currentTime, TimeSpan timeLine)
		{
			var retVal = expirationTime - currentTime;

			if (retVal <= TimeSpan.Zero)
				throw new InvalidOperationException(LocalizedStrings.Str710Params.Put(expirationTime, currentTime));

			return (double)retVal.Ticks / timeLine.Ticks;
		}

		//private const int _dayInYear = 365; // Количество дней в году (расчет временного распада)

		private static double InvertD1(double d1)
		{
			// http://ru.wikipedia.org/wiki/Нормальное_распределение (сигма=1 и мю=0)
			return Math.Exp(-d1 * d1 / 2.0) / Math.Sqrt(2 * Math.PI);
		}

		/// <summary>
		/// Рассчитать временную экспоненту.
		/// </summary>
		/// <param name="riskFree">Безрисковая процентная ставка.</param>
		/// <param name="timeToExp">Период опциона до экспирации.</param>
		/// <returns>Временная экспонента.</returns>
		public static double ExpRate(decimal riskFree, double timeToExp)
		{
			return riskFree == 0 ? 1 : Math.Exp(-(double)riskFree * timeToExp);
		}

		/// <summary>
		/// Рассчитать параметр d1 определения вероятности исполнения опциона.
		/// </summary>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <param name="strike">Цена страйка.</param>
		/// <param name="riskFree">Безрисковая процентная ставка.</param>
		/// <param name="dividend">Размер дивиденда по акциям.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="timeToExp">Период опциона до экспирации.</param>
		/// <returns>Параметр d1 определения вероятности исполнения опциона.</returns>
		public static double D1(decimal assetPrice, decimal strike, decimal riskFree, decimal dividend, decimal deviation, double timeToExp)
		{
			if (deviation < 0)
				throw new ArgumentOutOfRangeException("deviation", deviation, LocalizedStrings.Str711);

			return (((double)assetPrice / (double)strike).Log() +
				(double)(riskFree - dividend + deviation * deviation / 2.0m) * timeToExp) / ((double)deviation * timeToExp.Sqrt());
		}

		/// <summary>
		/// Рассчитать параметр d2 определения вероятности исполнения опциона.
		/// </summary>
		/// <param name="d1">Параметр d1 определения вероятности исполнения опциона.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="timeToExp">Период опциона до экспирации.</param>
		/// <returns>Параметр d2 определения вероятности исполнения опциона.</returns>
		public static double D2(double d1, decimal deviation, double timeToExp)
		{
			return d1 - (double)deviation * timeToExp.Sqrt();
		}

		/// <summary>
		/// Рассчитать премию опциона.
		/// </summary>
		/// <param name="optionType">Тип опциона.</param>
		/// <param name="strike">Цена страйка.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <param name="riskFree">Безрисковая процентная ставка.</param>
		/// <param name="dividend">Размер дивиденда по акциям.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="timeToExp">Период опциона до экспирации.</param>
		/// <param name="d1">Параметр d1 определения вероятности исполнения опциона.</param>
		/// <returns>Премия опциона.</returns>
		public static decimal Premium(OptionTypes optionType, decimal strike, decimal assetPrice, decimal riskFree, decimal dividend, decimal deviation, double timeToExp, double d1)
		{
			var sign = (optionType == OptionTypes.Call) ? 1 : -1;

			var expDiv = ExpRate(dividend, timeToExp);
			var expRate = ExpRate(riskFree, timeToExp);

			return (assetPrice * (decimal)(expDiv * NormalDistr(d1 * sign)) -
					strike * (decimal)(expRate * NormalDistr(D2(d1, deviation, timeToExp) * sign))) * sign;
		}

		/// <summary>
		/// Рассчитать дельту опциона.
		/// </summary>
		/// <param name="optionType">Тип опциона.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <param name="d1">Параметр d1 определения вероятности исполнения опциона.</param>
		/// <returns>Дельта опциона.</returns>
		public static decimal Delta(OptionTypes optionType, decimal assetPrice, double d1)
		{
			var delta = (decimal)NormalDistr(d1);

			if (optionType == OptionTypes.Put)
				delta -= 1;

			return delta;
		}

		/// <summary>
		/// Рассчитать гамму опциона.
		/// </summary>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="timeToExp">Период опциона до экспирации.</param>
		/// <param name="d1">Параметр d1 определения вероятности исполнения опциона.</param>
		/// <returns>Гамма опциона.</returns>
		public static decimal Gamma(decimal assetPrice, decimal deviation, double timeToExp, double d1)
		{
			if (deviation == 0)
				return 0;
			//throw new ArgumentOutOfRangeException("deviation", deviation, "Стандартное отклонение имеет недопустимое значение.");

			if (assetPrice == 0)
				return 0;

			return (decimal)InvertD1(d1) / (assetPrice * deviation * (decimal)timeToExp.Sqrt());
		}

		/// <summary>
		/// Рассчитать вегу опциона.
		/// </summary>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <param name="timeToExp">Период опциона до экспирации.</param>
		/// <param name="d1">Параметр d1 определения вероятности исполнения опциона.</param>
		/// <returns>Вега опциона.</returns>
		public static decimal Vega(decimal assetPrice, double timeToExp, double d1)
		{
			return assetPrice * (decimal)(0.01 * InvertD1(d1) * timeToExp.Sqrt());
		}

		/// <summary>
		/// Рассчитать тету опциона.
		/// </summary>
		/// <param name="optionType">Тип опциона.</param>
		/// <param name="strike">Цена страйка.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <param name="riskFree">Безрисковая процентная ставка.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="timeToExp">Период опциона до экспирации.</param>
		/// <param name="d1">Параметр d1 определения вероятности исполнения опциона.</param>
		/// <param name="daysInYear">Дней в году.</param>
		/// <returns>Тета опциона.</returns>
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
		/// Рассчитать ро опциона.
		/// </summary>
		/// <param name="optionType">Тип опциона.</param>
		/// <param name="strike">Цена страйка.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <param name="riskFree">Безрисковая процентная ставка.</param>
		/// <param name="deviation">Стандартное отклонение.</param>
		/// <param name="timeToExp">Период опциона до экспирации.</param>
		/// <param name="d1">Параметр d1 определения вероятности исполнения опциона.</param>
		/// <returns>Ро опциона.</returns>
		public static decimal Rho(OptionTypes optionType, decimal strike, decimal assetPrice, decimal riskFree, decimal deviation, double timeToExp, double d1)
		{
			var expRate = ExpRate(riskFree, timeToExp);

			var sign = optionType == OptionTypes.Call ? 1 : -1;

			return sign * (0.01m * strike * (decimal)(timeToExp * expRate * NormalDistr(sign * D2(d1, deviation, timeToExp))));
		}

		/// <summary>
		/// Рассчитать подразумеваемую волатильность (Implied  Volatility).
		/// </summary>
		/// <param name="premium">Премия по опциону.</param>
		/// <param name="getPremium">Рассчитать премию по волатильности.</param>
		/// <returns>Подразумеваевая волатильность.</returns>
		public static decimal ImpliedVolatility(decimal premium, Func<decimal, decimal> getPremium)
		{
			if (getPremium == null)
				throw new ArgumentNullException("getPremium");

			const decimal min = 0.00001m;

			var deviation = min;

			//Если Премия оказывается меньше чем премия с нулевой волатильностью, то выходим
			if (premium <= getPremium(deviation))
				return 0;

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