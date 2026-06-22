<img src="./Media/SLogo.png" align="right" />

# [StockSharp - トレーディングプラットフォーム][1]

## [English](README.md) | [Русский](README.ru.md) | [中文](README.zh.md) | **日本語**

## <a href="https://doc.stocksharp.com" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4d6.svg" alt="Docs" height="40"/> ドキュメント</a> <a href="https://stocksharp.com/products/download/" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4be.svg" alt="Download" height="40"/> ダウンロード</a> <a href="https://t.me/stocksharpchat/361" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4ac.svg" alt="Chat" height="40"/> チャット</a> <a href="https://www.youtube.com/@stocksharp"><img src="https://raw.githubusercontent.com/edent/SuperTinyIcons/master/images/svg/youtube.svg" alt="YouTube" height="40"/> YouTube</a>

## はじめに ##

**StockSharp**（略称 **S#**）は、世界中のあらゆる市場（暗号資産取引所、米国、欧州、アジア、ロシア、株式、先物、オプション、ビットコイン、外国為替など）でトレーディングを行うための**無料**プラットフォームです。手動取引はもちろん、自動取引（アルゴリズムトレーディングロボット、通常取引またはHFT）も可能です。

**利用可能な接続先**: Binance、MT4、MT5、FIX/FAST、PolygonIO、Trading Technologies、Alpaca Markets、BarChart、CQG、E*Trade、IQFeed、InteractiveBrokers、LMAX、MatLab、Oanda、FXCM、Rithmic、cTrader、DXtrade、BitStamp、Bitfinex、Coinbase、Kraken、Poloniex、GDAX、Bittrex、Bithumb、OKX、Coincheck、CEX.IO、BitMEX、YoBit、Livecoin、EXMO、Deribit、HTX、KuCoin、QuantFEED、Aster、edgeX、Ligther、Paradex、Hyperliquid、その他多数。

## [Designer][8]
<img src="./Media/Designer500.gif" align="left" />

**Designer** - 簡単に戦略を作成できる**無料**の汎用アルゴリズムトレーディング戦略アプリケーション:
  - マウスクリックで戦略を作成できるビジュアルデザイナー
  - 組み込みC#エディター
  - 独自インジケーターを簡単に作成可能
  - 組み込みデバッガー
  - 複数の電子取引所およびブローカーへの接続
  - 世界中のあらゆるプラットフォームに対応
  - チームとのスキーマ共有

## [Hydra][9]
<img src="./Media/Hydra500.gif" align="right" />

**Hydra** - マーケットデータを自動的に読み込み・保存する**無料**ソフトウェア:
  - 多数のデータソースをサポート
  - 高い圧縮率
  - あらゆるデータ型に対応
  - API経由で保存データにプログラムアクセス
  - csv、excel、xml、データベースへのエクスポート
  - csvからのインポート
  - スケジュールタスク
  - 複数のHydraインスタンス間でインターネット越しの自動同期

## [Terminal][10]
<img src="./Media/Terminal500.gif" align="left" />

**Terminal** - **無料**のトレーディングチャートアプリケーション（トレーディングターミナル）:
  - 複数の電子取引所およびブローカーへの接続
  - チャート上のクリックによる取引
  - 任意の時間足
  - 出来高、ティック、レンジ、P&F、練行足ローソク
  - クラスターチャート
  - ボックスチャート
  - ボリュームプロファイル
  
## [Shell][11]
<img src="./Media/Shell500.gif" align="right" />

**Shell** - ニーズに応じて素早くカスタマイズ可能で、C#で記述された完全オープンソースのグラフィカルフレームワーク:
  - 完全なソースコード
  - すべてのStockSharpプラットフォーム接続をサポート
  - Designerスキーマをサポート
  - 柔軟なユーザーインターフェース
  - 戦略テスト（統計、エクイティ、レポート）
  - 戦略設定の保存および読み込み
  - 戦略の並列実行
  - 戦略パフォーマンスの詳細情報
  - スケジュールによる戦略の起動

## [API][12]
APIはVisual Studioを使用するプログラマー向けの**無料**C#ライブラリです。APIを使用すれば、長期の時間足を使うポジション戦略から、取引所への直接アクセス（DMA）を伴う高頻度取引戦略（HFT）まで、あらゆるトレーディング戦略を作成できます。[詳細はこちら...][12]
### Connector の例
```C#
var connector = new Connector();
var security = connector.LookupById("AAPL@NASDAQ");

var subscription = new Subscription(DataType.TimeFrame(TimeSpan.FromMinutes(1)), security);

connector.CandleReceived += (sub, candle) =>
{
        if (sub != subscription || candle.State != CandleStates.Finished)
                return;

        // ローソク足の色を判定
        var isGreen = candle.ClosePrice > candle.OpenPrice;

        // ローソク足の色に応じて成行注文を発注
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

## 暗号資産取引所
|ロゴ | 名称 | ドキュメント |
|:---:|:----:|:-------------:|
|<img src="./Media/logos/bibox_logo.svg" height="30" /> |Bibox | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bibox.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Binance_logo.svg" height="30" /> |Binance | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/binance.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bingx_logo.svg" height="30" /> |BingX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bingx.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitalong_logo.svg" height="30" /> |Bitalong | <a href="https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitalong.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitbank_logo.svg" height="30" /> |Bitbank | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitbank.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitget_logo.svg" height="30" /> |Bitget | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitget.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitexbook_logo.svg" height="30" /> |Bitexbook | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitexbook.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitfinex_logo.svg" height="30" /> |Bitfinex | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitfinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bithumb_logo.svg" height="30" /> |Bithumb | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bithumb.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitmax_logo.svg" height="30" /> |BitMax | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitmax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitmex_logo.svg" height="30" /> |BitMEX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitmex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/BitStamp_logo.svg" height="30" /> |BitStamp | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitstamp.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bittrex_logo.svg" height="30" /> |Bittrex | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bittrex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/BitZ_logo.png" height="30" /> |BitZ | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitz.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bybit_logo.svg" height="30" /> |ByBit | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bybit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bw_logo.svg" height="30" /> |BW | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bw.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cexio_logo.svg" height="30" /> |CEX.IO | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/cex.io.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinbase_logo.svg" height="30" /> |Coinbase | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinbase.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinbene_logo.svg" height="30" /> |CoinBene | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinbene.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coincap_logo.svg" height="30" /> |CoinCap | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coincap.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coincheck_logo.svg" height="30" /> |Coincheck | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coincheck.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinex_logo.svg" height="30" /> |CoinEx | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/CoinExchange_logo.png" height="30" /> |CoinExchange | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinexchange.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinigy_logo.svg" height="30" /> |Coinigy  | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinigy.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinhub_logo.svg" height="30" /> |CoinHub | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinhub.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cryptopia_logo.svg" height="30" /> |Cryptopia | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/cryptopia.html" target="_blank">Docs</a> |
|<img src="./Media/logos/deribit_logo.svg" height="30" /> |Deribit | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/deribit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/digifinex_logo.svg" height="30" /> |DigiFinex | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/digifinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/digitexfutures_logo.svg" height="30" /> |DigitexFutures | <a href="https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/digitexfutures.html" target="_blank">Docs</a> |
|<img src="./Media/logos/exmo_logo.svg" height="30" /> |EXMO | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/exmo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fatbtc_logo.svg" height="30" /> |FatBTC | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/fatbtc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gateio_logo.svg" height="30" /> |GateIO | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/gateio.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gdax_logo.svg" height="30" /> |GDAX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/gdax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gopax_logo.svg" height="30" /> |GOPAX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/gopax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hitbtc_logo.svg" height="30" /> |HitBTC | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/hitbtc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hotbit_logo.svg" height="30" /> |Hotbit | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/hotbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/huobi_logo.svg" height="30" /> |Huobi | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/huobi.html" target="_blank">Docs</a> |
|<img src="./Media/logos/idax_logo.svg" height="30" /> |IDAX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/idax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kraken_logo.svg" height="30" /> |Kraken | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/kraken.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kucoin_logo.svg" height="30" /> |KuCoin | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/kucoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/latoken_logo.svg" height="30" /> |LATOKEN | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/latoken.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lbank_logo.svg" height="30" /> |LBank | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/lbank.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Liqui_logo.png" height="30" /> |Liqui | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/liqui.html" target="_blank">Docs</a> |
|<img src="./Media/logos/livecoin_logo.svg" height="30" /> |Livecoin | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/livecoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mexc_logo.svg" height="30" /> |MEXC | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/mexc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/okcoin_logo.svg" height="30" /> |OKCoin | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/okcoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/okex_logo.svg" height="30" /> |OKEx | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/okex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/poloniex_logo.svg" height="30" /> |Poloniex | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/poloniex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/prizmbit_logo.svg" height="30" /> |PrizmBit | <a href="https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/prizmbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/liquid_logo.svg" height="30" /> |QuoineX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/quoinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradeogre_logo.svg" height="30" /> |TradeOgre | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/tradeogre.html" target="_blank">Docs</a> |
|<img src="./Media/logos/upbit_logo.svg" height="30" /> |Upbit | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/upbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/yobit_logo.svg" height="30" /> |YoBit | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/yobit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/zaif_logo.svg" height="30" /> |Zaif | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/zaif.html" target="_blank">Docs</a> |
|<img src="./Media/logos/zb_logo.svg" height="30" /> |ZB | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/zb.html" target="_blank">Docs</a> |

## DEX取引所
|ロゴ | 名称 | ドキュメント |
|:---:|:----:|:-------------:|
|<img src="./Media/logos/Aster_logo.svg" height="30" /> |Aster | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/aster.html" target="_blank">Docs</a> |
|<img src="./Media/logos/edgeX_logo.svg" height="30" /> |edgeX | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/edgex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Ligther_logo.svg" height="30" /> |Ligther | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/ligther.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Paradex_logo.svg" height="30" /> |Paradex | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/paradex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Hyperliquid_logo.svg" height="30" /> |Hyperliquid | <a href="//doc.stocksharp.com/topics/api/connectors/crypto_exchanges/hyperliquid.html" target="_blank">Docs</a> |

## 株式・先物・オプション
|ロゴ | 名称 | ドキュメント |
|:---:|:----:|:-------------:|
|<img src="./Media/logos/polygonio_logo.svg" height="30" /> |Polygon.io | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/polygonio.html" target="_blank">Docs</a> |
|<img src="./Media/logos/alpaca_logo.svg" height="30" /> |Alpaca.Markets | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/alpaca.html" target="_blank">Docs</a> |
|<img src="./Media/logos/interactivebrokers_logo.svg" height="30" /> |Interactive Brokers | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/interactive_brokers.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fix_logo.svg" height="30" /> |FIXプロトコル (4.2, 4.4. 5.0) | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/fix_protocol.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fix_logo.svg" height="30" /> |FASTプロトコル | <a href="//doc.stocksharp.com/topics/api/connectors/common/fast_protocol.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bvmt_logo.svg" height="30" /> |BVMT | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/bvmt.html" target="_blank">Docs</a> |
|<img src="./Media/logos/alphavantage_logo.svg" height="30" /> |AlphaVantage | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/alphavantage.html" target="_blank">Docs</a> |
|<img src="./Media/logos/barchart_logo.svg" height="30" /> |BarChart | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/barchart.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cqg_logo.svg" height="30" /> |CQG | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/cqg.html" target="_blank">Docs</a> |
|<img src="./Media/logos/etrade_logo.svg" height="30" /> |E*TRADE | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/e_trade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/google_logo.svg" height="30" /> |Google | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/google.html" target="_blank">Docs</a> |
|<img src="./Media/logos/iex_logo.svg" height="30" /> |IEX | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/iex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/iqfeed_logo.svg" height="30" /> |IQFeed | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/iqfeed.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Lse_logo.svg" height="30" /> |ITCH | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/itch.html" target="_blank">Docs</a> |
|<img src="./Media/logos/OpenECry_logo.png" height="30" /> |OpenECry | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/openecry.html" target="_blank">Docs</a> |
|<img src="./Media/logos/quandl_logo.svg" height="30" /> |Quandl | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/quandl.html" target="_blank">Docs</a> |
|<img src="./Media/logos/quanthouse_logo.png" height="30" /> |QuantFEED | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/quantfeed.html" target="_blank">Docs</a> |
|<img src="./Media/logos/rithmic_logo.svg" height="30" /> |Rithmic | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/rithmic.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Sterling_logo.png" height="30" /> |Sterling | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/sterling.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradier_logo.svg" height="30" /> |Tradier | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/tradier.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Xignite_logo.png" height="30" /> |Xignite | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/xignite.html" target="_blank">Docs</a> |
|<img src="./Media/logos/yahoo_logo.svg" height="30" /> |Yahoo | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/yahoo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Blackwood_logo.png" height="30" /> |Blackwood (Fusion) | <a href="//doc.stocksharp.com/topics/api/connectors/stock_market/blackwood_fusion.html" target="_blank">Docs</a> |


## FX（外国為替）
|ロゴ | 名称 | ドキュメント |
|:---:|:----:|:-------------:|
|<img src="./Media/logos/devexperts_logo.svg" height="30" /> |DXtrade | <a href="//doc.stocksharp.com/topics/api/connectors/forex/dxtrade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ctrader_logo.svg" height="30" /> |cTrader | <a href="//doc.stocksharp.com/topics/api/connectors/forex/ctrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mt4_logo.svg" height="30" /> |MT4 | <a href="//doc.stocksharp.com/topics/api/connectors/forex/metatrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mt5_logo.svg" height="30" /> |MT5 | <a href="//doc.stocksharp.com/topics/api/connectors/forex/metatrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/dukascopy_logo.svg" height="30" /> |DukasCopy | <a href="//doc.stocksharp.com/topics/api/connectors/forex/dukascopy.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fxcm_logo.svg" height="30" /> |FXCM | <a href="//doc.stocksharp.com/topics/api/connectors/forex/fxcm.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lmax_logo.svg" height="30" /> |LMAX | <a href="//doc.stocksharp.com/topics/api/connectors/forex/lmax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Oanda_logo.svg" height="30" /> |Oanda | <a href="//doc.stocksharp.com/topics/api/connectors/forex/oanda.html" target="_blank">Docs</a> |

  [1]: https://stocksharp.com
  [4]: https://stocksharp.com/edu/
  [5]: https://stocksharp.com/forum/
  [6]: https://stocksharp.com/broker/
  [8]: https://stocksharp.com/store/strategy-designer/
  [9]: https://stocksharp.com/store/market-data-downloader/
  [10]: https://stocksharp.com/store/trading-terminal/
  [11]: https://stocksharp.com/store/trading-shell/
  [12]: https://stocksharp.com/store/api/
