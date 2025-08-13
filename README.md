<img src="./Media/SLogo.png" align="right" />

# [StockSharp - trading platform][1] 
## <a href="https://doc.stocksharp.com" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4d6.svg" alt="Docs" height="40"/> Docs</a> <a href="https://stocksharp.com/products/download/" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4be.svg" alt="Download" height="40"/> Download</a> <a href="https://t.me/stocksharpchat/361" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4ac.svg" alt="Chat" height="40"/> Chat</a> <a href="https://www.youtube.com/@stocksharp"><img src="https://raw.githubusercontent.com/edent/SuperTinyIcons/master/images/svg/youtube.svg" alt="YouTube" height="40"/> YouTube</a>

## Introduction ##

**StockSharp** (shortly **S#**) – are **free** platform for trading at any markets of the world (crypto exchanges, American, European, Asian, Russian, stocks, futures, options, Bitcoins, forex, etc.). You will be able to trade manually or automated trading (algorithmic trading robots, conventional or HFT).

**Available connections**: Binance, MT4, MT5, FIX/FAST, PolygonIO, Trading Technologies, Alpaca Markets, BarChart, CQG, E*Trade, IQFeed, InteractiveBrokers, LMAX, MatLab, Oanda, FXCM, Rithmic, cTrader, DXtrade, BitStamp, Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, OKX, Coincheck, CEX.IO, BitMEX, YoBit, Livecoin, EXMO, Deribit, HTX, KuCoin, QuantFEED and many other.

## [Designer][8]
<img src="./Media/Designer500.gif" align="left" />

**Designer** - **free** universal algorithmic strategies application for easy strategy creation:
  - Visual designer to create strategies by mouse clicking
  - Embedded C# editor
  - Easy to create own indicators
  - Build in debugger
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

**Terminal** - **free** trading charting application (trading terminal):
  - Connections to the multiple electronic boards and brokers
  - Trading from charts by clicking
  - Arbitrary timeframes
  - Volume, Tick, Range, P&F, Renko candles
  - Cluster charts
  - Box charts
  - Volume Profile
  
## [Shell][11]
<img src="./Media/Shell500.gif" align="right" />

**Shell** - the ready-made graphical framework with the ability to quickly change to your needs and with fully open source code in C#:
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
API is a **free** C# library for programmers who use Visual Studio. The API lets you create any trading strategy, from long-timeframe positional strategies to high frequency strategies (HFT) with direct access to the exchange (DMA). [More info...][12]
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
|Logo | Name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|<img src="./Media/logos/bibox_logo.svg" height="30" /> |Bibox | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bibox.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bibox.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Binance_logo.svg" height="30" /> |Binance | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/binance.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/binance.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bingx_logo.svg" height="30" /> |BingX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bingx.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bingx.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bitalong_logo.svg" height="30" /> |Bitalong | <a href="https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitalong.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitalong.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bitbank_logo.svg" height="30" /> |Bitbank | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitbank.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitbank.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bitget_logo.svg" height="30" /> |Bitget | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitget.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitget.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bitexbook_logo.svg" height="30" /> |Bitexbook | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitexbook.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitexbook.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bitfinex_logo.svg" height="30" /> |Bitfinex | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitfinex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitfinex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bithumb_logo.svg" height="30" /> |Bithumb | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bithumb.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bithumb.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bitmax_logo.svg" height="30" /> |BitMax | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitmax.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitmax.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bitmex_logo.svg" height="30" /> |BitMEX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitmex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitmex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/BitStamp_logo.svg" height="30" /> |BitStamp | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitstamp.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitstamp.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bittrex_logo.svg" height="30" /> |Bittrex | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bittrex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bittrex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/BitZ_logo.png" height="30" /> |BitZ | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitz.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitz.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bybit_logo.svg" height="30" /> |ByBit | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bybit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bybit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bw_logo.svg" height="30" /> |BW | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bw.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bw.html" target="_blank">Ru</a> |
|<img src="./Media/logos/cexio_logo.svg" height="30" /> |CEX.IO | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/cex.io.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/cex.io.html" target="_blank">Ru</a> |
|<img src="./Media/logos/coinbase_logo.svg" height="30" /> |Coinbase | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinbase.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinbase.html" target="_blank">Ru</a> |
|<img src="./Media/logos/coinbene_logo.svg" height="30" /> |CoinBene | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinbene.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinbene.html" target="_blank">Ru</a> |
|<img src="./Media/logos/coincap_logo.svg" height="30" /> |CoinCap | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coincap.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coincap.html" target="_blank">Ru</a> |
|<img src="./Media/logos/coincheck_logo.svg" height="30" /> |Coincheck | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coincheck.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coincheck.html" target="_blank">Ru</a> |
|<img src="./Media/logos/coinex_logo.svg" height="30" /> |CoinEx | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinExchange_logo.png" height="30" /> |CoinExchange | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinexchange.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinexchange.html" target="_blank">Ru</a> |
|<img src="./Media/logos/coinigy_logo.svg" height="30" /> |Coinigy  | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinigy.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinigy.html" target="_blank">Ru</a> |
|<img src="./Media/logos/coinhub_logo.svg" height="30" /> |CoinHub | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinhub.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinhub.html" target="_blank">Ru</a> |
|<img src="./Media/logos/cryptopia_logo.svg" height="30" /> |Cryptopia | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/cryptopia.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/cryptopia.html" target="_blank">Ru</a> |
|<img src="./Media/logos/deribit_logo.svg" height="30" /> |Deribit | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/deribit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/deribit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/digifinex_logo.svg" height="30" /> |DigiFinex | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/digifinex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/digifinex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/digitexfutures_logo.svg" height="30" /> |DigitexFutures | <a href="https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/digitexfutures.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/digitexfutures.html" target="_blank">Ru</a> |
|<img src="./Media/logos/exmo_logo.svg" height="30" /> |EXMO | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/exmo.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/exmo.html" target="_blank">Ru</a> |
|<img src="./Media/logos/fatbtc_logo.svg" height="30" /> |FatBTC | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/fatbtc.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/fatbtc.html" target="_blank">Ru</a> |
|<img src="./Media/logos/gateio_logo.svg" height="30" /> |GateIO | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/gateio.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/gateio.html" target="_blank">Ru</a> |
|<img src="./Media/logos/gdax_logo.svg" height="30" /> |GDAX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/gdax.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/gdax.html" target="_blank">Ru</a> |
|<img src="./Media/logos/gopax_logo.svg" height="30" /> |GOPAX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/gopax.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/gopax.html" target="blank">Ru</a> |
|<img src="./Media/logos/hitbtc_logo.svg" height="30" /> |HitBTC | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/hitbtc.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/hitbtc.html" target="_blank">Ru</a> |
|<img src="./Media/logos/hotbit_logo.svg" height="30" /> |Hotbit | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/hotbit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/hotbit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/huobi_logo.svg" height="30" /> |Huobi | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/huobi.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/huobi.html" target="_blank">Ru</a> |
|<img src="./Media/logos/idax_logo.svg" height="30" /> |IDAX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/idax.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/idax.html" target="_blank">Ru</a> |
|<img src="./Media/logos/kraken_logo.svg" height="30" /> |Kraken | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/kraken.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/kraken.html" target="_blank">Ru</a> |
|<img src="./Media/logos/kucoin_logo.svg" height="30" /> |KuCoin | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/kucoin.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/kucoin.html" target="_blank">Ru</a> |
|<img src="./Media/logos/latoken_logo.svg" height="30" /> |LATOKEN | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/latoken.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/latoken.html" target="_blank">Ru</a> |
|<img src="./Media/logos/lbank_logo.svg" height="30" /> |LBank | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/lbank.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/lbank.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Liqui_logo.png" height="30" /> |Liqui | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/liqui.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/liqui.html" target="_blank">Ru</a> |
|<img src="./Media/logos/livecoin_logo.svg" height="30" /> |Livecoin | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/livecoin.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/livecoin.html" target="_blank">Ru</a> |
|<img src="./Media/logos/mexc_logo.svg" height="30" /> |MEXC | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/mexc.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/mexc.html" target="_blank">Ru</a> |
|<img src="./Media/logos/okcoin_logo.svg" height="30" /> |OKCoin | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/okcoin.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/okcoin.html" target="_blank">Ru</a> |
|<img src="./Media/logos/okex_logo.svg" height="30" /> |OKEx | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/okex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/okex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/poloniex_logo.svg" height="30" /> |Poloniex | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/poloniex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/poloniex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/prizmbit_logo.svg" height="30" /> |PrizmBit | <a href="https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/prizmbit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/prizmbit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/liquid_logo.svg" height="30" /> |QuoineX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/quoinex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/quoinex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/tradeogre_logo.svg" height="30" /> |TradeOgre | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/tradeogre.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/tradeogre.html" target="_blank">Ru</a> |
|<img src="./Media/logos/upbit_logo.svg" height="30" /> |Upbit | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/upbit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/upbit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/yobit_logo.svg" height="30" /> |YoBit | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/yobit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/yobit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/zaif_logo.svg" height="30" /> |Zaif | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/zaif.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/zaif.html" target="_blank">Ru</a> |
|<img src="./Media/logos/zb_logo.svg" height="30" /> |ZB | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/zb.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/zb.html" target="_blank">Ru</a> |

## Stock, Futures and Options
|Logo | Name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|<img src="./Media/logos/polygonio_logo.svg" height="30" /> |Polygon.io | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/polygonio.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/polygonio.html" target="_blank">Ru</a> |
|<img src="./Media/logos/alpaca_logo.svg" height="30" /> |Alpaca.Markets | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/alpaca.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/alpaca.html" target="_blank">Ru</a> |
|<img src="./Media/logos/interactivebrokers_logo.svg" height="30" /> |Interactive Brokers | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/interactive_brokers.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/interactive_brokers.html" target="_blank">Ru</a> |
|<img src="./Media/logos/fix_logo.svg" height="30" /> |FIX protocol (4.2, 4.4. 5.0) | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/fix_protocol.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/fix_protocol.html" target="_blank">Ru</a> |
|<img src="./Media/logos/fix_logo.svg" height="30" /> |FAST protocol | <a href="//doc.stocksharp.com/topics/api/connectors/common/fast_protocol.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/common/fast_protocol.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bvmt_logo.svg" height="30" /> |BVMT | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/bvmt.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/bvmt.html" target="_blank">Ru</a> |
|<img src="./Media/logos/alphavantage_logo.svg" height="30" /> |AlphaVantage | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/alphavantage.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/alphavantage.html" target="_blank">Ru</a> |
|<img src="./Media/logos/barchart_logo.svg" height="30" /> |BarChart | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/barchart.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/barchart.html" target="_blank">Ru</a> |
|<img src="./Media/logos/cqg_logo.svg" height="30" /> |CQG | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/cqg.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/cqg.html" target="_blank">Ru</a> |
|<img src="./Media/logos/etrade_logo.svg" height="30" /> |E*TRADE | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/e_trade.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/e_trade.html" target="_blank">Ru</a> |
|<img src="./Media/logos/google_logo.svg" height="30" /> |Google | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/google.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/google.html" target="_blank">Ru</a> |
|<img src="./Media/logos/iex_logo.svg" height="30" /> |IEX | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/iex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/iex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/iqfeed_logo.svg" height="30" /> |IQFeed | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/iqfeed.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/iqfeed.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Lse_logo.svg" height="30" /> |ITCH | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/itch.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/itch.html" target="_blank">Ru</a> |
|<img src="./Media/logos/OpenECry_logo.png" height="30" /> |OpenECry | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/openecry.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/openecry.html" target="_blank">Ru</a> |
|<img src="./Media/logos/quandl_logo.svg" height="30" /> |Quandl | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/quandl.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/quandl.html" target="_blank">Ru</a> |
|<img src="./Media/logos/quanthouse_logo.png" height="30" /> |QuantFEED | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/quantfeed.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/quantfeed.html" target="_blank">Ru</a> |
|<img src="./Media/logos/rithmic_logo.svg" height="30" /> |Rithmic | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/rithmic.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/rithmic.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Sterling_logo.png" height="30" /> |Sterling | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/sterling.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/sterling.html" target="_blank">Ru</a> |
|<img src="./Media/logos/tradier_logo.svg" height="30" /> |Tradier | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/tradier.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/tradier.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Xignite_logo.png" height="30" /> |Xignite | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/xignite.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/xignite.html" target="_blank">Ru</a> |
|<img src="./Media/logos/yahoo_logo.svg" height="30" /> |Yahoo | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/yahoo.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/yahoo.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Blackwood_logo.png" height="30" /> |Blackwood (Fusion) | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/blackwood_fusion.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/blackwood_fusion.html" target="_blank">Ru</a> |

## Russian market
|Logo | Name |  Documentation Ru| 
|:---:|:----:|:---------------:|
|<img src="./Media/logos/quik_logo.svg" height="30" /> |Quik |  <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/quik.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Tinkoff_logo.svg" height="30" /> |Tinkoff |  <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/tinkoff.html" target="_blank">Ru</a> |
|<img src="./Media/logos/mfd_logo.svg" height="30" /> |Mfd | <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/mfd.html" target="_blank">Ru</a> |
|<img src="./Media/logos/moex_logo.svg" height="30" /> |Micex (TEAP) | <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/micex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/moex_logo.svg" height="30" /> |Plaza II | <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/plaza.html" target="_blank">Ru</a> |
|<img src="./Media/logos/quik_logo.svg" height="30" /> |Quik FIX |  <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/quikfix.html" target="_blank">Ru</a> |
|<img src="./Media/logos/itinvest_logo.svg" height="30" /> |SmartCOM |  <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/smartcom.html" target="_blank">Ru</a> |
|<img src="./Media/logos/spbex_logo.svg" height="30" /> |SPB Exchange |  <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/spb_exchange.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Transaq_logo.png" height="30" /> |Transaq |  <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/transaq.html" target="_blank">Ru</a> |
|<img src="./Media/logos/moex_logo.svg" height="30" /> |Twime |  <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/twime.html" target="_blank">Ru</a> |
|<img src="./Media/logos/UkrExh_logo.png" height="30" /> |UX (сайт) | <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/ux.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Alor_logo.svg" height="30" /> |Алор История | <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/alorhistory.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Alor_logo.svg" height="30" /> |Алор Брокер | <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/alor.html" target="_blank">Ru</a> |
|<img src="./Media/logos/alfadirect_logo.svg" height="30" /> |Альфа-Директ | <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/alfadirect.html" target="_blank">Ru</a> |
|<img src="./Media/logos/MoexLchi_logo.png" height="30" /> |ЛЧИ | <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/lci.html" target="_blank">Ru</a> |
|<img src="./Media/logos/finam_logo.svg" height="30" /> |Финам | <a href="https://doc.stocksharp.ru/topics/api/connectors/russia/finam.html" target="_blank">Ru</a> |

## Forex
|Logo | Name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|<img src="./Media/logos/devexperts_logo.svg" height="30" /> |DXtrade | <a href="//doc.stocksharp.com/topics/api/connectors/forex/dxtrade.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/dxtrade.html" target="_blank">Ru</a> |
|<img src="./Media/logos/ctrader_logo.svg" height="30" /> |cTrader | <a href="//doc.stocksharp.com/topics/api/connectors/forex/ctrader.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/ctrader.html" target="_blank">Ru</a> |
|<img src="./Media/logos/mt4_logo.svg" height="30" /> |MT4 | <a href="//doc.stocksharp.com/topics/api/connectors/forex/metatrader.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/metatrader.html" target="_blank">Ru</a> |
|<img src="./Media/logos/mt5_logo.svg" height="30" /> |MT5 | <a href="//doc.stocksharp.com/topics/api/connectors/forex/metatrader.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/metatrader.html" target="_blank">Ru</a> |
|<img src="./Media/logos/dukascopy_logo.svg" height="30" /> |DukasCopy | <a href="//doc.stocksharp.com/topics/api/connectors/forex/dukascopy.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/dukascopy.html" target="_blank">Ru</a> |
|<img src="./Media/logos/fxcm_logo.svg" height="30" /> |FXCM | <a href="//doc.stocksharp.com/topics/api/connectors/forex/fxcm.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/fxcm.html" target="_blank">Ru</a> |
|<img src="./Media/logos/lmax_logo.svg" height="30" /> |LMAX | <a href="//doc.stocksharp.com/topics/api/connectors/forex/lmax.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/lmax.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Oanda_logo.svg" height="30" /> |Oanda | <a href="//doc.stocksharp.com/topics/api/connectors/forex/oanda.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/oanda.html" target="_blank">Ru</a> |

  [1]: https://stocksharp.com
  [4]: https://stocksharp.com/edu/
  [5]: https://stocksharp.com/forum/
  [6]: https://stocksharp.com/broker/
  [8]: https://stocksharp.com/store/strategy-designer/
  [9]: https://stocksharp.com/store/market-data-downloader/
  [10]: https://stocksharp.com/store/trading-terminal/
  [11]: https://stocksharp.com/store/trading-shell/
  [12]: https://stocksharp.com/store/api/

