<img src="./Media/SLogo.png" align="right" />

# [StockSharp - trading platform][1]

## **English** | [Русский](README.ru.md) | [中文](README.zh.md)

## <a href="https://doc.stocksharp.com/en" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4d6.svg" alt="Docs" height="40"/> Docs</a> <a href="https://stocksharp.com/en/products/download/" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4be.svg" alt="Download" height="40"/> Download</a> <a href="https://stocksharp.com/en/chat/" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4ac.svg" alt="Chat" height="40"/> Chat</a> <a href="https://www.youtube.com/@stocksharp"><img src="https://raw.githubusercontent.com/edent/SuperTinyIcons/master/images/svg/youtube.svg" alt="YouTube" height="40"/> YouTube</a>

## Introduction ##

**StockSharp** (**S#** for short) – is a **free** platform for trading on any market in the world (crypto exchanges, American, European, Asian, Russian, stocks, futures, options, Bitcoins, forex, etc.). You will be able to trade manually or automatically (algorithmic trading robots, conventional or HFT).

**Available connections**: Binance, MT4, MT5, FIX/FAST, PolygonIO, Trading Technologies, Alpaca Markets, BarChart, CQG, E*Trade, IQFeed, InteractiveBrokers, LMAX, MatLab, Oanda, FXCM, Rithmic, cTrader, DXtrade, BitStamp, Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, OKX, Coincheck, CEX.IO, BitMEX, YoBit, Livecoin, EXMO, Deribit, HTX, KuCoin, QuantFEED, Aster, edgeX, Ligther, Paradex, Hyperliquid and many others.

Connector source code and the full connector list are available in the [StockSharp Connectors repository](https://github.com/StockSharp/Connectors).

## [Designer][8]
<img src="./Media/Designer500.gif" align="left" />

**Designer** - a **free** universal algorithmic strategy application for easy strategy creation:
  - Visual designer to create strategies by mouse clicking
  - Embedded C# editor
  - Easy to create own indicators
  - Built-in debugger
  - Connections to the multiple electronic boards and brokers
  - All world platforms
  - Schema sharing with own team

## [Hydra][9]
<img src="./Media/Hydra500.gif" align="right" />

**Hydra** - **free** software to automatically load and store market data:
  - Supports many sources
  - High compression ratio
  - Any data type
  - Program access to stored data via API
  - Export to csv, excel, xml or database
  - Import from csv
  - Scheduled tasks
  - Auto-sync over the Internet between several Hydra instances

## [Terminal][10]
<img src="./Media/Terminal500.gif" align="left" />

**Terminal** - a **free** trading charting application (trading terminal):
  - Connections to the multiple electronic boards and brokers
  - Trading from charts by clicking
  - Arbitrary timeframes
  - Volume, Tick, Range, P&F, Renko candles
  - Cluster charts
  - Box charts
  - Volume Profile
  
## [Shell][11]
<img src="./Media/Shell500.gif" align="right" />

**Shell** - the ready-made graphical framework with the ability to quickly adapt to your needs and with fully open source code in C#:
  - Complete source code
  - Support for all StockSharp platform connections
  - Support for Designer schemas
  - Flexible user interface
  - Strategy testing (statistics, equity, reports)
  - Save and load strategy settings
  - Launch strategies in parallel
  - Detailed information on strategy performance 
  - Launch strategies on schedule

## [API][12]
API is a **free** C# library for programmers who use Visual Studio. The API lets you create any trading strategy, from long-timeframe positional strategies to high-frequency strategies (HFT) with direct access to the exchange (DMA). [More info...][12]
### Connector example
```C#
var connector = new Connector();
var security = connector.LookupById("AAPL@NASDAQ");

var subscription = new Subscription(DataType.TimeFrame(TimeSpan.FromMinutes(1)), security);

connector.CandleReceived += (sub, candle) =>
{
        if (sub != subscription || candle.State != CandleStates.Finished)
                return;

        // determine candle color
        var isGreen = candle.ClosePrice > candle.OpenPrice;

        // register market order depending on candle color
        var order = new Order
        {
                Security = security,
                Type = OrderTypes.Market,
                Side = isGreen ? Sides.Buy : Sides.Sell,
                Volume = 1
        };

        connector.RegisterOrder(order);
};

connector.Subscribe(subscription);
connector.Connect();
```

## Crypto exchanges
|Logo | Name | Documentation |
|:---:|:----:|:-------------:|
|<img src="./Media/logos/bibox_logo.svg" height="30" /> |Bibox | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bibox.html" target="_blank">Docs</a> |
|<img src="./Media/logos/binance_logo.svg" height="30" /> |Binance | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/binance.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bingx_logo.svg" height="30" /> |BingX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bingx.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitalong_logo.svg" height="30" /> |Bitalong | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitalong.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitbank_logo.svg" height="30" /> |Bitbank | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitbank.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitget_logo.svg" height="30" /> |Bitget | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitget.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitexbook_logo.svg" height="30" /> |Bitexbook | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitexbook.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitfinex_logo.svg" height="30" /> |Bitfinex | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitfinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bithumb_logo.svg" height="30" /> |Bithumb | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bithumb.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitmax_logo.svg" height="30" /> |BitMax | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitmax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitmex_logo.svg" height="30" /> |BitMEX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitmex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitstamp_logo.svg" height="30" /> |BitStamp | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitstamp.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bittrex_logo.svg" height="30" /> |Bittrex | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bittrex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitz_logo.svg" height="30" /> |BitZ | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitz.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bybit_logo.svg" height="30" /> |ByBit | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bybit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bw_logo.svg" height="30" /> |BW | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bw.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cexio_logo.svg" height="30" /> |CEX.IO | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/cex.io.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinbase_logo.svg" height="30" /> |Coinbase | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coinbase.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinbene_logo.svg" height="30" /> |CoinBene | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coinbene.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coincap_logo.svg" height="30" /> |CoinCap | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coincap.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coincheck_logo.svg" height="30" /> |Coincheck | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coincheck.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinex_logo.svg" height="30" /> |CoinEx | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinexchange_logo.svg" height="30" /> |CoinExchange | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coinexchange.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinigy_logo.svg" height="30" /> |Coinigy  | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coinigy.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinhub_logo.svg" height="30" /> |CoinHub | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coinhub.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cryptocom_logo.svg" height="30" /> |Crypto.com Exchange | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/crypto_com.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cryptopia_logo.svg" height="30" /> |Cryptopia | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/cryptopia.html" target="_blank">Docs</a> |
|<img src="./Media/logos/deribit_logo.svg" height="30" /> |Deribit | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/deribit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/digifinex_logo.svg" height="30" /> |DigiFinex | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/digifinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/digitexfutures_logo.svg" height="30" /> |DigitexFutures | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/digitexfutures.html" target="_blank">Docs</a> |
|<img src="./Media/logos/exmo_logo.svg" height="30" /> |EXMO | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/exmo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fatbtc_logo.svg" height="30" /> |FatBTC | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/fatbtc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gateio_logo.svg" height="30" /> |GateIO | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/gateio.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gdax_logo.svg" height="30" /> |GDAX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/gdax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gopax_logo.svg" height="30" /> |GOPAX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/gopax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hitbtc_logo.svg" height="30" /> |HitBTC | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/hitbtc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hotbit_logo.svg" height="30" /> |Hotbit | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/hotbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/huobi_logo.svg" height="30" /> |Huobi | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/huobi.html" target="_blank">Docs</a> |
|<img src="./Media/logos/idax_logo.svg" height="30" /> |IDAX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/idax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kraken_logo.svg" height="30" /> |Kraken | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/kraken.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kucoin_logo.svg" height="30" /> |KuCoin | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/kucoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/latoken_logo.svg" height="30" /> |LATOKEN | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/latoken.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lbank_logo.svg" height="30" /> |LBank | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/lbank.html" target="_blank">Docs</a> |
|<img src="./Media/logos/liqui_logo.svg" height="30" /> |Liqui | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/liqui.html" target="_blank">Docs</a> |
|<img src="./Media/logos/livecoin_logo.svg" height="30" /> |Livecoin | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/livecoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mexc_logo.svg" height="30" /> |MEXC | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/mexc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/okcoin_logo.svg" height="30" /> |OKCoin | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/okcoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/okex_logo.svg" height="30" /> |OKEx | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/okex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/poloniex_logo.svg" height="30" /> |Poloniex | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/poloniex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/prizmbit_logo.svg" height="30" /> |PrizmBit | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/prizmbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/liquid_logo.svg" height="30" /> |QuoineX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/quoinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradeogre_logo.svg" height="30" /> |TradeOgre | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/tradeogre.html" target="_blank">Docs</a> |
|<img src="./Media/logos/upbit_logo.svg" height="30" /> |Upbit | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/upbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/yobit_logo.svg" height="30" /> |YoBit | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/yobit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/zaif_logo.svg" height="30" /> |Zaif | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/zaif.html" target="_blank">Docs</a> |
|<img src="./Media/logos/zb_logo.svg" height="30" /> |ZB | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/zb.html" target="_blank">Docs</a> |

## DEX exchanges
|Logo | Name | Documentation |
|:---:|:----:|:-------------:|
|<img src="./Media/logos/aster_logo.svg" height="30" /> |Aster | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/aster.html" target="_blank">Docs</a> |
|<img src="./Media/logos/toobit_logo.svg" height="30" /> |Toobit | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/toobit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/whitebit_logo.svg" height="30" /> |WhiteBIT | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/whitebit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/weex_logo.svg" height="30" /> |WEEX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/weex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinw_logo.svg" height="30" /> |CoinW | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coinw.html" target="_blank">Docs</a> |
|<img src="./Media/logos/pionex_logo.png" height="30" /> |Pionex | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/pionex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/xtcom_logo.png" height="30" /> |XT.COM | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/xtcom.html" target="_blank">Docs</a> |
|<img src="./Media/logos/phemex_logo.png" height="30" /> |Phemex | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/phemex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bullish_logo.png" height="30" /> |Bullish | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bullish.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ourbit_logo.svg" height="30" /> |Ourbit | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/ourbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitunix_logo.svg" height="30" /> |Bitunix | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitunix.html" target="_blank">Docs</a> |
|<img src="./Media/logos/blofin_logo.png" height="30" /> |BloFin | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/blofin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/deepcoin_logo.png" height="30" /> |Deepcoin | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/deepcoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitrue_logo.png" height="30" /> |Bitrue | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitrue.html" target="_blank">Docs</a> |
|<img src="./Media/logos/woox_logo.svg" height="30" /> |WOO X | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/woox.html" target="_blank">Docs</a> |
|<img src="./Media/logos/backpack_logo.png" height="30" /> |Backpack Exchange | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/backpack.html" target="_blank">Docs</a> |
|<img src="./Media/logos/btse_logo.png" height="30" /> |BTSE | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/btse.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitvavo_logo.png" height="30" /> |Bitvavo | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitvavo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gemini_logo.png" height="30" /> |Gemini | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/gemini.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitpandafusion_logo.png" height="30" /> |Bitpanda Fusion | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitpanda_fusion.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hashkey_logo.png" height="30" /> |HashKey Global | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/hashkey_global.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitso_logo.png" height="30" /> |Bitso | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitso.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitkub_logo.svg" height="30" /> |Bitkub | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitkub.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coindcx_logo.svg" height="30" /> |CoinDCX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coindcx.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinsph_logo.svg" height="30" /> |Coins.ph | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coins_ph.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitflyer_logo.svg" height="30" /> |bitFlyer | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/bitflyer.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinone_logo.svg" height="30" /> |Coinone | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coinone.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gmocoin_logo.svg" height="30" /> |GMO Coin | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/gmo_coin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mercadobitcoin_logo.svg" height="30" /> |Mercado Bitcoin | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/mercado_bitcoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/valr_logo.svg" height="30" /> |VALR | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/valr.html" target="_blank">Docs</a> |
|<img src="./Media/logos/luno_logo.svg" height="30" /> |Luno | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/luno.html" target="_blank">Docs</a> |
|<img src="./Media/logos/independentreserve_logo.svg" height="30" /> |Independent Reserve | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/independent_reserve.html" target="_blank">Docs</a> |
|<img src="./Media/logos/btcmarkets_logo.svg" height="30" /> |BTC Markets | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/btc_markets.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinjar_logo.svg" height="30" /> |CoinJar Exchange | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/coinjar.html" target="_blank">Docs</a> |
|<img src="./Media/logos/korbit_logo.svg" height="30" /> |Korbit | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/korbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/pintupro_logo.svg" height="30" /> |Pintu Pro | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/pintu_pro.html" target="_blank">Docs</a> |
|<img src="./Media/logos/edgex_logo.svg" height="30" /> |edgeX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/edgex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ligther_logo.svg" height="30" /> |Ligther | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/ligther.html" target="_blank">Docs</a> |
|<img src="./Media/logos/paradex_logo.svg" height="30" /> |Paradex | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/paradex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hyperliquid_logo.svg" height="30" /> |Hyperliquid | <a href="https://doc.stocksharp.com/en/topics/api/connectors/crypto_exchanges/hyperliquid.html" target="_blank">Docs</a> |

## Stock, Futures and Options
|Logo | Name | Documentation |
|:---:|:----:|:-------------:|
|<img src="./Media/logos/polygonio_logo.svg" height="30" /> |Polygon.io | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/polygonio.html" target="_blank">Docs</a> |
|<img src="./Media/logos/publicdotcom_logo.svg" height="30" /> |Public.com | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/public.html" target="_blank">Docs</a> |
|<img src="./Media/logos/moomoo_logo.svg" height="30" /> |Moomoo | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/moomoo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ninjatrader_logo.svg" height="30" /> |NinjaTrader | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/ninjatrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lime_logo.svg" height="30" /> |Lime Trader | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/lime.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lemonmarkets_logo.svg" height="30" /> |lemon.markets | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/lemon_markets.html" target="_blank">Docs</a> |
|<img src="./Media/logos/snaptrade_logo.svg" height="30" /> |SnapTrade | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/snaptrade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/openmarkets_logo.svg" height="30" /> |OpenMarkets | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/openmarkets.html" target="_blank">Docs</a> |
|<img src="./Media/logos/phillip_poems_logo.png" height="30" /> |Phillip POEMS | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/phillip_poems.html" target="_blank">Docs</a> |
|<img src="./Media/logos/usmart_logo.png" height="30" /> |uSMART | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/usmart.html" target="_blank">Docs</a> |
|<img src="./Media/logos/alpaca_logo.svg" height="30" /> |Alpaca.Markets | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/alpaca.html" target="_blank">Docs</a> |
|<img src="./Media/logos/interactivebrokers_logo.svg" height="30" /> |Interactive Brokers | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/interactive_brokers.html" target="_blank">Docs</a> |
|<img src="./Media/logos/schwab_logo.svg" height="30" /> |Charles Schwab | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/schwab.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradovate_logo.svg" height="30" /> |Tradovate | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/tradovate.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradestation_logo.svg" height="30" /> |TradeStation | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/tradestation.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradelocker_logo.png" height="30" /> |TradeLocker | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/tradelocker.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tastytrade_logo.svg" height="30" /> |tastytrade | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/tastytrade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradezero_logo.svg" height="30" /> |TradeZero | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/tradezero.html" target="_blank">Docs</a> |
|<img src="./Media/logos/webull_logo.svg" height="30" /> |Webull | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/webull.html" target="_blank">Docs</a> |
|<img src="./Media/logos/angelone_logo.svg" height="30" /> |Angel One SmartAPI | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/angelone.html" target="_blank">Docs</a> |
|<img src="./Media/logos/dhan_logo.svg" height="30" /> |DhanHQ v2 | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/dhan.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fyers_logo.svg" height="30" /> |FYERS API v3 | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/fyers.html" target="_blank">Docs</a> |
|<img src="./Media/logos/breeze_logo.svg" height="30" /> |ICICI Direct Breeze API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/breeze.html" target="_blank">Docs</a> |
|<img src="./Media/logos/upstox_logo.svg" height="30" /> |Upstox | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/upstox.html" target="_blank">Docs</a> |
|<img src="./Media/logos/xtp_logo.svg" height="30" /> |Zhongtai XTP | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/xtp.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ctp_logo.svg" height="30" /> |CTP | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/ctp.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kotakneo_logo.svg" height="30" /> |Kotak Neo Trade API v2 | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/kotak_neo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tigerbrokers_logo.svg" height="30" /> |Tiger Brokers OpenAPI | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/tiger_brokers.html" target="_blank">Docs</a> |
|<img src="./Media/logos/saxo_logo.svg" height="30" /> |Saxo OpenAPI | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/saxo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/questrade_logo.svg" height="30" /> |Questrade API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/questrade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/longbridge_logo.svg" height="30" /> |Longbridge OpenAPI | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/longbridge.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cqg_logo.svg" height="30" /> |CQG Web API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/cqg_web_api.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ig_logo.svg" height="30" /> |IG Markets API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/ig.html" target="_blank">Docs</a> |
|<img src="./Media/logos/etoro_logo.svg" height="30" /> |eToro Public API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/etoro.html" target="_blank">Docs</a> |
|<img src="./Media/logos/koreainvestment_logo.svg" height="30" /> |Korea Investment & Securities Open API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/korea_investment.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kiwoom_logo.svg" height="30" /> |Kiwoom REST API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/kiwoom.html" target="_blank">Docs</a> |
|<img src="./Media/logos/trading212_logo.svg" height="30" /> |Trading 212 | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/trading212.html" target="_blank">Docs</a> |
|<img src="./Media/logos/daishin_logo.svg" height="30" /> |Daishin CYBOS Plus | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/daishin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/capital_futures_logo.svg" height="30" /> |Capital Futures API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/capital_futures.html" target="_blank">Docs</a> |
|<img src="./Media/logos/yuanta_logo.png" height="30" /> |Yuanta SPARK API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/yuanta.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fubon_neo_logo.png" height="30" /> |Fubon Neo API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/fubon_neo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/shioaji_logo.png" height="30" /> |SinoPac Shioaji | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/shioaji.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fugle_logo.png" height="30" /> |Fugle Market Data API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/fugle.html" target="_blank">Docs</a> |
|<img src="./Media/logos/flattrade_logo.png" height="30" /> |Flattrade Pi API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/flattrade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/alice_blue_logo.png" height="30" /> |Alice Blue ANT API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/alice_blue.html" target="_blank">Docs</a> |
|<img src="./Media/logos/shoonya_logo.png" height="30" /> |Shoonya API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/shoonya.html" target="_blank">Docs</a> |
|<img src="./Media/logos/motilal_oswal_logo.svg" height="30" /> |Motilal Oswal MO API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/motilal_oswal.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fivepaisa_logo.svg" height="30" /> |5paisa Xstream | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/fivepaisa.html" target="_blank">Docs</a> |
|<img src="./Media/logos/qmt_logo.svg" height="30" /> |QMT / MiniQMT / XtQuant | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/qmt.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lsegrealtime_logo.svg" height="30" /> |LSEG Real-Time | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/lseg_real_time.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bloomberg_logo.svg" height="30" /> |Bloomberg | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/bloomberg.html" target="_blank">Docs</a> |
|<img src="./Media/logos/databento_logo.svg" height="30" /> |Databento | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/databento.html" target="_blank">Docs</a> |
|<img src="./Media/logos/dxfeed_logo.svg" height="30" /> |dxFeed | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/dxfeed.html" target="_blank">Docs</a> |
|<img src="./Media/logos/swissquote_logo.svg" height="30" /> |Swissquote | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/swissquote.html" target="_blank">Docs</a> |
|<img src="./Media/logos/sharekhan_logo.svg" height="30" /> |Mirae Asset Sharekhan | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/mirae_asset_sharekhan.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lssecurities_logo.svg" height="30" /> |LS Securities Open API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/ls_securities.html" target="_blank">Docs</a> |
|<img src="./Media/logos/zerodha_logo.svg" height="30" /> |Zerodha Kite Connect | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/zerodha.html" target="_blank">Docs</a> |
|<img src="./Media/logos/capitalcom_logo.svg" height="30" /> |Capital.com API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/capitalcom.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kabustation_logo.svg" height="30" /> |Mitsubishi UFJ eSmart kabu Station API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/kabu_station.html" target="_blank">Docs</a> |
|<img src="./Media/logos/rakuten_rss_logo.png" height="30" /> |Rakuten MARKETSPEED II RSS | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/rakuten_rss.html" target="_blank">Docs</a> |
|<img src="./Media/logos/groww_logo.svg" height="30" /> |Groww Trading API | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/groww.html" target="_blank">Docs</a> |
|<img src="./Media/logos/goldmansachs_logo.png" height="30" /> |Goldman Sachs Marquee | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/marquee.html" target="_blank">Docs</a> |
|<img src="./Media/logos/jpmorgan_logo.png" height="30" /> |J.P. Morgan DataQuery | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/jpm_dataquery.html" target="_blank">Docs</a> |
|<img src="./Media/logos/factset_logo.png" height="30" /> |FactSet Prices | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/factset.html" target="_blank">Docs</a> |
|<img src="./Media/logos/morningstar_logo.svg" height="30" /> |Morningstar Direct Web Services | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/morningstar.html" target="_blank">Docs</a> |
|<img src="./Media/logos/spglobal_logo.png" height="30" /> |S&P Global Commodity Insights | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/sp_global_commodity_insights.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cboedatashop_logo.png" height="30" /> |Cboe DataShop / LiveVol | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/cboe_datashop.html" target="_blank">Docs</a> |
|<img src="./Media/logos/nasdaq_logo.svg" height="30" /> |Nasdaq Data Link | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/nasdaq_data_link.html" target="_blank">Docs</a> |
|<img src="./Media/logos/nasdaq_logo.svg" height="30" /> |Nasdaq Cloud Data Service | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/nasdaq_cloud_data_service.html" target="_blank">Docs</a> |
|<img src="./Media/logos/intrinio_logo.png" height="30" /> |Intrinio | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/intrinio.html" target="_blank">Docs</a> |
|<img src="./Media/logos/finnhub_logo.png" height="30" /> |Finnhub | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/finnhub.html" target="_blank">Docs</a> |
|<img src="./Media/logos/twelvedata_logo.png" height="30" /> |Twelve Data | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/twelvedata.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tiingo_logo.svg" height="30" /> |Tiingo | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/tiingo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/eodhd_logo.svg" height="30" /> |EOD Historical Data | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/eodhd.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fmp_logo.svg" height="30" /> |Financial Modeling Prep | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/fmp.html" target="_blank">Docs</a> |
|<img src="./Media/logos/marketstack_logo.svg" height="30" /> |Marketstack | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/marketstack.html" target="_blank">Docs</a> |
|<img src="./Media/logos/thetadata_logo.svg" height="30" /> |ThetaData | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/thetadata.html" target="_blank">Docs</a> |
|<img src="./Media/logos/orats_logo.svg" height="30" /> |ORATS | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/orats.html" target="_blank">Docs</a> |
|<img src="./Media/logos/optionmetrics_logo.svg" height="30" /> |OptionMetrics IvyDB | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/optionmetrics.html" target="_blank">Docs</a> |
|<img src="./Media/logos/algoseek_logo.svg" height="30" /> |AlgoSeek | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/algoseek.html" target="_blank">Docs</a> |
|<img src="./Media/logos/exegy_logo.svg" height="30" /> |Exegy | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/exegy.html" target="_blank">Docs</a> |
|<img src="./Media/logos/quodd_logo.svg" height="30" /> |QUODD | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/quodd.html" target="_blank">Docs</a> |
|<img src="./Media/logos/activfinancial_logo.svg" height="30" /> |ACTIV Financial | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/activ_financial.html" target="_blank">Docs</a> |
|<img src="./Media/logos/benzinga_logo.svg" height="30" /> |Benzinga | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/benzinga.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ravenpack_logo.svg" height="30" /> |RavenPack | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/ravenpack.html" target="_blank">Docs</a> |
|<img src="./Media/logos/dowjones_logo.svg" height="30" /> |Dow Jones | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/dow_jones.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mtnewswires_logo.svg" height="30" /> |MT Newswires | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/mt_newswires.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bmll_logo.svg" height="30" /> |BMLL | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/bmll.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fix_logo.svg" height="30" /> |FIX protocol (4.2, 4.4. 5.0) | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/fix_protocol.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fix_logo.svg" height="30" /> |FAST protocol | <a href="https://doc.stocksharp.com/en/topics/api/connectors/common/fast_protocol.html" target="_blank">Docs</a> |
|<img src="./Media/logos/sierrachartdtc_logo.svg" height="30" /> |Sierra Chart DTC | <a href="https://doc.stocksharp.com/en/topics/api/connectors/common/sierra_chart_dtc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bvmt_logo.svg" height="30" /> |BVMT | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/bvmt.html" target="_blank">Docs</a> |
|<img src="./Media/logos/alphavantage_logo.svg" height="30" /> |AlphaVantage | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/alphavantage.html" target="_blank">Docs</a> |
|<img src="./Media/logos/barchart_logo.svg" height="30" /> |BarChart | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/barchart.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cqg_logo.svg" height="30" /> |CQG | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/cqg.html" target="_blank">Docs</a> |
|<img src="./Media/logos/etrade_logo.svg" height="30" /> |E*TRADE | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/e_trade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/google_logo.svg" height="30" /> |Google | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/google.html" target="_blank">Docs</a> |
|<img src="./Media/logos/iex_logo.svg" height="30" /> |IEX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/iex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/iqfeed_logo.svg" height="30" /> |IQFeed | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/iqfeed.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lse_logo.svg" height="30" /> |ITCH | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/itch.html" target="_blank">Docs</a> |
|<img src="./Media/logos/openecry_logo.svg" height="30" /> |OpenECry | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/openecry.html" target="_blank">Docs</a> |
|<img src="./Media/logos/quandl_logo.svg" height="30" /> |Quandl | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/quandl.html" target="_blank">Docs</a> |
|<img src="./Media/logos/quanthouse_logo.svg" height="30" /> |QuantFEED | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/quantfeed.html" target="_blank">Docs</a> |
|<img src="./Media/logos/rithmic_logo.svg" height="30" /> |Rithmic | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/rithmic.html" target="_blank">Docs</a> |
|<img src="./Media/logos/robinhood_logo.svg" height="30" /> |Robinhood | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/robinhood.html" target="_blank">Docs</a> |
|<img src="./Media/logos/sterling_logo.svg" height="30" /> |Sterling | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/sterling.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradier_logo.svg" height="30" /> |Tradier | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/tradier.html" target="_blank">Docs</a> |
|<img src="./Media/logos/xignite_logo.svg" height="30" /> |Xignite | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/xignite.html" target="_blank">Docs</a> |
|<img src="./Media/logos/yahoo_logo.svg" height="30" /> |Yahoo | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/yahoo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/blackwood_logo.svg" height="30" /> |Blackwood (Fusion) | <a href="https://doc.stocksharp.com/en/topics/api/connectors/stock_market/blackwood_fusion.html" target="_blank">Docs</a> |


## Forex
|Logo | Name | Documentation |
|:---:|:----:|:-------------:|
|<img src="./Media/logos/devexperts_logo.svg" height="30" /> |DXtrade | <a href="https://doc.stocksharp.com/en/topics/api/connectors/forex/dxtrade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ctrader_logo.svg" height="30" /> |cTrader | <a href="https://doc.stocksharp.com/en/topics/api/connectors/forex/ctrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/matchtrader_logo.png" height="30" /> |Match-Trader | <a href="https://doc.stocksharp.com/en/topics/api/connectors/forex/matchtrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/xopenhub_logo.png" height="30" /> |X Open Hub | <a href="https://doc.stocksharp.com/en/topics/api/connectors/forex/xopenhub.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mt4_logo.svg" height="30" /> |MT4 | <a href="https://doc.stocksharp.com/en/topics/api/connectors/forex/metatrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mt5_logo.svg" height="30" /> |MT5 | <a href="https://doc.stocksharp.com/en/topics/api/connectors/forex/metatrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/dukascopy_logo.svg" height="30" /> |DukasCopy | <a href="https://doc.stocksharp.com/en/topics/api/connectors/forex/dukascopy.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fxcm_logo.svg" height="30" /> |FXCM | <a href="https://doc.stocksharp.com/en/topics/api/connectors/forex/fxcm.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lmax_logo.svg" height="30" /> |LMAX | <a href="https://doc.stocksharp.com/en/topics/api/connectors/forex/lmax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/oanda_logo.svg" height="30" /> |Oanda | <a href="https://doc.stocksharp.com/en/topics/api/connectors/forex/oanda.html" target="_blank">Docs</a> |

  [1]: https://stocksharp.com/en
  [4]: https://stocksharp.com/en/edu/
  [5]: https://stocksharp.com/en/forum/
  [6]: https://stocksharp.com/en/broker/
  [8]: https://stocksharp.com/en/store/strategy-designer/
  [9]: https://stocksharp.com/en/store/market-data-downloader/
  [10]: https://stocksharp.com/en/store/trading-terminal/
  [11]: https://stocksharp.com/en/store/trading-shell/
  [12]: https://stocksharp.com/en/store/api/
