<img src="https://github.com/StockSharp/StockSharp/blob/master/Media/SLogo.png" align="right" />

# [StockSharp - trading platform][1] 
## [Documentation][2] | [Download][3] | [Support][7] | [Algotrading training][4]

## Introduction ##

**StockSharp** (shortly **S#**) – are **free** programs for trading at any markets of the world (American, European, Asian, Russian, stocks, futures, options, Bitcoins, forex, etc.). You will be able to trade manually or automated trading (algorithmic trading robots, conventional or HFT).

**Available connections**: FIX/FAST, ITCH (LSE, NASDAQ), Blackwood/Fusion, BarChart, CQG, E*Trade, IQFeed, InteractiveBrokers, LMAX, MatLab, Oanda, FXCM, OpenECry, Rithmic, RSS, Sterling, BTCE, BitStamp, Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck, Binance, Liqui, CEX.IO, Cryptopia, OKEx, BitMEX, YoBit, Livecoin, EXMO, Deribit, Huobi, KuCoin, BITEXBOOK, CoinExchange, QuantFEED and many other.

## [S#.Designer][8]
<img src="https://github.com/StockSharp/StockSharp/blob/master/Media/Designer500.gif" align="left" />

**S#.Designer** - **free** universal algorithmic strategies application for easy strategy creation::
  - Visual designer to create strategies by mouse clicking
  - Embedded C# editor
  - Easy to create own indicators
  - Build in debugger
  - Connections to the multiple electronic boards and brokers
  - All world platforms
  - Schema sharing with own team

## [S#.Data][9]
<img src="https://github.com/StockSharp/StockSharp/blob/master/Media/Hydra500.gif" align="right" />

**S#.Data** - **free** software to automatically load and store market data:
  - Supports many sources
  - High compression ratio
  - Any data type
  - Program access to stored data via API
  - Export to csv, excel, xml or database
  - Import from csv
  - Scheduled tasks
  - Auto-sync over the Internet between several running programs S#.Data

## [S#.Terminal][10]
<img src="https://github.com/StockSharp/StockSharp/blob/master/Media/Terminal500.gif" align="left" />

**Terminal** - **free** trading charting application (trading terminal):
  - Connections to the multiple electronic boards and brokers
  - Trading from charts by clicking
  - Arbitrary timeframes
  - Volume, Tick, Range, P&F, Renko candles
  - Cluster charts
  - Box charts
  - Volume Profile
  
## [S#.Shell][11]
<img src="https://github.com/StockSharp/StockSharp/blob/master/Media/Shell500.gif" align="right" />

**S#.Shell** - the ready-made graphical framework with the ability to quickly change to your needs and with fully open source code in C#:
  - Complete source code
  - Support for all StockSharp platform connections
  - Support for S#.Designer schemas
  - Flexible user interface
  - Strategy testing (statistics, equity, reports)
  - Save and load strategy settings
  - Launch strategies in parallel
  - Detailed information on strategy performance 
  - Launch strategies on schedule

## [S#.API][12]
S#.API is a **free** C# library for programmers who use Visual Studio. S#.API lets you create any trading strategy, from long-timeframe positional strategies to high frequency strategies (HFT) with direct access to the exchange (DMA). [More info...](https://stocksharp.com/products/api/)
### Strategy example
```C#
public class SimpleStrategy : Strategy
{
	[Display(Name = "CandleSeries",
		 GroupName = "Base settings")]
	public CandleSeries CandleSeries { get; set; }
	public SimpleStrategy(){}

	protected override void OnStarted()
	{
		var connector = (Connector)Connector;
		connector.WhenCandlesFinished(CandleSeries).Do(CandlesFinished).Apply(this);
		connector.SubscribeCandles(CandleSeries);
		base.OnStarted();
	}

	private void CandlesFinished(Candle candle)
	{
		if (candle.OpenPrice < candle.ClosePrice && Position <= 0)
		{
			RegisterOrder(this.BuyAtMarket(Volume + Math.Abs(Position)));
		}
		else if (candle.OpenPrice > candle.ClosePrice && Position >= 0)
		{
			RegisterOrder(this.SellAtMarket(Volume + Math.Abs(Position)));
		}
	}
}
```

## Development stage

Current stage of all components - [RELEASE_STAGES.md](../master/_ReleaseNotes/RELEASE_STAGES.md).
Release notes - [RELEASE_NOTES.md](../master/_ReleaseNotes/CHANGE_LOG_API.md).

## License

StockSharp code is licensed under the [Apache License 2.0](../master/LICENSE).

  [1]: https://stocksharp.com
  [2]: https://doc.stocksharp.com
  [3]: https://github.com/StockSharp/StockSharp/releases
  [4]: https://stocksharp.com/edu/
  [5]: https://stocksharp.com/forum/
  [6]: https://stocksharp.com/broker/
  [7]: https://stocksharp.com/support/
  [8]: https://stocksharp.com/products/designer/
  [9]: https://stocksharp.com/products/hydra/
  [10]: https://stocksharp.com/products/terminal/
  [11]: https://stocksharp.com/products/shell/
  [12]: https://stocksharp.com/products/api/

