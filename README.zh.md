<img src="./Media/SLogo.png" align="right" />

# [StockSharp - 交易平台][1]

## [English](README.md) | [Русский](README.ru.md) | **中文**

## <a href="https://doc.stocksharp.com/zh" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4d6.svg" alt="Docs" height="40"/> 文档</a> <a href="https://stocksharp.com/zh/products/download/" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4be.svg" alt="Download" height="40"/> 下载</a> <a href="https://stocksharp.com/zh/chat/" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4ac.svg" alt="Chat" height="40"/> 聊天</a> <a href="https://www.youtube.com/@stocksharp_china"><img src="https://raw.githubusercontent.com/edent/SuperTinyIcons/master/images/svg/youtube.svg" alt="YouTube" height="40"/> YouTube</a>

## 介绍 ##

**StockSharp**（简称 **S#**）是一个**免费**的全球市场交易平台（加密货币交易所、美国、欧洲、亚洲、俄罗斯股票、期货、期权、比特币、外汇等）。您可以进行手动交易或自动交易（算法交易机器人、常规或高频交易 HFT）。

**可用连接**: Binance, MT4, MT5, FIX/FAST, PolygonIO, Trading Technologies, Alpaca Markets, BarChart, CQG, E*Trade, IQFeed, InteractiveBrokers, LMAX, MatLab, Oanda, FXCM, Rithmic, cTrader, DXtrade, BitStamp, Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, OKX, Coincheck, CEX.IO, BitMEX, YoBit, Livecoin, EXMO, Deribit, HTX, KuCoin, QuantFEED, Aster, edgeX, Ligther, Paradex, Hyperliquid 等等。

连接器源代码和完整连接器列表可在 [StockSharp Connectors 仓库](https://github.com/StockSharp/Connectors) 中找到。

## [Designer][8]
<img src="./Media/Designer500.gif" align="left" />

**Designer** - **免费**的通用算法策略应用程序，用于轻松创建策略：
  - 可视化设计器，通过鼠标点击创建策略
  - 内置 C# 编辑器
  - 轻松创建自己的指标
  - 内置调试器
  - 连接到多个电子交易所和经纪商
  - 全球所有平台
  - 与团队共享架构

## [Hydra][9]
<img src="./Media/Hydra500.gif" align="right" />

**Hydra** - **免费**软件，用于自动加载和存储市场数据：
  - 支持多种数据源
  - 高压缩比
  - 任何数据类型
  - 通过 API 程序访问存储的数据
  - 导出到 csv、excel、xml 或数据库
  - 从 csv 导入
  - 计划任务
  - 通过互联网在多个 Hydra 实例之间自动同步

## [Terminal][10]
<img src="./Media/Terminal500.gif" align="left" />

**Terminal** - **免费**交易图表应用程序（交易终端）：
  - 连接到多个电子交易所和经纪商
  - 通过点击图表进行交易
  - 任意时间框架
  - Volume、Tick、Range、P&F、Renko K线
  - 集群图表
  - Box 图表
  - 成交量分布

## [Shell][11]
<img src="./Media/Shell500.gif" align="right" />

**Shell** - 现成的图形框架，可以快速适应您的需求，具有完全开源的 C# 代码：
  - 完整源代码
  - 支持所有 StockSharp 平台连接
  - 支持 Designer 架构
  - 灵活的用户界面
  - 策略测试（统计、权益、报告）
  - 保存和加载策略设置
  - 并行启动策略
  - 策略性能的详细信息
  - 按计划启动策略

## [API][12]
API 是一个面向使用 Visual Studio 的程序员的**免费** C# 库。API 允许您创建任何交易策略，从长期持仓策略到直接访问交易所的高频策略 (HFT) (DMA)。[更多信息...][12]

### 连接器示例
```C#
var connector = new Connector();
var security = connector.LookupById("AAPL@NASDAQ");

var subscription = new Subscription(DataType.TimeFrame(TimeSpan.FromMinutes(1)), security);

connector.CandleReceived += (sub, candle) =>
{
        if (sub != subscription || candle.State != CandleStates.Finished)
                return;

        // 确定K线颜色
        var isGreen = candle.ClosePrice > candle.OpenPrice;

        // 根据K线颜色注册市价单
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

## 加密货币交易所
|图标 | 名称 | 文档|
|:---:|:----:|:------:|
|<img src="./Media/logos/bibox_logo.svg" height="30" /> |Bibox | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bibox.html" target="_blank">Docs</a> |
|<img src="./Media/logos/binance_logo.svg" height="30" /> |Binance | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/binance.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bingx_logo.svg" height="30" /> |BingX | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bingx.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitalong_logo.svg" height="30" /> |Bitalong | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bitalong.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitbank_logo.svg" height="30" /> |Bitbank | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bitbank.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitget_logo.svg" height="30" /> |Bitget | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bitget.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitexbook_logo.svg" height="30" /> |Bitexbook | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bitexbook.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitfinex_logo.svg" height="30" /> |Bitfinex | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bitfinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bithumb_logo.svg" height="30" /> |Bithumb | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bithumb.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitmax_logo.svg" height="30" /> |BitMax | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bitmax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitmex_logo.svg" height="30" /> |BitMEX | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bitmex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitstamp_logo.svg" height="30" /> |BitStamp | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bitstamp.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bittrex_logo.svg" height="30" /> |Bittrex | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bittrex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitz_logo.svg" height="30" /> |BitZ | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bitz.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bybit_logo.svg" height="30" /> |ByBit | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bybit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bw_logo.svg" height="30" /> |BW | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/bw.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cexio_logo.svg" height="30" /> |CEX.IO | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/cex.io.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinbase_logo.svg" height="30" /> |Coinbase | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/coinbase.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinbene_logo.svg" height="30" /> |CoinBene | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/coinbene.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coincap_logo.svg" height="30" /> |CoinCap | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/coincap.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coincheck_logo.svg" height="30" /> |Coincheck | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/coincheck.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinex_logo.svg" height="30" /> |CoinEx | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/coinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinexchange_logo.svg" height="30" /> |CoinExchange | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/coinexchange.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinigy_logo.svg" height="30" /> |Coinigy  | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/coinigy.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinhub_logo.svg" height="30" /> |CoinHub | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/coinhub.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cryptopia_logo.svg" height="30" /> |Cryptopia | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/cryptopia.html" target="_blank">Docs</a> |
|<img src="./Media/logos/deribit_logo.svg" height="30" /> |Deribit | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/deribit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/digifinex_logo.svg" height="30" /> |DigiFinex | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/digifinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/digitexfutures_logo.svg" height="30" /> |DigitexFutures | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/digitexfutures.html" target="_blank">Docs</a> |
|<img src="./Media/logos/exmo_logo.svg" height="30" /> |EXMO | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/exmo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fatbtc_logo.svg" height="30" /> |FatBTC | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/fatbtc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gateio_logo.svg" height="30" /> |GateIO | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/gateio.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gdax_logo.svg" height="30" /> |GDAX | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/gdax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gopax_logo.svg" height="30" /> |GOPAX | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/gopax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hitbtc_logo.svg" height="30" /> |HitBTC | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/hitbtc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hotbit_logo.svg" height="30" /> |Hotbit | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/hotbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/huobi_logo.svg" height="30" /> |Huobi | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/huobi.html" target="_blank">Docs</a> |
|<img src="./Media/logos/idax_logo.svg" height="30" /> |IDAX | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/idax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kraken_logo.svg" height="30" /> |Kraken | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/kraken.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kucoin_logo.svg" height="30" /> |KuCoin | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/kucoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/latoken_logo.svg" height="30" /> |LATOKEN | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/latoken.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lbank_logo.svg" height="30" /> |LBank | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/lbank.html" target="_blank">Docs</a> |
|<img src="./Media/logos/liqui_logo.svg" height="30" /> |Liqui | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/liqui.html" target="_blank">Docs</a> |
|<img src="./Media/logos/livecoin_logo.svg" height="30" /> |Livecoin | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/livecoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mexc_logo.svg" height="30" /> |MEXC | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/mexc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/okcoin_logo.svg" height="30" /> |OKCoin | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/okcoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/okex_logo.svg" height="30" /> |OKEx | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/okex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/poloniex_logo.svg" height="30" /> |Poloniex | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/poloniex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/prizmbit_logo.svg" height="30" /> |PrizmBit | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/prizmbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/liquid_logo.svg" height="30" /> |QuoineX | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/quoinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradeogre_logo.svg" height="30" /> |TradeOgre | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/tradeogre.html" target="_blank">Docs</a> |
|<img src="./Media/logos/upbit_logo.svg" height="30" /> |Upbit | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/upbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/yobit_logo.svg" height="30" /> |YoBit | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/yobit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/zaif_logo.svg" height="30" /> |Zaif | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/zaif.html" target="_blank">Docs</a> |
|<img src="./Media/logos/zb_logo.svg" height="30" /> |ZB | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/zb.html" target="_blank">Docs</a> |

## DEX exchanges
|Logo | Name | Documentation |
|:---:|:----:|:-------------:|
|<img src="./Media/logos/aster_logo.svg" height="30" /> |Aster | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/aster.html" target="_blank">Docs</a> |
|<img src="./Media/logos/edgex_logo.svg" height="30" /> |edgeX | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/edgex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ligther_logo.svg" height="30" /> |Ligther | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/ligther.html" target="_blank">Docs</a> |
|<img src="./Media/logos/paradex_logo.svg" height="30" /> |Paradex | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/paradex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hyperliquid_logo.svg" height="30" /> |Hyperliquid | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/crypto_exchanges/hyperliquid.html" target="_blank">Docs</a> |


*[所有加密货币交易所的完整列表 - 请参阅英文版 README](README.md#crypto-exchanges)*

## 股票、期货和期权
|图标 | 名称 | 文档|
|:---:|:----:|:------:|
|<img src="./Media/logos/polygonio_logo.svg" height="30" /> |Polygon.io | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/polygonio.html" target="_blank">Docs</a> |
|<img src="./Media/logos/publicdotcom_logo.svg" height="30" /> |Public.com | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/public.html" target="_blank">文档</a> |
|<img src="./Media/logos/moomoo_logo.svg" height="30" /> |Moomoo | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/moomoo.html" target="_blank">文档</a> |
|<img src="./Media/logos/ninjatrader_logo.svg" height="30" /> |NinjaTrader | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/ninjatrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lime_logo.svg" height="30" /> |Lime Trader | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/lime.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lemonmarkets_logo.svg" height="30" /> |lemon.markets | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/lemon_markets.html" target="_blank">Docs</a> |
|<img src="./Media/logos/snaptrade_logo.svg" height="30" /> |SnapTrade | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/snaptrade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/alpaca_logo.svg" height="30" /> |Alpaca.Markets | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/alpaca.html" target="_blank">Docs</a> |
|<img src="./Media/logos/interactivebrokers_logo.svg" height="30" /> |Interactive Brokers | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/interactive_brokers.html" target="_blank">Docs</a> |
|<img src="./Media/logos/schwab_logo.svg" height="30" /> |Charles Schwab | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/schwab.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradovate_logo.svg" height="30" /> |Tradovate | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/tradovate.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradestation_logo.svg" height="30" /> |TradeStation | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/tradestation.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tastytrade_logo.svg" height="30" /> |tastytrade | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/tastytrade.html" target="_blank">文档</a> |
|<img src="./Media/logos/tradezero_logo.svg" height="30" /> |TradeZero | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/tradezero.html" target="_blank">Docs</a> |
|<img src="./Media/logos/webull_logo.svg" height="30" /> |Webull | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/webull.html" target="_blank">Docs</a> |
|<img src="./Media/logos/angelone_logo.svg" height="30" /> |Angel One SmartAPI | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/angelone.html" target="_blank">Docs</a> |
|<img src="./Media/logos/dhan_logo.svg" height="30" /> |DhanHQ v2 | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/dhan.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fyers_logo.svg" height="30" /> |FYERS API v3 | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/fyers.html" target="_blank">Docs</a> |
|<img src="./Media/logos/breeze_logo.svg" height="30" /> |ICICI Direct Breeze API | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/breeze.html" target="_blank">Docs</a> |
|<img src="./Media/logos/upstox_logo.svg" height="30" /> |Upstox | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/upstox.html" target="_blank">Docs</a> |
|<img src="./Media/logos/xtp_logo.svg" height="30" /> |Zhongtai XTP | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/xtp.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ctp_logo.svg" height="30" /> |CTP | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/ctp.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kotakneo_logo.svg" height="30" /> |Kotak Neo Trade API v2 | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/kotak_neo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tigerbrokers_logo.svg" height="30" /> |Tiger Brokers OpenAPI | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/tiger_brokers.html" target="_blank">Docs</a> |
|<img src="./Media/logos/saxo_logo.svg" height="30" /> |Saxo OpenAPI | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/saxo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/questrade_logo.svg" height="30" /> |Questrade API | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/questrade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/longbridge_logo.svg" height="30" /> |Longbridge OpenAPI | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/longbridge.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cqg_logo.svg" height="30" /> |CQG Web API | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/cqg_web_api.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ig_logo.svg" height="30" /> |IG Markets API | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/ig.html" target="_blank">Docs</a> |
|<img src="./Media/logos/etoro_logo.svg" height="30" /> |eToro Public API | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/etoro.html" target="_blank">Docs</a> |
|<img src="./Media/logos/koreainvestment_logo.svg" height="30" /> |Korea Investment & Securities Open API | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/korea_investment.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fix_logo.svg" height="30" /> |FIX protocol (4.2, 4.4. 5.0) | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/fix_protocol.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fix_logo.svg" height="30" /> |FAST protocol | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/common/fast_protocol.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bvmt_logo.svg" height="30" /> |BVMT | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/bvmt.html" target="_blank">Docs</a> |
|<img src="./Media/logos/alphavantage_logo.svg" height="30" /> |AlphaVantage | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/alphavantage.html" target="_blank">Docs</a> |
|<img src="./Media/logos/barchart_logo.svg" height="30" /> |BarChart | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/barchart.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cqg_logo.svg" height="30" /> |CQG | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/cqg.html" target="_blank">Docs</a> |
|<img src="./Media/logos/etrade_logo.svg" height="30" /> |E*TRADE | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/e_trade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/google_logo.svg" height="30" /> |Google | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/google.html" target="_blank">Docs</a> |
|<img src="./Media/logos/iex_logo.svg" height="30" /> |IEX | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/iex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/iqfeed_logo.svg" height="30" /> |IQFeed | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/iqfeed.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lse_logo.svg" height="30" /> |ITCH | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/itch.html" target="_blank">Docs</a> |
|<img src="./Media/logos/openecry_logo.svg" height="30" /> |OpenECry | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/openecry.html" target="_blank">Docs</a> |
|<img src="./Media/logos/quandl_logo.svg" height="30" /> |Quandl | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/quandl.html" target="_blank">Docs</a> |
|<img src="./Media/logos/quanthouse_logo.svg" height="30" /> |QuantFEED | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/quantfeed.html" target="_blank">Docs</a> |
|<img src="./Media/logos/rithmic_logo.svg" height="30" /> |Rithmic | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/rithmic.html" target="_blank">Docs</a> |
|<img src="./Media/logos/robinhood_logo.svg" height="30" /> |Robinhood | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/robinhood.html" target="_blank">Docs</a> |
|<img src="./Media/logos/sterling_logo.svg" height="30" /> |Sterling | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/sterling.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradier_logo.svg" height="30" /> |Tradier | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/tradier.html" target="_blank">Docs</a> |
|<img src="./Media/logos/xignite_logo.svg" height="30" /> |Xignite | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/xignite.html" target="_blank">Docs</a> |
|<img src="./Media/logos/yahoo_logo.svg" height="30" /> |Yahoo | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/yahoo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/blackwood_logo.svg" height="30" /> |Blackwood (Fusion) | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/stock_market/blackwood_fusion.html" target="_blank">Docs</a> |

*[所有股票交易所的完整列表 - 请参阅英文版 README](README.md#stock-futures-and-options)*

## 外汇
|图标 | 名称 | 文档|
|:---:|:----:|:------:|
|<img src="./Media/logos/devexperts_logo.svg" height="30" /> |DXtrade | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/forex/dxtrade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ctrader_logo.svg" height="30" /> |cTrader | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/forex/ctrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mt4_logo.svg" height="30" /> |MT4 | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/forex/metatrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mt5_logo.svg" height="30" /> |MT5 | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/forex/metatrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/dukascopy_logo.svg" height="30" /> |DukasCopy | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/forex/dukascopy.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fxcm_logo.svg" height="30" /> |FXCM | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/forex/fxcm.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lmax_logo.svg" height="30" /> |LMAX | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/forex/lmax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/oanda_logo.svg" height="30" /> |Oanda | <a href="https://doc.stocksharp.com/zh/topics/api/connectors/forex/oanda.html" target="_blank">Docs</a> |

  [1]: https://stocksharp.com/zh
  [4]: https://stocksharp.com/zh/edu/
  [5]: https://stocksharp.com/zh/forum/
  [6]: https://stocksharp.com/zh/broker/
  [8]: https://stocksharp.com/zh/store/strategy-designer/
  [9]: https://stocksharp.com/zh/store/market-data-downloader/
  [10]: https://stocksharp.com/zh/store/trading-terminal/
  [11]: https://stocksharp.com/zh/store/trading-shell/
  [12]: https://stocksharp.com/zh/store/api/
