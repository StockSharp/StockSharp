namespace StockSharp.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Security;

	using Ecng.Common;
	using Ecng.Reflection;

	using StockSharp.AlfaDirect;
	using StockSharp.AlorHistory;
	using StockSharp.AlphaVantage;
	using StockSharp.BarChart;
	using StockSharp.Bibox;
	using StockSharp.Binance;
	using StockSharp.Bitbank;
	using StockSharp.Bitexbook;
	using StockSharp.Bitfinex;
	using StockSharp.Bithumb;
	using StockSharp.BitMax;
	using StockSharp.Bitmex;
	using StockSharp.BitStamp;
	using StockSharp.Bittrex;
	using StockSharp.BitZ;
	using StockSharp.Blackwood;
	using StockSharp.Btce;
	using StockSharp.BW;
	using StockSharp.Cex;
	using StockSharp.Coinbase;
	using StockSharp.CoinBene;
	using StockSharp.CoinCap;
	using StockSharp.Coincheck;
	using StockSharp.CoinExchange;
	using StockSharp.Coinigy;
	using StockSharp.Cqg.Com;
	using StockSharp.Cqg.Continuum;
	using StockSharp.Cryptopia;
	using StockSharp.CSV;
	using StockSharp.Deribit;
	using StockSharp.Digifinex;
	using StockSharp.DukasCopy;
	using StockSharp.ETrade;
	using StockSharp.Exmo;
	using StockSharp.Finam;
	using StockSharp.FinViz;
	using StockSharp.Fix;
	using StockSharp.Fxcm;
	using StockSharp.Gdax;
	using StockSharp.Google;
	using StockSharp.HitBtc;
	using StockSharp.Huobi;
	using StockSharp.Idax;
	using StockSharp.IEX;
	using StockSharp.InteractiveBrokers;
	using StockSharp.IQFeed;
	using StockSharp.ITCH;
	using StockSharp.Kraken;
	using StockSharp.Kucoin;
	using StockSharp.LBank;
	using StockSharp.Liqui;
	using StockSharp.LiveCoin;
	using StockSharp.LMAX;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Mfd;
	using StockSharp.Micex;
	using StockSharp.Oanda;
	using StockSharp.Okcoin;
	using StockSharp.Okex;
	using StockSharp.OpenECry;
	using StockSharp.Plaza;
	using StockSharp.Poloniex;
	using StockSharp.Quandl;
	using StockSharp.QuantHouse;
	using StockSharp.Quik;
	using StockSharp.Quik.Lua;
	using StockSharp.Quoinex;
	using StockSharp.Rithmic;
	using StockSharp.Rss;
	using StockSharp.SmartCom;
	using StockSharp.SpbEx;
	using StockSharp.Sterling;
	using StockSharp.TradeOgre;
	using StockSharp.Tradier;
	using StockSharp.Transaq;
	using StockSharp.Twime;
	using StockSharp.UkrExh;
	using StockSharp.Xignite;
	using StockSharp.Yahoo;
	using StockSharp.Yobit;
	using StockSharp.Zaif;
	using StockSharp.ZB;

	/// <summary>
	/// In memory configuration message adapter's provider.
	/// </summary>
	public class InMemoryMessageAdapterProvider : IMessageAdapterProvider
	{
		/// <summary>
		/// Initialize <see cref="InMemoryMessageAdapterProvider"/>.
		/// </summary>
		/// <param name="currentAdapters">All currently available adapters.</param>
		public InMemoryMessageAdapterProvider(IEnumerable<IMessageAdapter> currentAdapters)
		{
			CurrentAdapters = currentAdapters ?? throw new ArgumentNullException(nameof(currentAdapters));

			var idGenerator = new IncrementalIdGenerator();
			PossibleAdapters = Adapters.Select(t => t.CreateAdapter(idGenerator)).ToArray();
		}

		/// <inheritdoc />
		public virtual IEnumerable<IMessageAdapter> CurrentAdapters { get; }

		/// <inheritdoc />
		public virtual IEnumerable<IMessageAdapter> PossibleAdapters { get; }

		/// <inheritdoc />
		public virtual IEnumerable<IMessageAdapter> CreateStockSharpAdapters(IdGenerator transactionIdGenerator, string login, SecureString password) => Enumerable.Empty<IMessageAdapter>();

		private static readonly Lazy<Func<Type>[]> _standardAdapters = new Lazy<Func<Type>[]>(() => new[]
		{
			(Func<Type>)(() => typeof(AlfaDirectMessageAdapter)),
			() => typeof(BarChartMessageAdapter),
			() => typeof(BitStampMessageAdapter),
			() => typeof(BlackwoodMessageAdapter),
			() => typeof(BtceMessageAdapter),
			() => typeof(CqgComMessageAdapter),
			() => typeof(CqgContinuumMessageAdapter),
			() => typeof(ETradeMessageAdapter),
			() => typeof(FixMessageAdapter),
			() => typeof(FastMessageAdapter),
			() => typeof(InteractiveBrokersMessageAdapter),
			() => typeof(IQFeedMessageAdapter),
			() => typeof(ItchMessageAdapter),
			() => typeof(LmaxMessageAdapter),
			() => typeof(MicexMessageAdapter),
			() => typeof(OandaMessageAdapter),
			() => typeof(OpenECryMessageAdapter),
			() => typeof(PlazaMessageAdapter),
			() => typeof(LuaFixTransactionMessageAdapter),
			() => typeof(LuaFixMarketDataMessageAdapter),
			() => typeof(QuikTrans2QuikAdapter),
			() => typeof(QuikDdeAdapter),
			() => typeof(RithmicMessageAdapter),
			() => typeof(RssMessageAdapter),
			() => typeof(SmartComMessageAdapter),
			() => typeof(SterlingMessageAdapter),
			() => typeof(TransaqMessageAdapter),
			() => typeof(TwimeMessageAdapter),
			() => typeof(SpbExMessageAdapter),
			() => typeof(FxcmMessageAdapter),
			() => typeof(QuantFeedMessageAdapter),
			() => typeof(BitfinexMessageAdapter),
			() => typeof(BithumbMessageAdapter),
			() => typeof(BittrexMessageAdapter),
			() => typeof(CoinbaseMessageAdapter),
			() => typeof(CoincheckMessageAdapter),
			() => typeof(GdaxMessageAdapter),
			() => typeof(HitBtcMessageAdapter),
			() => typeof(KrakenMessageAdapter),
			() => typeof(OkcoinMessageAdapter),
			() => typeof(PoloniexMessageAdapter),
			() => typeof(BinanceMessageAdapter),
			() => typeof(BitexbookMessageAdapter),
			() => typeof(BitmexMessageAdapter),
			() => typeof(CexMessageAdapter),
			() => typeof(CoinExchangeMessageAdapter),
			() => typeof(CryptopiaMessageAdapter),
			() => typeof(DeribitMessageAdapter),
			() => typeof(ExmoMessageAdapter),
			() => typeof(HuobiMessageAdapter),
			() => typeof(KucoinMessageAdapter),
			() => typeof(LiquiMessageAdapter),
			() => typeof(LiveCoinMessageAdapter),
			() => typeof(OkexMessageAdapter),
			() => typeof(YobitMessageAdapter),
			() => typeof(AlphaVantageMessageAdapter),
			() => typeof(IEXMessageAdapter),
			() => typeof(QuoinexMessageAdapter),
			() => typeof(BitbankMessageAdapter),
			() => typeof(ZaifMessageAdapter),
			() => typeof(DigifinexMessageAdapter),
			() => typeof(IdaxMessageAdapter),
			() => typeof(TradeOgreMessageAdapter),
			() => typeof(CoinCapMessageAdapter),
			() => typeof(CoinigyMessageAdapter),
			() => typeof(LBankMessageAdapter),
			() => typeof(BitMaxMessageAdapter),
			() => typeof(BWMessageAdapter),
			() => typeof(BiboxMessageAdapter),
			() => typeof(CoinBeneMessageAdapter),
			() => typeof(BitZMessageAdapter),
			() => typeof(ZBMessageAdapter),
			() => typeof(TradierMessageAdapter),
			() => typeof(DukasCopyMessageAdapter),
			() => typeof(FinamMessageAdapter),
			() => typeof(AlorHistoryMessageAdapter),
			() => typeof(MfdMessageAdapter),
			() => typeof(QuandlMessageAdapter),
			() => typeof(XigniteMessageAdapter),
			() => typeof(YahooMessageAdapter),
			() => typeof(GoogleMessageAdapter),
			() => typeof(FinVizMessageAdapter),
			() => typeof(UkrExhMessageAdapter),
			() => typeof(CSVMessageAdapter),
		});
		
		private static readonly SyncObject _adaptersLock = new SyncObject();
		private static Type[] _adapters;

		/// <summary>
		/// All available adapters.
		/// </summary>
		private static IEnumerable<Type> Adapters
		{
			get
			{
				lock (_adaptersLock)
				{
					if (_adapters == null)
					{
						var exceptions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
						{
							"StockSharp.Alerts",
							"StockSharp.Algo",
							"StockSharp.Algo.History",
							"StockSharp.Algo.Strategies",
							"StockSharp.BusinessEntities",
							"StockSharp.Community",
							"StockSharp.Configuration",
							"StockSharp.Licensing",
							"StockSharp.Localization",
							"StockSharp.Logging",
							"StockSharp.Messages",
							"StockSharp.Xaml",
							"StockSharp.Xaml.Actipro",
							"StockSharp.Xaml.Charting",
							"StockSharp.Xaml.Diagram",
							"StockSharp.Studio.Core",
							"StockSharp.Studio.Controls",
							"StockSharp.QuikLua",
						};

						var adapters = new List<Type>();

						foreach (var func in _standardAdapters.Value)
						{
							try
							{
								var type = func();

								exceptions.Add(type.Assembly.GetName().Name);

								if (type == typeof(QuikDdeAdapter) || type == typeof(QuikTrans2QuikAdapter))
									continue;

								adapters.Add(type);
							}
							catch (Exception e)
							{
								e.LogError();
							}
						}

						var assemblies = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll").Where(p =>
						{
							var name = Path.GetFileNameWithoutExtension(p);
							return !exceptions.Contains(name) && name.StartsWithIgnoreCase("StockSharp.");
						});

						foreach (var assembly in assemblies)
						{
							if (!assembly.IsAssembly())
								continue;

							try
							{
								var asm = Assembly.Load(AssemblyName.GetAssemblyName(assembly));

								adapters.AddRange(asm
									.GetTypes()
									.Where(t => typeof(IMessageAdapter).IsAssignableFrom(t) && !t.IsAbstract)
									.ToArray());
							}
							catch (Exception e)
							{
								e.LogError();
							}
						}

						_adapters = adapters.ToArray();
					}
				}

				return _adapters;
			}
		}
	}
}