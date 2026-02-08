<img src="./Media/SLogo.png" align="right" />

# [StockSharp - торговая платформа][1]

## [English](README.md) | **Русский** | [中文](README.zh.md)

## <a href="https://doc.stocksharp.ru" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4d6.svg" alt="Docs" height="40"/> Документация</a> <a href="https://stocksharp.ru/products/download/" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4be.svg" alt="Download" height="40"/> Скачать</a> <a href="https://t.me/stocksharpchat/1" style="margin-right:15px;"><img src="https://raw.githubusercontent.com/twitter/twemoji/master/assets/svg/1f4ac.svg" alt="Chat" height="40"/> Чат</a> <a href="https://vkvideo.ru/@stocksharp"><img src="https://raw.githubusercontent.com/edent/SuperTinyIcons/master/images/svg/youtube.svg" alt="Video" height="40"/> Видео</a>

## Введение ##

**StockSharp** (кратко **S#**) – это **бесплатная** платформа для торговли на любых рынках мира (криптобиржи, американские, европейские, азиатские, российские биржи акций, фьючерсов, опционов, Биткоин, форекс и т.д.). Вы сможете торговать вручную или автоматически (алгоритмические торговые роботы, обычные или высокочастотные HFT).

**Доступные подключения**: Binance, MT4, MT5, FIX/FAST, PolygonIO, Trading Technologies, Alpaca Markets, BarChart, CQG, E*Trade, IQFeed, InteractiveBrokers, LMAX, MatLab, Oanda, FXCM, Rithmic, cTrader, DXtrade, BitStamp, Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, OKX, Coincheck, CEX.IO, BitMEX, YoBit, Livecoin, EXMO, Deribit, HTX, KuCoin, QuantFEED, Aster, edgeX, Ligther, Paradex, Hyperliquid и многие другие.

## [Designer][8]
<img src="./Media/Designer500.gif" align="left" />

**Designer** - **бесплатное** универсальное приложение для создания алгоритмических стратегий:
  - Визуальный конструктор для создания стратегий кликами мыши
  - Встроенный редактор C#
  - Простое создание собственных индикаторов
  - Встроенный отладчик
  - Подключения к множеству электронных площадок и брокеров
  - Все мировые площадки
  - Обмен схемами с командой

## [Hydra][9]
<img src="./Media/Hydra500.gif" align="right" />

**Hydra** - **бесплатное** ПО для автоматической загрузки и хранения рыночных данных:
  - Поддержка множества источников
  - Высокая степень сжатия
  - Любые типы данных
  - Программный доступ к сохраненным данным через API
  - Экспорт в csv, excel, xml или базу данных
  - Импорт из csv
  - Запланированные задачи
  - Автосинхронизация через Интернет между несколькими экземплярами Hydra

## [Terminal][10]
<img src="./Media/Terminal500.gif" align="left" />

**Terminal** - **бесплатное** приложение для торговли с графиками (торговый терминал):
  - Подключения к множеству электронных площадок и брокеров
  - Торговля с графиков кликами
  - Произвольные таймфреймы
  - Свечи Volume, Tick, Range, P&F, Renko
  - Кластерные графики
  - Box графики
  - Volume Profile

## [Shell][11]
<img src="./Media/Shell500.gif" align="right" />

**Shell** - готовый графический фреймворк с возможностью быстрой настройки под ваши нужды и с полностью открытым исходным кодом на C#:
  - Полный исходный код
  - Поддержка всех подключений платформы StockSharp
  - Поддержка схем Designer
  - Гибкий пользовательский интерфейс
  - Тестирование стратегий (статистика, эквити, отчеты)
  - Сохранение и загрузка настроек стратегий
  - Запуск стратегий параллельно
  - Детальная информация о производительности стратегий
  - Запуск стратегий по расписанию

## [API][12]
API - это **бесплатная** библиотека C# для программистов, использующих Visual Studio. API позволяет создавать любые торговые стратегии, от долгосрочных позиционных стратегий до высокочастотных стратегий (HFT) с прямым доступом к бирже (DMA). [Подробнее...][12]

### Пример коннектора
```C#
var connector = new Connector();
var security = connector.LookupById("AAPL@NASDAQ");

var subscription = new Subscription(DataType.TimeFrame(TimeSpan.FromMinutes(1)), security);

connector.CandleReceived += (sub, candle) =>
{
        if (sub != subscription || candle.State != CandleStates.Finished)
                return;

        // определяем цвет свечи
        var isGreen = candle.ClosePrice > candle.OpenPrice;

        // регистрируем рыночную заявку в зависимости от цвета свечи
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

## Криптобиржи
|Лого | Название | Документация |
|:---:|:--------:|:------------:|
|<img src="./Media/logos/bibox_logo.svg" height="30" /> |Bibox | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bibox.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Binance_logo.svg" height="30" /> |Binance | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/binance.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bingx_logo.svg" height="30" /> |BingX | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bingx.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitalong_logo.svg" height="30" /> |Bitalong | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitalong.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitbank_logo.svg" height="30" /> |Bitbank | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitbank.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitget_logo.svg" height="30" /> |Bitget | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitget.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitexbook_logo.svg" height="30" /> |Bitexbook | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitexbook.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitfinex_logo.svg" height="30" /> |Bitfinex | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitfinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bithumb_logo.svg" height="30" /> |Bithumb | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bithumb.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitmax_logo.svg" height="30" /> |BitMax | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitmax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bitmex_logo.svg" height="30" /> |BitMEX | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitmex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/BitStamp_logo.svg" height="30" /> |BitStamp | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitstamp.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bittrex_logo.svg" height="30" /> |Bittrex | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bittrex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/BitZ_logo.png" height="30" /> |BitZ | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bitz.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bybit_logo.svg" height="30" /> |ByBit | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bybit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bw_logo.svg" height="30" /> |BW | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/bw.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cexio_logo.svg" height="30" /> |CEX.IO | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/cex.io.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinbase_logo.svg" height="30" /> |Coinbase | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinbase.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinbene_logo.svg" height="30" /> |CoinBene | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinbene.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coincap_logo.svg" height="30" /> |CoinCap | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coincap.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coincheck_logo.svg" height="30" /> |Coincheck | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coincheck.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinex_logo.svg" height="30" /> |CoinEx | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/CoinExchange_logo.png" height="30" /> |CoinExchange | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinexchange.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinigy_logo.svg" height="30" /> |Coinigy  | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinigy.html" target="_blank">Docs</a> |
|<img src="./Media/logos/coinhub_logo.svg" height="30" /> |CoinHub | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/coinhub.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cryptopia_logo.svg" height="30" /> |Cryptopia | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/cryptopia.html" target="_blank">Docs</a> |
|<img src="./Media/logos/deribit_logo.svg" height="30" /> |Deribit | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/deribit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/digifinex_logo.svg" height="30" /> |DigiFinex | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/digifinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/digitexfutures_logo.svg" height="30" /> |DigitexFutures | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/digitexfutures.html" target="_blank">Docs</a> |
|<img src="./Media/logos/exmo_logo.svg" height="30" /> |EXMO | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/exmo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fatbtc_logo.svg" height="30" /> |FatBTC | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/fatbtc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gateio_logo.svg" height="30" /> |GateIO | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/gateio.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gdax_logo.svg" height="30" /> |GDAX | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/gdax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/gopax_logo.svg" height="30" /> |GOPAX | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/gopax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hitbtc_logo.svg" height="30" /> |HitBTC | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/hitbtc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/hotbit_logo.svg" height="30" /> |Hotbit | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/hotbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/huobi_logo.svg" height="30" /> |Huobi | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/huobi.html" target="_blank">Docs</a> |
|<img src="./Media/logos/idax_logo.svg" height="30" /> |IDAX | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/idax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kraken_logo.svg" height="30" /> |Kraken | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/kraken.html" target="_blank">Docs</a> |
|<img src="./Media/logos/kucoin_logo.svg" height="30" /> |KuCoin | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/kucoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/latoken_logo.svg" height="30" /> |LATOKEN | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/latoken.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lbank_logo.svg" height="30" /> |LBank | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/lbank.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Liqui_logo.png" height="30" /> |Liqui | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/liqui.html" target="_blank">Docs</a> |
|<img src="./Media/logos/livecoin_logo.svg" height="30" /> |Livecoin | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/livecoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mexc_logo.svg" height="30" /> |MEXC | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/mexc.html" target="_blank">Docs</a> |
|<img src="./Media/logos/okcoin_logo.svg" height="30" /> |OKCoin | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/okcoin.html" target="_blank">Docs</a> |
|<img src="./Media/logos/okex_logo.svg" height="30" /> |OKEx | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/okex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/poloniex_logo.svg" height="30" /> |Poloniex | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/poloniex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/prizmbit_logo.svg" height="30" /> |PrizmBit | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/prizmbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/liquid_logo.svg" height="30" /> |QuoineX | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/quoinex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradeogre_logo.svg" height="30" /> |TradeOgre | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/tradeogre.html" target="_blank">Docs</a> |
|<img src="./Media/logos/upbit_logo.svg" height="30" /> |Upbit | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/upbit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/yobit_logo.svg" height="30" /> |YoBit | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/yobit.html" target="_blank">Docs</a> |
|<img src="./Media/logos/zaif_logo.svg" height="30" /> |Zaif | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/zaif.html" target="_blank">Docs</a> |
|<img src="./Media/logos/zb_logo.svg" height="30" /> |ZB | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/zb.html" target="_blank">Docs</a> |

## DEX exchanges
|Logo | Name | Documentation |
|:---:|:----:|:-------------:|
|<img src="./Media/logos/Aster_logo.svg" height="30" /> |Aster | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/aster.html" target="_blank">Docs</a> |
|<img src="./Media/logos/edgeX_logo.svg" height="30" /> |edgeX | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/edgex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Ligther_logo.svg" height="30" /> |Ligther | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/ligther.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Paradex_logo.svg" height="30" /> |Paradex | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/paradex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Hyperliquid_logo.svg" height="30" /> |Hyperliquid | <a href="https://doc.stocksharp.ru/topics/api/connectors/crypto_exchanges/hyperliquid.html" target="_blank">Docs</a> |


## Акции, фьючерсы и опционы
|Лого | Название | Документация |
|:---:|:--------:|:------------:|
|<img src="./Media/logos/polygonio_logo.svg" height="30" /> |Polygon.io | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/polygonio.html" target="_blank">Docs</a> |
|<img src="./Media/logos/alpaca_logo.svg" height="30" /> |Alpaca.Markets | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/alpaca.html" target="_blank">Docs</a> |
|<img src="./Media/logos/interactivebrokers_logo.svg" height="30" /> |Interactive Brokers | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/interactive_brokers.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fix_logo.svg" height="30" /> |FIX protocol (4.2, 4.4. 5.0) | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/fix_protocol.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fix_logo.svg" height="30" /> |FAST protocol | <a href="https://doc.stocksharp.ru/topics/api/connectors/common/fast_protocol.html" target="_blank">Docs</a> |
|<img src="./Media/logos/bvmt_logo.svg" height="30" /> |BVMT | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/bvmt.html" target="_blank">Docs</a> |
|<img src="./Media/logos/alphavantage_logo.svg" height="30" /> |AlphaVantage | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/alphavantage.html" target="_blank">Docs</a> |
|<img src="./Media/logos/barchart_logo.svg" height="30" /> |BarChart | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/barchart.html" target="_blank">Docs</a> |
|<img src="./Media/logos/cqg_logo.svg" height="30" /> |CQG | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/cqg.html" target="_blank">Docs</a> |
|<img src="./Media/logos/etrade_logo.svg" height="30" /> |E*TRADE | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/e_trade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/google_logo.svg" height="30" /> |Google | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/google.html" target="_blank">Docs</a> |
|<img src="./Media/logos/iex_logo.svg" height="30" /> |IEX | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/iex.html" target="_blank">Docs</a> |
|<img src="./Media/logos/iqfeed_logo.svg" height="30" /> |IQFeed | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/iqfeed.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Lse_logo.svg" height="30" /> |ITCH | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/itch.html" target="_blank">Docs</a> |
|<img src="./Media/logos/OpenECry_logo.png" height="30" /> |OpenECry | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/openecry.html" target="_blank">Docs</a> |
|<img src="./Media/logos/quandl_logo.svg" height="30" /> |Quandl | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/quandl.html" target="_blank">Docs</a> |
|<img src="./Media/logos/quanthouse_logo.png" height="30" /> |QuantFEED | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/quantfeed.html" target="_blank">Docs</a> |
|<img src="./Media/logos/rithmic_logo.svg" height="30" /> |Rithmic | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/rithmic.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Sterling_logo.png" height="30" /> |Sterling | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/sterling.html" target="_blank">Docs</a> |
|<img src="./Media/logos/tradier_logo.svg" height="30" /> |Tradier | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/tradier.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Xignite_logo.png" height="30" /> |Xignite | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/xignite.html" target="_blank">Docs</a> |
|<img src="./Media/logos/yahoo_logo.svg" height="30" /> |Yahoo | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/yahoo.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Blackwood_logo.png" height="30" /> |Blackwood (Fusion) | <a href="https://doc.stocksharp.ru/topics/api/connectors/stock_market/blackwood_fusion.html" target="_blank">Docs</a> |

## Российский рынок
|Лого | Название |  Документация Ru|
|:---:|:--------:|:---------------:|
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

## Форекс
|Лого | Название | Документация |
|:---:|:--------:|:------------:|
|<img src="./Media/logos/devexperts_logo.svg" height="30" /> |DXtrade | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/dxtrade.html" target="_blank">Docs</a> |
|<img src="./Media/logos/ctrader_logo.svg" height="30" /> |cTrader | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/ctrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mt4_logo.svg" height="30" /> |MT4 | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/metatrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/mt5_logo.svg" height="30" /> |MT5 | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/metatrader.html" target="_blank">Docs</a> |
|<img src="./Media/logos/dukascopy_logo.svg" height="30" /> |DukasCopy | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/dukascopy.html" target="_blank">Docs</a> |
|<img src="./Media/logos/fxcm_logo.svg" height="30" /> |FXCM | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/fxcm.html" target="_blank">Docs</a> |
|<img src="./Media/logos/lmax_logo.svg" height="30" /> |LMAX | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/lmax.html" target="_blank">Docs</a> |
|<img src="./Media/logos/Oanda_logo.svg" height="30" /> |Oanda | <a href="https://doc.stocksharp.ru/topics/api/connectors/forex/oanda.html" target="_blank">Docs</a> |

  [1]: https://stocksharp.ru
  [4]: https://stocksharp.ru/edu/
  [5]: https://stocksharp.ru/forum/
  [6]: https://stocksharp.ru/broker/
  [8]: https://stocksharp.ru/store/strategy-designer/
  [9]: https://stocksharp.ru/store/market-data-downloader/
  [10]: https://stocksharp.ru/store/trading-terminal/
  [11]: https://stocksharp.ru/store/trading-shell/
  [12]: https://stocksharp.ru/store/api/