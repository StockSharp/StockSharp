<img src="./Media/SLogo.png" align="right" />

# [StockSharp - trading platform][1] 
## [Documentation][2] | [Download][3] | [Support][7] | [Algotrading training][4]

## Introduction ##

**StockSharp** (shortly **S#**) – are **free** programs for trading at any markets of the world (American, European, Asian, Russian, stocks, futures, options, Bitcoins, forex, etc.). You will be able to trade manually or automated trading (algorithmic trading robots, conventional or HFT).

**Available connections**: FIX/FAST, ITCH (LSE, NASDAQ), Blackwood/Fusion, BarChart, CQG, E*Trade, IQFeed, InteractiveBrokers, LMAX, MatLab, Oanda, FXCM, OpenECry, Rithmic, RSS, Sterling, BTCE, BitStamp, Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck, Binance, Liqui, CEX.IO, Cryptopia, OKEx, BitMEX, YoBit, Livecoin, EXMO, Deribit, Huobi, KuCoin, BITEXBOOK, CoinExchange, QuantFEED and many other.

## [S#.Designer][8]
<img src="./Media/Designer500.gif" align="left" />

**S#.Designer** - **free** universal algorithmic strategies application for easy strategy creation:
  - Visual designer to create strategies by mouse clicking
  - Embedded C# editor
  - Easy to create own indicators
  - Build in debugger
  - Connections to the multiple electronic boards and brokers
  - All world platforms
  - Schema sharing with own team

## [S#.Data][9]
<img src="./Media/Hydra500.gif" align="right" />

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
<img src="./Media/Terminal500.gif" align="left" />

**Terminal** - **free** trading charting application (trading terminal):
  - Connections to the multiple electronic boards and brokers
  - Trading from charts by clicking
  - Arbitrary timeframes
  - Volume, Tick, Range, P&F, Renko candles
  - Cluster charts
  - Box charts
  - Volume Profile
  
## [S#.Shell][11]
<img src="./Media/Shell500.gif" align="right" />

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
S#.API is a **free** C# library for programmers who use Visual Studio. S#.API lets you create any trading strategy, from long-timeframe positional strategies to high frequency strategies (HFT) with direct access to the exchange (DMA). [More info...][12]
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
## American Stock, Futures and Options
|Logo | Name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|<img src="./Media/logos/AlphaVantage_logo.png" height="30" /> |AlphaVantage | <a href="//doc.stocksharp.com/topics/AlphaVantage.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/AlphaVantage.html" target="_blank">Ru</a> |
|<img src="./Media/logos/BarChart_logo.png" height="30" /> |Bachart | <a href="//doc.stocksharp.com/topics/BarChart.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/BarChart.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Blackwood_logo.png" height="30" /> |Blackwood (Fusion) | <a href="//doc.stocksharp.com/topics/Blackwood.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Blackwood.html" target="_blank">Ru</a> |
|<img src="./Media/logos/CQG_logo.png" height="30" /> |CQG | <a href="//doc.stocksharp.com/topics/CQG.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/CQG.html" target="_blank">Ru</a> |
|<img src="./Media/logos/ETrade_logo.png" height="30" /> |E*TRADE | <a href="//doc.stocksharp.com/topics/ETrade.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/ETrade.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Google_logo.png" height="30" /> |Google | <a href="//doc.stocksharp.com/topics/Google.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Google.html" target="_blank">Ru</a> |
|<img src="./Media/logos/iex_logo.png" height="30" /> |IEX | <a href="//doc.stocksharp.com/topics/IEX.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/IEX.html" target="_blank">Ru</a> |
|<img src="./Media/logos/InteractiveBrokers_logo.png" height="30" /> |Interactive Brokers | <a href="//doc.stocksharp.com/topics/IB.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/IB.html" target="_blank">Ru</a> |
|<img src="./Media/logos/IQFeed_logo.png" height="30" /> |IQFeed | <a href="//doc.stocksharp.com/topics/IQFeed.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/IQFeed.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Itch_logo.png" height="30" /> |ITCH | <a href="//doc.stocksharp.com/topics/ITCH.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/ITCH.html" target="_blank">Ru</a> |
|<img src="./Media/logos/OpenECry_logo.png" height="30" /> |OpenECry | <a href="//doc.stocksharp.com/topics/OEC.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/OEC.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Quandl_logo.png" height="30" /> |Quandl | <a href="//doc.stocksharp.com/topics/Quandl.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Quandl.html" target="_blank">Ru</a> |
|<img src="./Media/logos/quanthouse_logo.png" height="30" /> |QuantFEED | <a href="//doc.stocksharp.com/topics/QuantFeed.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/QuantFeed.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Rithmic_logo.png" height="30" /> |Rithmic | <a href="//doc.stocksharp.com/topics/Rithmic.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Rithmic.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Sterling_logo.png" height="30" /> |Sterling | <a href="//doc.stocksharp.com/topics/Sterling.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Sterling.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Tradier_logo.png" height="30" /> |Tradier | <a href="//doc.stocksharp.com/topics/Tradier.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Tradier.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Xignite_logo.png" height="30" /> |Xignite | <a href="//doc.stocksharp.com/topics/Xignite.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Xignite.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Yahoo_logo.png" height="30" /> |Yahoo | <a href="//doc.stocksharp.com/topics/Yahoo.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Yahoo.html" target="_blank">Ru</a> |

## Russian Stock, Futures and Options
|Logo | Name |  Documentation Ru| 
|:---:|:----:|:---------------:|
|<img src="./Media/logos/Mfd_logo.png" height="30" /> |Mfd | <a href="https://doc.stocksharp.ru/topics/Mfd.html" target="_blank">Ru</a> |
|<img src="./Media/logos/micex_logo.png" height="30" /> |Micex (TEAP) | <a href="https://doc.stocksharp.ru/topics/Micex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/plaza_logo.png" height="30" /> |Plaza II | <a href="https://doc.stocksharp.ru/topics/Plaza.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Quik_logo.png" height="30" /> |Quik |  <a href="https://doc.stocksharp.ru/topics/Quik.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Quik_logo.png" height="30" /> |Quik FIX |  <a href="https://doc.stocksharp.ru/topics/QuikFix.html" target="_blank">Ru</a> |
|<img src="./Media/logos/SmartCom_logo.png" height="30" /> |SmartCOM |  <a href="https://doc.stocksharp.ru/topics/Smart.html" target="_blank">Ru</a> |
|<img src="./Media/logos/spbex_logo.png" height="30" /> |SPB Exchange |  <a href="https://doc.stocksharp.ru/topics/SpbEx.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Transaq_logo.png" height="30" /> |Transaq |  <a href="https://doc.stocksharp.ru/topics/Transaq.html" target="_blank">Ru</a> |
|<img src="./Media/logos/micex_logo.png" height="30" /> |Twime |  <a href="https://doc.stocksharp.ru/topics/TWIME.html" target="_blank">Ru</a> |
|<img src="./Media/logos/UkrExh_logo.png" height="30" /> |UX (сайт) | <a href="https://doc.stocksharp.ru/topics/UX.html" target="_blank">Ru</a> |
|<img src="./Media/logos/AlorHistory_logo.png" height="30" /> |Алор Трейд | <a href="https://doc.stocksharp.ru/topics/AlorHistory.html" target="_blank">Ru</a> |
|<img src="./Media/logos/AlfaDirect_logo.png" height="30" /> |Альфа-Директ | <a href="https://doc.stocksharp.ru/topics/Alfa.html" target="_blank">Ru</a> |
|<img src="./Media/logos/MoexLchi_logo.png" height="30" /> |ЛЧИ | <a href="https://doc.stocksharp.ru/topics/LCI.html" target="_blank">Ru</a> |
|<img src="./Media/logos/RtsHistory_logo.png" height="30" /> |РТС | <a href="https://doc.stocksharp.ru/topics/RTS.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Finam_logo.png" height="30" /> |Финам | <a href="https://doc.stocksharp.ru/topics/Finam.html" target="_blank">Ru</a> |

## Forex
|Logo | Name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|<img src="./Media/logos/DukasCopy_logo.png" height="30" /> |DukasCopy | <a href="//doc.stocksharp.com/topics/DukasCopy.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/DukasCopy.html" target="_blank">Ru</a> |
|<img src="./Media/logos/FinViz_logo.png" height="30" /> |FinViz | <a href="//doc.stocksharp.com/topics/FinViz.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/FinViz.html" target="_blank">Ru</a> |
|<img src="./Media/logos/fxcm_logo.png" height="30" /> |FXCM | <a href="//doc.stocksharp.com/topics/Fxcm.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Fxcm.html" target="_blank">Ru</a> |
|<img src="./Media/logos/GainCapital_logo.png" height="30" /> |GAIN Capital | <a href="//doc.stocksharp.com/topics/GAIN%20Capital.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/GAIN%20Capital.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Lmax_logo.png" height="30" /> |LMAX | <a href="//doc.stocksharp.com/topics/LMAX.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/LMAX.html" target="_blank">Ru</a> |
|<img src="./Media/logos/MBTrading_logo.png" height="30" /> |MB Trading | <a href="//doc.stocksharp.com/topics/MB%20Trading.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/MB%20Trading.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Oanda_logo.png" height="30" /> |Oanda | <a href="//doc.stocksharp.com/topics/Oanda.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Oanda.html" target="_blank">Ru</a> |
|<img src="./Media/logos/TrueFX_logo.png" height="30" /> |TrueFX | <a href="//doc.stocksharp.com/topics/TrueFX.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/TrueFX.html" target="_blank">Ru</a> |

## Cryptocurrencies
|Logo | Name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|<img src="./Media/logos/Bibox_logo.png" height="30" /> |Bibox | <a href="//doc.stocksharp.com/topics/Bibox.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Bibox.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Binance_logo.svg" height="30" /> |Binance | <a href="//doc.stocksharp.com/topics/Binance.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Binance.html" target="_blank">Ru</a> |
|<img src="./Media/logos/bitalong_logo.png" height="30" /> |Bitalong | <a href="https://doc.stocksharp.com/topics/Bitalong.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Bitalong.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Bitbank_logo.png" height="30" /> |Bitbank | <a href="//doc.stocksharp.com/topics/Bitbank.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Bitbank.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Bitexbook_logo.png" height="30" /> |Bitexbook | <a href="//doc.stocksharp.com/topics/Bitexbook.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Bitexbook.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Bitfinex_logo.png" height="30" /> |Bitfinex | <a href="//doc.stocksharp.com/topics/Bitfinex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Bitfinex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Bithumb_logo.png" height="30" /> |Bithumb | <a href="//doc.stocksharp.com/topics/Bithumb.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Bithumb.html" target="_blank">Ru</a> |
|<img src="./Media/logos/BitMax_logo.png" height="30" /> |BitMax | <a href="//doc.stocksharp.com/topics/BitMax.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/BitMax.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Bitmex_logo.png" height="30" /> |BitMEX | <a href="//doc.stocksharp.com/topics/Bitmex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Bitmex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/BitStamp_logo.svg" height="30" /> |BitStamp | <a href="//doc.stocksharp.com/topics/BitStamp.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/BitStamp.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Bittrex_logo.png" height="30" /> |Bittrex | <a href="//doc.stocksharp.com/topics/Bittrex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Bittrex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/BitZ_logo.png" height="30" /> |BitZ | <a href="//doc.stocksharp.com/topics/BitZ.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/BitZ.html" target="_blank">Ru</a> |
|<img src="./Media/logos/BW_logo.png" height="30" /> |BW | <a href="//doc.stocksharp.com/topics/BW.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/BW.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Cex_logo.png" height="30" /> |CEX.IO | <a href="//doc.stocksharp.com/topics/Cex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Cex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Coinbase_logo.png" height="30" /> |Coinbase | <a href="//doc.stocksharp.com/topics/Coinbase.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Coinbase.html" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinBene_logo.png" height="30" /> |CoinBene | <a href="//doc.stocksharp.com/topics/CoinBene.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/CoinBene.html" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinCap_logo.png" height="30" /> |CoinCap | <a href="//doc.stocksharp.com/topics/CoinCap.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/CoinCap.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Coincheck_logo.png" height="30" /> |Coincheck | <a href="//doc.stocksharp.com/topics/Coincheck.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Coincheck.html" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinEx_logo.png" height="30" /> |CoinEx | <a href="//doc.stocksharp.com/topics/CoinEx.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/CoinEx.html" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinExchange_logo.png" height="30" /> |CoinExchange | <a href="//doc.stocksharp.com/topics/CoinExchange.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/CoinExchange.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Coinigy_logo.png" height="30" /> |Coinigy  | <a href="//doc.stocksharp.com/topics/Coinigy.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Coinigy.html" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinHub_logo.png" height="30" /> |CoinHub | <a href="//doc.stocksharp.com/topics/CoinHub.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/CoinHub.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Cryptopia_logo.png" height="30" /> |Cryptopia | <a href="//doc.stocksharp.com/topics/Cryptopia.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Cryptopia.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Deribit_logo.png" height="30" /> |Deribit | <a href="//doc.stocksharp.com/topics/Deribit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Deribit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Digifinex_logo.png" height="30" /> |DigiFinex | <a href="//doc.stocksharp.com/topics/Digifinex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Digifinex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/digitexfutures_logo.png" height="30" /> |DigitexFutures | <a href="https://doc.stocksharp.com/topics/DigitexFutures.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/DigitexFutures.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Exmo_logo.png" height="30" /> |EXMO | <a href="//doc.stocksharp.com/topics/Exmo.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Exmo.html" target="_blank">Ru</a> |
|<img src="./Media/logos/FatBtc_logo.png" height="30" /> |FatBTC | <a href="//doc.stocksharp.com/topics/FatBTC.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/FatBTC.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Gdax_logo.png" height="30" /> |GDAX | <a href="//doc.stocksharp.com/topics/GDAX.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/GDAX.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Gopax_logo.png" height="30" /> |GOPAX | <a href="//doc.stocksharp.com/topics/Gopax.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Gopax.html" target="_blank">Ru</a> |
|<img src="./Media/logos/HitBtc_logo.png" height="30" /> |HitBTC | <a href="//doc.stocksharp.com/topics/HitBTC.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/HitBTC.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Hotbit_logo.png" height="30" /> |Hotbit | <a href="//doc.stocksharp.com/topics/Hotbit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Hotbit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Huobi_logo.png" height="30" /> |Huobi | <a href="//doc.stocksharp.com/topics/Huobi.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Huobi.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Idax_logo.png" height="30" /> |IDAX | <a href="//doc.stocksharp.com/topics/Idax.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Idax.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Kraken_logo.png" height="30" /> |Kraken | <a href="//doc.stocksharp.com/topics/Kraken.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Kraken.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Kucoin_logo.png" height="30" /> |KuCoin | <a href="//doc.stocksharp.com/topics/Kucoin.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Kucoin.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Latoken_logo.png" height="30" /> |LATOKEN | <a href="//doc.stocksharp.com/topics/Latoken.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Latoken.html" target="_blank">Ru</a> |
|<img src="./Media/logos/LBank_logo.png" height="30" /> |LBank | <a href="//doc.stocksharp.com/topics/LBank.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/LBank.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Liqui_logo.png" height="30" /> |Liqui | <a href="//doc.stocksharp.com/topics/Liqui.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Liqui.html" target="_blank">Ru</a> |
|<img src="./Media/logos/LiveCoin_logo.png" height="30" /> |Livecoin | <a href="//doc.stocksharp.com/topics/LiveCoin.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/LiveCoin.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Okcoin_logo.png" height="30" /> |OKCoin | <a href="//doc.stocksharp.com/topics/OKCoin.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/OKCoin.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Okex_logo.png" height="30" /> |OKEx | <a href="//doc.stocksharp.com/topics/Okex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Okex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/poloniex_logo.png" height="30" /> |Poloniex | <a href="//doc.stocksharp.com/topics/Poloniex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Poloniex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/prizmbit_logo.png" height="30" /> |PrizmBit | <a href="https://doc.stocksharp.com/topics/PrizmBit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/PrizmBit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Quoinex_logo.png" height="30" /> |QuoineX | <a href="//doc.stocksharp.com/topics/Quoinex.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Quoinex.html" target="_blank">Ru</a> |
|<img src="./Media/logos/TradeOgre_logo.png" height="30" /> |TradeOgre | <a href="//doc.stocksharp.com/topics/TradeOrge.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/TradeOrge.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Upbit_logo.png" height="30" /> |Upbit | <a href="//doc.stocksharp.com/topics/Upbit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Upbit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Yobit_logo.png" height="30" /> |YoBit | <a href="//doc.stocksharp.com/topics/Yobit.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Yobit.html" target="_blank">Ru</a> |
|<img src="./Media/logos/Zaif_logo.png" height="30" /> |Zaif | <a href="//doc.stocksharp.com/topics/Zaif.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/Zaif.html" target="_blank">Ru</a> |
|<img src="./Media/logos/ZB_logo.png" height="30" /> |ZB | <a href="//doc.stocksharp.com/topics/ZB.html" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/topics/ZB.html" target="_blank">Ru</a> |

## Development stage

Current stage of all components - [RELEASE_STAGES.md](../master/_ReleaseNotes/RELEASE_STAGES.md).
Release notes - [RELEASE_NOTES.md](../master/_ReleaseNotes/CHANGE_LOG_API.md).

## License

StockSharp code is licensed under the [Apache License 2.0](../master/LICENSE).

  [1]: https://stocksharp.com
  [2]: https://doc.stocksharp.com
  [3]: https://stocksharp.com/products/download/
  [4]: https://stocksharp.com/edu/
  [5]: https://stocksharp.com/forum/
  [6]: https://stocksharp.com/broker/
  [7]: https://stocksharp.com/support/
  [8]: https://stocksharp.com/store/strategy%20designer/
  [9]: https://stocksharp.com/store/hydra/
  [10]: https://stocksharp.com/store/trading%20terminal/
  [11]: https://stocksharp.com/store/trading%20shell/
  [12]: https://stocksharp.com/store/api/

