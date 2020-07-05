<img src="./Media/SLogo.png" align="right" />

# [StockSharp - trading platform][1] 
## [Documentation][2] | [Download][3] | [Support][7] | [Algotrading training][4]

## Introduction ##

**StockSharp** (shortly **S#**) – are **free** programs for trading at any markets of the world (American, European, Asian, Russian, stocks, futures, options, Bitcoins, forex, etc.). You will be able to trade manually or automated trading (algorithmic trading robots, conventional or HFT).

**Available connections**: FIX/FAST, ITCH (LSE, NASDAQ), Blackwood/Fusion, BarChart, CQG, E*Trade, IQFeed, InteractiveBrokers, LMAX, MatLab, Oanda, FXCM, OpenECry, Rithmic, RSS, Sterling, BTCE, BitStamp, Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck, Binance, Liqui, CEX.IO, Cryptopia, OKEx, BitMEX, YoBit, Livecoin, EXMO, Deribit, Huobi, KuCoin, BITEXBOOK, CoinExchange, QuantFEED and many other.

## [S#.Designer][8]
<img src="./Media/Designer500.gif" align="left" />

**S#.Designer** - **free** universal algorithmic strategies application for easy strategy creation::
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
## American Stock, Futures and Options
|Logo | Name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|<img src="./Media/logos/AlphaVantage_logo.png" height="30" /> |AlphaVantage | <a href="//doc.stocksharp.com/html/cd9ecaf0-caea-462c-a6a4-c2905fe9f3eb.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/cd9ecaf0-caea-462c-a6a4-c2905fe9f3eb.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/BarChart_logo.png" height="30" /> |Bachart | <a href="//doc.stocksharp.com/html/4448a233-b3d5-46e1-a0f2-549ec5fa681a.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/4448a233-b3d5-46e1-a0f2-549ec5fa681a.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Blackwood_logo.png" height="30" /> |Blackwood (Fusion) | <a href="//doc.stocksharp.com/html/89c3f13d-2602-446a-8c3d-5615b6f901b9.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/89c3f13d-2602-446a-8c3d-5615b6f901b9.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/CQG_logo.png" height="30" /> |CQG | <a href="//doc.stocksharp.com/html/aac980b1-ac5b-415b-811c-a8d128942391.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/aac980b1-ac5b-415b-811c-a8d128942391.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/ETrade_logo.png" height="30" /> |E*TRADE | <a href="//doc.stocksharp.com/html/84d6a0fb-607f-4d87-be8a-e2b58006493e.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/84d6a0fb-607f-4d87-be8a-e2b58006493e.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Google_logo.png" height="30" /> |Google | <a href="//doc.stocksharp.com/html/eba96e4f-8f29-4fc2-8011-5d38b415281b.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/eba96e4f-8f29-4fc2-8011-5d38b415281b.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/iex_logo.png" height="30" /> |IEX | <a href="//doc.stocksharp.com/html/fb946f86-fe4b-4e30-97f7-543178e81792.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/fb946f86-fe4b-4e30-97f7-543178e81792.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/InteractiveBrokers_logo.png" height="30" /> |Interactive Brokers | <a href="//doc.stocksharp.com/html/bae7b613-dcf6-4abb-b595-6c61fc4e5c46.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/bae7b613-dcf6-4abb-b595-6c61fc4e5c46.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/IQFeed_logo.png" height="30" /> |IQFeed | <a href="//doc.stocksharp.com/html/c7ff5937-e230-4db3-857f-4cd68583ebfc.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/c7ff5937-e230-4db3-857f-4cd68583ebfc.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Itch_logo.png" height="30" /> |ITCH | <a href="//doc.stocksharp.com/html/62dc0f78-2b9a-4f88-b6bc-68361bd0d8fe.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/62dc0f78-2b9a-4f88-b6bc-68361bd0d8fe.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/OpenECry_logo.png" height="30" /> |OpenECry | <a href="//doc.stocksharp.com/html/f8cae46b-57e1-4954-a4cf-832854840981.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/f8cae46b-57e1-4954-a4cf-832854840981.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Quandl_logo.png" height="30" /> |Quandl | <a href="//doc.stocksharp.com/html/2da31578-dd6d-4682-a9de-42f7bf892681.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/2da31578-dd6d-4682-a9de-42f7bf892681.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/quanthouse_logo.png" height="30" /> |QuantFEED | <a href="//doc.stocksharp.com/html/003e9486-6ed9-4afc-a325-6b2cbc382794.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/003e9486-6ed9-4afc-a325-6b2cbc382794.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Rithmic_logo.png" height="30" /> |Rithmic | <a href="//doc.stocksharp.com/html/777d9208-1146-4d54-b5ae-0315b4186522.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/777d9208-1146-4d54-b5ae-0315b4186522.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Sterling_logo.png" height="30" /> |Sterling | <a href="//doc.stocksharp.com/html/8a531942-a16c-4348-a4d0-dd4ae999d8f9.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/8a531942-a16c-4348-a4d0-dd4ae999d8f9.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Tradier_logo.png" height="30" /> |Tradier | <a href="//doc.stocksharp.com/html/113c8f3e-3145-4899-bd5b-60ceac995be2.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/113c8f3e-3145-4899-bd5b-60ceac995be2.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Xignite_logo.png" height="30" /> |Xignite | <a href="//doc.stocksharp.com/html/f119fa3f-2ce2-4924-acb8-e97f1b9e4b5b.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/f119fa3f-2ce2-4924-acb8-e97f1b9e4b5b.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Yahoo_logo.png" height="30" /> |Yahoo | <a href="//doc.stocksharp.com/html/d73e788c-9915-402f-ba27-47217539979e.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/d73e788c-9915-402f-ba27-47217539979e.htm" target="_blank">Ru</a> |

## Russian Stock, Futures and Options
|Logo | Name |  Documentation Ru| 
|:---:|:----:|:---------------:|
|<img src="./Media/logos/Mfd_logo.png" height="30" /> |Mfd | <a href="https://doc.stocksharp.ru/html/3f8e07ed-6fc1-4145-8532-2a960735f112.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/micex_logo.png" height="30" /> |Micex (TEAP) | <a href="https://doc.stocksharp.ru/html/61692ace-225e-4ecc-845a-504021d59a8f.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/plaza_logo.png" height="30" /> |Plaza II | <a href="https://doc.stocksharp.ru/html/7eda6d74-d3b8-4fe5-b6a3-fab60e441daf.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Quik_logo.png" height="30" /> |Quik |  <a href="https://doc.stocksharp.ru/html/769f74c8-6f8e-4312-a867-3dc6e8482636.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Quik_logo.png" height="30" /> |Quik FIX |  <a href="https://doc.stocksharp.ru/html/b64a1826-58e3-4ac8-8923-099b52992e2e.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/SmartCom_logo.png" height="30" /> |SmartCOM |  <a href="https://doc.stocksharp.ru/html/7f488b0b-0f59-42b4-845b-fd766f5699dc.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/spbex_logo.png" height="30" /> |SPB Exchange |  <a href="https://doc.stocksharp.ru/html/2bbcaa58-0092-4603-a35f-d4a9bc7cb835.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Transaq_logo.png" height="30" /> |Transaq |  <a href="https://doc.stocksharp.ru/html/a010f9bd-15bb-4858-a067-590101087dff.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/micex_logo.png" height="30" /> |Twime |  <a href="https://doc.stocksharp.ru/html/1ee210ee-a004-4277-b8f4-91cb08d651db.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/UkrExh_logo.png" height="30" /> |UX (сайт) | <a href="https://doc.stocksharp.ru/html/778e03c7-d639-4b5d-b874-d5bab5a1034d.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/AlorHistory_logo.png" height="30" /> |Алор Трейд | <a href="https://doc.stocksharp.ru/html/87880b45-6311-42af-9d37-2f4ad9597658.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/AlfaDirect_logo.png" height="30" /> |Альфа-Директ | <a href="https://doc.stocksharp.ru/html/fdfe3e0b-60b8-4915-8db5-8bfab7d9e391.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/MoexLchi_logo.png" height="30" /> |ЛЧИ | <a href="https://doc.stocksharp.ru/html/e0fcdbe7-d595-4cf2-ae9d-4ba5b215273f.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/RtsHistory_logo.png" height="30" /> |РТС | <a href="https://doc.stocksharp.ru/html/98efb1f0-107b-4442-846f-1d517330ba39.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Finam_logo.png" height="30" /> |Финам | <a href="https://doc.stocksharp.ru/html/19c2bdbe-15ab-4b41-9d87-e838b5f17c8e.htm" target="_blank">Ru</a> |

## Forex
|Logo | Name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|<img src="./Media/logos/DukasCopy_logo.png" height="30" /> |DukasCopy | <a href="//doc.stocksharp.com/html/4e2b91c9-624d-4a4c-b71f-41de89ad032b.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/4e2b91c9-624d-4a4c-b71f-41de89ad032b.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/FinViz_logo.png" height="30" /> |FinViz | <a href="//doc.stocksharp.com/html/c0e87965-c0de-47c5-9e45-242c9c5b72cc.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/c0e87965-c0de-47c5-9e45-242c9c5b72cc.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/fxcm_logo.png" height="30" /> |FXCM | <a href="//doc.stocksharp.com/html/92073cd8-8e10-498e-8de9-47d3f77d278a.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/92073cd8-8e10-498e-8de9-47d3f77d278a.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/GainCapital_logo.png" height="30" /> |GAIN Capital | <a href="//doc.stocksharp.com/html/97482eee-4ffd-4a3c-bb34-bee4cd399d40.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/97482eee-4ffd-4a3c-bb34-bee4cd399d40.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Lmax_logo.png" height="30" /> |LMAX | <a href="//doc.stocksharp.com/html/4f50724b-00de-4ed4-b043-7dacb6277c98.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/4f50724b-00de-4ed4-b043-7dacb6277c98.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/MBTrading_logo.png" height="30" /> |MB Trading | <a href="//doc.stocksharp.com/html/da4d8797-6ce6-4947-af51-7c568d17e29e.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/da4d8797-6ce6-4947-af51-7c568d17e29e.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Oanda_logo.png" height="30" /> |Oanda | <a href="//doc.stocksharp.com/html/c2162c96-d12f-4107-ac96-0238b793f466.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/c2162c96-d12f-4107-ac96-0238b793f466.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/TrueFX_logo.png" height="30" /> |TrueFX | <a href="//doc.stocksharp.com/html/e37be6cb-638e-445e-b0df-f40ecec74343.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/e37be6cb-638e-445e-b0df-f40ecec74343.htm" target="_blank">Ru</a> |

## Cryptocurrencies
|Logo | Name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|<img src="./Media/logos/Bibox_logo.png" height="30" /> |Bibox | <a href="//doc.stocksharp.com/html/8f22e760-96c8-493a-aef0-569cbc42a9da.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/8f22e760-96c8-493a-aef0-569cbc42a9da.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Binance_logo.svg" height="30" /> |Binance | <a href="//doc.stocksharp.com/html/9bf6d7aa-a3b8-42ba-a889-de1b2f7847f2.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/9bf6d7aa-a3b8-42ba-a889-de1b2f7847f2.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/bitalong_logo.png" height="30" /> |Bitalong | <a href="TODO" target="_blank">Eng</a> | <a href="TODO" target="_blank">Ru</a> |
|<img src="./Media/logos/Bitbank_logo.png" height="30" /> |Bitbank | <a href="//doc.stocksharp.com/html/02e5e6b3-4436-4c1d-ae39-776c6d398cd3.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/02e5e6b3-4436-4c1d-ae39-776c6d398cd3.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Bitexbook_logo.png" height="30" /> |Bitexbook | <a href="//doc.stocksharp.com/html/4f6d317f-8788-48cb-b8d7-5e621481181c.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/4f6d317f-8788-48cb-b8d7-5e621481181c.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Bitfinex_logo.png" height="30" /> |Bitfinex | <a href="//doc.stocksharp.com/html/f49aab57-c1fd-4558-9241-5b42a2e619d7.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/f49aab57-c1fd-4558-9241-5b42a2e619d7.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Bithumb_logo.png" height="30" /> |Bithumb | <a href="//doc.stocksharp.com/html/4f431ab1-64bc-4cef-8387-c14c931f17b7.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/4f431ab1-64bc-4cef-8387-c14c931f17b7.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/BitMax_logo.png" height="30" /> |BitMax | <a href="//doc.stocksharp.com/html/740cbac0-53a1-47b9-92c2-397fd7e9c97f.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/740cbac0-53a1-47b9-92c2-397fd7e9c97f.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Bitmex_logo.png" height="30" /> |BitMEX | <a href="//doc.stocksharp.com/html/81f33924-b166-42c1-8ced-f7e9468f86b5.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/81f33924-b166-42c1-8ced-f7e9468f86b5.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/BitStamp_logo.svg" height="30" /> |BitStamp | <a href="//doc.stocksharp.com/html/345fa341-661d-4992-a9a6-9c89af399feb.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/345fa341-661d-4992-a9a6-9c89af399feb.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Bittrex_logo.png" height="30" /> |Bittrex | <a href="//doc.stocksharp.com/html/0d71e2d9-6f13-435d-8697-58d41978d46b.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/0d71e2d9-6f13-435d-8697-58d41978d46b.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/BitZ_logo.png" height="30" /> |BitZ | <a href="//doc.stocksharp.com/html/66d9f9b3-1251-414a-ab9b-18060f2f62c6.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/66d9f9b3-1251-414a-ab9b-18060f2f62c6.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Btce_logo.png" height="30" /> |BTC-E | <a href="//doc.stocksharp.com/html/5d162089-902a-4a4e-885f-f38ff94fbe58.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/5d162089-902a-4a4e-885f-f38ff94fbe58.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/BW_logo.png" height="30" /> |BW | <a href="//doc.stocksharp.com/html/0f66a2b6-8ad1-4331-99c8-7543be3fabb0.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/0f66a2b6-8ad1-4331-99c8-7543be3fabb0.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Cex_logo.png" height="30" /> |CEX.IO | <a href="//doc.stocksharp.com/html/418c4e3a-6129-4147-a3b6-a27701c15814.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/418c4e3a-6129-4147-a3b6-a27701c15814.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Coinbase_logo.png" height="30" /> |Coinbase | <a href="//doc.stocksharp.com/html/e4f12540-0650-4cf8-80a4-216b1acf37f3.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/e4f12540-0650-4cf8-80a4-216b1acf37f3.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinBene_logo.png" height="30" /> |CoinBene | <a href="//doc.stocksharp.com/html/1e7027ce-03c3-4b35-bdfd-abcceed7d249.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/1e7027ce-03c3-4b35-bdfd-abcceed7d249.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinCap_logo.png" height="30" /> |CoinCap | <a href="//doc.stocksharp.com/html/588ae84d-4ac1-4f08-a235-a194292e66ff.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/588ae84d-4ac1-4f08-a235-a194292e66ff.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Coincheck_logo.png" height="30" /> |Coincheck | <a href="//doc.stocksharp.com/html/5bd2fb14-4391-4782-8031-55a8391d302c.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/5bd2fb14-4391-4782-8031-55a8391d302c.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinEx_logo.png" height="30" /> |CoinEx | <a href="//doc.stocksharp.com/html/2de49aa5-33f4-43e0-a36a-3312b274daa2.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/2de49aa5-33f4-43e0-a36a-3312b274daa2.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinExchange_logo.png" height="30" /> |CoinExchange | <a href="//doc.stocksharp.com/html/956025d9-6a39-4bf5-a143-903c7540ff58.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/956025d9-6a39-4bf5-a143-903c7540ff58.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Coinigy_logo.png" height="30" /> |Coinigy  | <a href="//doc.stocksharp.com/html/93ab9c0b-d310-4285-b132-cdace95134dd.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/93ab9c0b-d310-4285-b132-cdace95134dd.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/CoinHub_logo.png" height="30" /> |CoinHub | <a href="//doc.stocksharp.com/html/6795eb25-c7be-456f-a8f4-893115f407c7.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/6795eb25-c7be-456f-a8f4-893115f407c7.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Cryptopia_logo.png" height="30" /> |Cryptopia | <a href="//doc.stocksharp.com/html/d4fa79a6-59fd-43a9-ac61-b9b8933618c6.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/d4fa79a6-59fd-43a9-ac61-b9b8933618c6.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Deribit_logo.png" height="30" /> |Deribit | <a href="//doc.stocksharp.com/html/0b4fde09-8808-4ef8-89df-39aebcdd64a1.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/0b4fde09-8808-4ef8-89df-39aebcdd64a1.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Digifinex_logo.png" height="30" /> |DigiFinex | <a href="//doc.stocksharp.com/html/5097b5fb-42fa-4418-9afc-de5b6ff69df6.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/5097b5fb-42fa-4418-9afc-de5b6ff69df6.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/digitexfutures_logo.png" height="30" /> |DigitexFutures | <a href="TODO" target="_blank">Eng</a> | <a href="TODO" target="_blank">Ru</a> |
|<img src="./Media/logos/Exmo_logo.png" height="30" /> |EXMO | <a href="//doc.stocksharp.com/html/601781f1-fae7-4c82-adb4-a5e7d9394cfb.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/601781f1-fae7-4c82-adb4-a5e7d9394cfb.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/FatBtc_logo.png" height="30" /> |FatBTC | <a href="//doc.stocksharp.com/html/d342e0f7-6bd2-4627-831a-ad669082e2a4.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/d342e0f7-6bd2-4627-831a-ad669082e2a4.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Gdax_logo.png" height="30" /> |GDAX | <a href="//doc.stocksharp.com/html/d941f537-602b-400f-91d4-d4b2b37b9767.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/d941f537-602b-400f-91d4-d4b2b37b9767.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Gopax_logo.png" height="30" /> |GOPAX | <a href="//doc.stocksharp.com/html/2a1fb6a1-6d22-482a-a367-9fa0cfea1ae3.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/2a1fb6a1-6d22-482a-a367-9fa0cfea1ae3.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/HitBtc_logo.png" height="30" /> |HitBTC | <a href="//doc.stocksharp.com/html/17ebcf40-aa25-4717-83cd-d275209ff524.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/17ebcf40-aa25-4717-83cd-d275209ff524.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Hotbit_logo.png" height="30" /> |Hotbit | <a href="//doc.stocksharp.com/html/9a4eb0e5-5312-4a67-9ffb-7a7760cc54fe.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/9a4eb0e5-5312-4a67-9ffb-7a7760cc54fe.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Huobi_logo.png" height="30" /> |Huobi | <a href="//doc.stocksharp.com/html/16eaa615-aaf7-42a5-9de7-01762a650758.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/16eaa615-aaf7-42a5-9de7-01762a650758.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Idax_logo.png" height="30" /> |IDAX | <a href="//doc.stocksharp.com/html/886bcc99-7011-4c3c-85fe-d0fc646ee00f.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/886bcc99-7011-4c3c-85fe-d0fc646ee00f.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Kraken_logo.png" height="30" /> |Kraken | <a href="//doc.stocksharp.com/html/cabadfac-eff9-48ab-ada6-c5856778cf68.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/cabadfac-eff9-48ab-ada6-c5856778cf68.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Kucoin_logo.png" height="30" /> |KuCoin | <a href="//doc.stocksharp.com/html/d2379d21-9bea-4199-8432-c25f73cb1594.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/d2379d21-9bea-4199-8432-c25f73cb1594.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Latoken_logo.png" height="30" /> |LATOKEN | <a href="//doc.stocksharp.com/html/e99810b8-4b14-42f5-a987-7bb905142d18.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/e99810b8-4b14-42f5-a987-7bb905142d18.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/LBank_logo.png" height="30" /> |LBank | <a href="//doc.stocksharp.com/html/3a5c4a94-ab01-49a5-ab41-82c85479d147.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/3a5c4a94-ab01-49a5-ab41-82c85479d147.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Liqui_logo.png" height="30" /> |Liqui | <a href="//doc.stocksharp.com/html/70ecf683-4336-4848-a05d-8edbcc730af4.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/70ecf683-4336-4848-a05d-8edbcc730af4.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/LiveCoin_logo.png" height="30" /> |Livecoin | <a href="//doc.stocksharp.com/html/8e093fe9-7b2d-47f3-a95a-ece0e0033ecc.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/8e093fe9-7b2d-47f3-a95a-ece0e0033ecc.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Okcoin_logo.png" height="30" /> |OKCoin | <a href="//doc.stocksharp.com/html/f9ec5eb4-4237-4a2a-9663-757d3d4c0689.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/f9ec5eb4-4237-4a2a-9663-757d3d4c0689.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Okex_logo.png" height="30" /> |OKEx | <a href="//doc.stocksharp.com/html/554b35f0-4110-4da0-ba30-4c450e8a996b.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/554b35f0-4110-4da0-ba30-4c450e8a996b.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/poloniex_logo.png" height="30" /> |Poloniex | <a href="//doc.stocksharp.com/html/18667328-deec-46dd-8c27-fce52733e5ce.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/18667328-deec-46dd-8c27-fce52733e5ce.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/prizmbit_logo.png" height="30" /> |PrizmBit | <a href="TODO" target="_blank">Eng</a> | <a href="TODO" target="_blank">Ru</a> |
|<img src="./Media/logos/Quoinex_logo.png" height="30" /> |QuoineX | <a href="//doc.stocksharp.com/html/713a3769-b80c-4342-89d2-031a492372df.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/713a3769-b80c-4342-89d2-031a492372df.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/TradeOgre_logo.png" height="30" /> |TradeOgre | <a href="//doc.stocksharp.com/html/989447cf-bc05-4f6a-a736-b3a916ee1ffc.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/989447cf-bc05-4f6a-a736-b3a916ee1ffc.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Upbit_logo.png" height="30" /> |Upbit | <a href="//doc.stocksharp.com/html/c8df9244-9329-4e0d-bb80-58d0cbd0760b.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/c8df9244-9329-4e0d-bb80-58d0cbd0760b.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Yobit_logo.png" height="30" /> |YoBit | <a href="//doc.stocksharp.com/html/ea1dbc04-3c29-4fbe-91b0-260d1ded31ae.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/ea1dbc04-3c29-4fbe-91b0-260d1ded31ae.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/Zaif_logo.png" height="30" /> |Zaif | <a href="//doc.stocksharp.com/html/6d22d370-721c-452b-85af-6fc9c31115ac.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/6d22d370-721c-452b-85af-6fc9c31115ac.htm" target="_blank">Ru</a> |
|<img src="./Media/logos/ZB_logo.png" height="30" /> |ZB | <a href="//doc.stocksharp.com/html/3502cfdf-e496-4567-ad4e-c7b195569b60.htm" target="_blank">Eng</a> | <a href="https://doc.stocksharp.ru/html/3502cfdf-e496-4567-ad4e-c7b195569b60.htm" target="_blank">Ru</a> |

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

