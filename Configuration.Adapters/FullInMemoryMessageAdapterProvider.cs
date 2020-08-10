namespace StockSharp.Configuration
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.AlfaDirect;
	using StockSharp.AlorHistory;
	using StockSharp.AlphaVantage;
	using StockSharp.BarChart;
	using StockSharp.Bibox;
	using StockSharp.Binance;
	using StockSharp.Bitalong;
	using StockSharp.Bitbank;
	using StockSharp.Bitexbook;
	using StockSharp.Bitfinex;
	using StockSharp.Bithumb;
	using StockSharp.BitMax;
	using StockSharp.Bitmex;
	using StockSharp.BitStamp;
	using StockSharp.Bittrex;
	using StockSharp.BitZ;
	//using StockSharp.Blackwood;
	using StockSharp.Btce;
	using StockSharp.BW;
	using StockSharp.Cex;
	using StockSharp.Coinbase;
	using StockSharp.CoinBene;
	using StockSharp.CoinCap;
	using StockSharp.Coincheck;
	using StockSharp.CoinEx;
	using StockSharp.CoinExchange;
	using StockSharp.CoinHub;
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
	using StockSharp.FatBTC;
	using StockSharp.Finam;
	using StockSharp.FinViz;
	using StockSharp.Fix;
	using StockSharp.Fxcm;
	using StockSharp.Gdax;
	using StockSharp.Google;
	using StockSharp.Gopax;
	using StockSharp.HitBtc;
	using StockSharp.Hotbit;
	using StockSharp.Huobi;
	using StockSharp.Idax;
	using StockSharp.IEX;
	using StockSharp.InteractiveBrokers;
	using StockSharp.IQFeed;
	using StockSharp.ITCH;
	using StockSharp.Kraken;
	using StockSharp.Kucoin;
	using StockSharp.LATOKEN;
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
	//using StockSharp.OpenECry;
	using StockSharp.Plaza;
	using StockSharp.Poloniex;
	using StockSharp.PrizmBit;
	using StockSharp.Quandl;
	//using StockSharp.QuantHouse;
	//using StockSharp.Quik;
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
	using StockSharp.Upbit;
	using StockSharp.Xignite;
	using StockSharp.Yahoo;
	using StockSharp.Yobit;
	using StockSharp.Zaif;
	using StockSharp.ZB;
	using StockSharp.DigitexFutures;

	/// <summary>
	/// In memory configuration message adapter's provider.
	/// </summary>
	public class FullInMemoryMessageAdapterProvider : InMemoryMessageAdapterProvider
	{
		/// <summary>
		/// Initialize <see cref="FullInMemoryMessageAdapterProvider"/>.
		/// </summary>
		/// <param name="currentAdapters">All currently available adapters.</param>
		public FullInMemoryMessageAdapterProvider(IEnumerable<IMessageAdapter> currentAdapters)
			: base(currentAdapters)
		{
		}

		/// <inheritdoc />
		protected override IEnumerable<Type> GetAdapters()
		{
			var adapters = new HashSet<Type>(base.GetAdapters());

			foreach (var func in _standardAdapters.Value)
			{
				try
				{
					var type = func();

					//if (type == typeof(QuikDdeAdapter) || type == typeof(QuikTrans2QuikAdapter))
					//	adapters.Remove(type);
					//else
					adapters.Add(type);
				}
				catch (Exception e)
				{
					e.LogError();
				}
			}

			return adapters;
		}

		private static readonly Lazy<Func<Type>[]> _standardAdapters = new Lazy<Func<Type>[]>(() => new[]
		{
			(Func<Type>)(() => typeof(AlfaDirectMessageAdapter)),
			() => typeof(BarChartMessageAdapter),
			() => typeof(BitStampMessageAdapter),
			//() => typeof(BlackwoodMessageAdapter),
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
			//() => typeof(OpenECryMessageAdapter),
			() => typeof(PlazaMessageAdapter),
			() => typeof(LuaFixTransactionMessageAdapter),
			() => typeof(LuaFixMarketDataMessageAdapter),
			//() => typeof(QuikTrans2QuikAdapter),
			//() => typeof(QuikDdeAdapter),
			() => typeof(RithmicMessageAdapter),
			() => typeof(RssMessageAdapter),
			() => typeof(SmartComMessageAdapter),
			() => typeof(SterlingMessageAdapter),
			() => typeof(TransaqMessageAdapter),
			() => typeof(TwimeMessageAdapter),
			() => typeof(SpbExMessageAdapter),
			() => typeof(FxcmMessageAdapter),
			//() => typeof(QuantFeedMessageAdapter),
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
			() => typeof(UpbitMessageAdapter),
			() => typeof(CoinExMessageAdapter),
			() => typeof(FatBtcMessageAdapter),
			() => typeof(LatokenMessageAdapter),
			() => typeof(GopaxMessageAdapter),
			() => typeof(HotbitMessageAdapter),
			() => typeof(CoinHubMessageAdapter),
			() => typeof(BitalongMessageAdapter),
			() => typeof(PrizmBitMessageAdapter),
			() => typeof(DigitexFuturesMessageAdapter),
		});

		/// <inheritdoc />
		public override IMessageAdapter CreateTransportAdapter(IdGenerator transactionIdGenerator) => new FixMessageAdapter(transactionIdGenerator);
	}
}