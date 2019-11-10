<img src="https://github.com/Zalutskiy/StockSharp/blob/master/Media/SLogo.png" align="right" />

# [StockSharp - trading platform][1] 
## [Documentation][2] | [Download][3] | [Support][7] | [Algotrading training][4]

## Introduction ##

**StockSharp** (shortly **S#**) – are **free** programs for trading at any markets of the world (American, European, Asian, Russian, stocks, futures, options, Bitcoins, forex, etc.). You will be able to trade manually or automated trading (algorithmic trading robots, conventional or HFT).

**Available connections**: FIX/FAST, ITCH (LSE, NASDAQ), Blackwood/Fusion, BarChart, CQG, E*Trade, IQFeed, InteractiveBrokers, LMAX, MatLab, Oanda, FXCM, OpenECry, Rithmic, RSS, Sterling, BTCE, BitStamp, Bitfinex, Coinbase, Kraken, Poloniex, GDAX, Bittrex, Bithumb, HitBTC, OKCoin, Coincheck, Binance, Liqui, CEX.IO, Cryptopia, OKEx, BitMEX, YoBit, Livecoin, EXMO, Deribit, Huobi, KuCoin, BITEXBOOK, CoinExchange, QuantFEED and many other.

## [S#.Designer][8]
<img src="https://github.com/Zalutskiy/StockSharp/blob/master/Media/Designer500.gif" align="left" />

**S#.Designer** - **free** universal algorithmic strategies application for easy strategy creation::
  - Visual designer to create strategies by mouse clicking
  - Embedded C# editor
  - Easy to create own indicators
  - Build in debugger
  - Connections to the multiple electronic boards and brokers
  - All world platforms
  - Schema sharing with own team

## [S#.Data][9]
<img src="https://github.com/Zalutskiy/StockSharp/blob/master/Media/Hydra500.gif" align="right" />

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
<img src="https://github.com/Zalutskiy/StockSharp/blob/master/Media/Terminal500.gif" align="left" />

**Terminal** - **free** trading charting application (trading terminal):
  - Connections to the multiple electronic boards and brokers
  - Trading from charts by clicking
  - Arbitrary timeframes
  - Volume, Tick, Range, P&F, Renko candles
  - Cluster charts
  - Box charts
  - Volume Profile
  
## [S#.Shell][11]
<img src="https://github.com/Zalutskiy/StockSharp/blob/master/Media/Shell500.gif" align="right" />

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
|logo | name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/AlphaVantage_logo.png) |AlphaVantage | [Eng](https://doc.stocksharp.com/html/cd9ecaf0-caea-462c-a6a4-c2905fe9f3eb.htm) | [Ru](https://doc.stocksharp.ru/html/cd9ecaf0-caea-462c-a6a4-c2905fe9f3eb.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Bachart_logo.png) |Bachart | [Eng](https://doc.stocksharp.com/html/4448a233-b3d5-46e1-a0f2-549ec5fa681a.htm) | [Ru](https://doc.stocksharp.ru/html/4448a233-b3d5-46e1-a0f2-549ec5fa681a.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Blackwood_logo.png) |Blackwood (Fusion) | [Eng](https://doc.stocksharp.com/html/89c3f13d-2602-446a-8c3d-5615b6f901b9.htm) | [Ru](https://doc.stocksharp.ru/html/89c3f13d-2602-446a-8c3d-5615b6f901b9.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/CQG_logo.png) |CQG | [Eng](https://doc.stocksharp.com/html/aac980b1-ac5b-415b-811c-a8d128942391.htm) | [Ru](https://doc.stocksharp.ru/html/aac980b1-ac5b-415b-811c-a8d128942391.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/E*TRADE_logo.png) |E*TRADE | [Eng](https://doc.stocksharp.com/html/84d6a0fb-607f-4d87-be8a-e2b58006493e.htm) | [Ru](https://doc.stocksharp.ru/html/84d6a0fb-607f-4d87-be8a-e2b58006493e.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Google_logo.png) |Google | [Eng](https://doc.stocksharp.com/html/eba96e4f-8f29-4fc2-8011-5d38b415281b.htm) | [Ru](https://doc.stocksharp.ru/html/eba96e4f-8f29-4fc2-8011-5d38b415281b.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/IEX_logo.png) |IEX | [Eng](https://doc.stocksharp.com/html/fb946f86-fe4b-4e30-97f7-543178e81792.htm) | [Ru](https://doc.stocksharp.ru/html/fb946f86-fe4b-4e30-97f7-543178e81792.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/InteractiveBrokers_logo.png) |Interactive Brokers | [Eng](https://doc.stocksharp.com/html/bae7b613-dcf6-4abb-b595-6c61fc4e5c46.htm) | [Ru](https://doc.stocksharp.ru/html/bae7b613-dcf6-4abb-b595-6c61fc4e5c46.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/IQFeed_logo.png) |IQFeed | [Eng](https://doc.stocksharp.com/html/c7ff5937-e230-4db3-857f-4cd68583ebfc.htm) | [Ru](https://doc.stocksharp.ru/html/c7ff5937-e230-4db3-857f-4cd68583ebfc.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/ITCH_logo.png) |ITCH | [Eng](https://doc.stocksharp.com/html/62dc0f78-2b9a-4f88-b6bc-68361bd0d8fe.htm) | [Ru](https://doc.stocksharp.ru/html/62dc0f78-2b9a-4f88-b6bc-68361bd0d8fe.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/OpenECry_logo.png) |OpenECry | [Eng](https://doc.stocksharp.com/html/f8cae46b-57e1-4954-a4cf-832854840981.htm) | [Ru](https://doc.stocksharp.ru/html/f8cae46b-57e1-4954-a4cf-832854840981.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Quandl_logo.png) |Quandl | [Eng](https://doc.stocksharp.com/html/2da31578-dd6d-4682-a9de-42f7bf892681.htm) | [Ru](https://doc.stocksharp.ru/html/2da31578-dd6d-4682-a9de-42f7bf892681.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/QuantFEED_logo.png) |QuantFEED | [Eng](https://doc.stocksharp.com/html/003e9486-6ed9-4afc-a325-6b2cbc382794.htm) | [Ru](https://doc.stocksharp.ru/html/003e9486-6ed9-4afc-a325-6b2cbc382794.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Rithmic_logo.png) |Rithmic | [Eng](https://doc.stocksharp.com/html/777d9208-1146-4d54-b5ae-0315b4186522.htm) | [Ru](https://doc.stocksharp.ru/html/777d9208-1146-4d54-b5ae-0315b4186522.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Sterling_logo.png) |Sterling | [Eng](https://doc.stocksharp.com/html/8a531942-a16c-4348-a4d0-dd4ae999d8f9.htm) | [Ru](https://doc.stocksharp.ru/html/8a531942-a16c-4348-a4d0-dd4ae999d8f9.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Tradier_logo.png) |Tradier | [Eng](https://doc.stocksharp.com/html/113c8f3e-3145-4899-bd5b-60ceac995be2.htm) | [Ru](https://doc.stocksharp.ru/html/113c8f3e-3145-4899-bd5b-60ceac995be2.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Xignite_logo.png) |Xignite | [Eng](https://doc.stocksharp.com/html/f119fa3f-2ce2-4924-acb8-e97f1b9e4b5b.htm) | [Ru](https://doc.stocksharp.ru/html/f119fa3f-2ce2-4924-acb8-e97f1b9e4b5b.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Yahoo_logo.png) |Yahoo | [Eng](https://doc.stocksharp.com/html/d73e788c-9915-402f-ba27-47217539979e.htm) | [Ru](https://doc.stocksharp.ru/html/d73e788c-9915-402f-ba27-47217539979e.htm)|

## Russian Stock, Futures and Options
|logo | name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Mfd_logo.png) |Mfd | [Eng](https://doc.stocksharp.com/html/3f8e07ed-6fc1-4145-8532-2a960735f112.htm) | [Ru](https://doc.stocksharp.ru/html/3f8e07ed-6fc1-4145-8532-2a960735f112.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/micex_logo.png) |Micex (TEAP) | [Eng](https://doc.stocksharp.com/html/61692ace-225e-4ecc-845a-504021d59a8f.htm) | [Ru](https://doc.stocksharp.ru/html/61692ace-225e-4ecc-845a-504021d59a8f.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/plaza_logo.png) |Plaza II | [Eng](https://doc.stocksharp.com/html/7eda6d74-d3b8-4fe5-b6a3-fab60e441daf.htm) | [Ru](https://doc.stocksharp.ru/html/7eda6d74-d3b8-4fe5-b6a3-fab60e441daf.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Quik_logo.png) |Quik | [Eng](https://doc.stocksharp.com/html/769f74c8-6f8e-4312-a867-3dc6e8482636.htm) | [Ru](https://doc.stocksharp.ru/html/769f74c8-6f8e-4312-a867-3dc6e8482636.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Quik_logo.png) |Quik FIX | [Eng](https://doc.stocksharp.com/html/b64a1826-58e3-4ac8-8923-099b52992e2e.htm) | [Ru](https://doc.stocksharp.ru/html/b64a1826-58e3-4ac8-8923-099b52992e2e.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/SmartCOM_logo.png) |SmartCOM | [Eng](https://doc.stocksharp.com/html/7f488b0b-0f59-42b4-845b-fd766f5699dc.htm) | [Ru](https://doc.stocksharp.ru/html/7f488b0b-0f59-42b4-845b-fd766f5699dc.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/spb_logo.png) |SPB Exchange | [Eng](https://doc.stocksharp.com/html/2bbcaa58-0092-4603-a35f-d4a9bc7cb835.htm) | [Ru](https://doc.stocksharp.ru/html/2bbcaa58-0092-4603-a35f-d4a9bc7cb835.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Transaq_logo.png) |Transaq | [Eng](https://doc.stocksharp.com/html/a010f9bd-15bb-4858-a067-590101087dff.htm) | [Ru](https://doc.stocksharp.ru/html/a010f9bd-15bb-4858-a067-590101087dff.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Twime_logo.png) |Twime | [Eng](https://doc.stocksharp.com/html/1ee210ee-a004-4277-b8f4-91cb08d651db.htm) | [Ru](https://doc.stocksharp.ru/html/1ee210ee-a004-4277-b8f4-91cb08d651db.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/UkrExh_logo.png) |UX (сайт) | [Eng](https://doc.stocksharp.com/html/778e03c7-d639-4b5d-b874-d5bab5a1034d.htm) | [Ru](https://doc.stocksharp.ru/html/778e03c7-d639-4b5d-b874-d5bab5a1034d.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/AlorHistory_logo.png) |Алор Трейд | [Eng](https://doc.stocksharp.com/html/87880b45-6311-42af-9d37-2f4ad9597658.htm) | [Ru](https://doc.stocksharp.ru/html/87880b45-6311-42af-9d37-2f4ad9597658.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/AlfaDirect_logo.png) |Альфа-Директ | [Eng](https://doc.stocksharp.com/html/fdfe3e0b-60b8-4915-8db5-8bfab7d9e391.htm) | [Ru](https://doc.stocksharp.ru/html/fdfe3e0b-60b8-4915-8db5-8bfab7d9e391.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/MoexLchi_logo.png) |ЛЧИ | [Eng](https://doc.stocksharp.com/html/e0fcdbe7-d595-4cf2-ae9d-4ba5b215273f.htm) | [Ru](https://doc.stocksharp.ru/html/e0fcdbe7-d595-4cf2-ae9d-4ba5b215273f.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/RtsHistory_logo.png) |РТС | [Eng](https://doc.stocksharp.com/html/98efb1f0-107b-4442-846f-1d517330ba39.htm) | [Ru](https://doc.stocksharp.ru/html/98efb1f0-107b-4442-846f-1d517330ba39.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Finam_logo.png) |Финам | [Eng](https://doc.stocksharp.com/html/19c2bdbe-15ab-4b41-9d87-e838b5f17c8e.htm) | [Ru](https://doc.stocksharp.ru/html/19c2bdbe-15ab-4b41-9d87-e838b5f17c8e.htm)|

## Forex
|logo | name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/DukasCopy_logo.png) |DukasCopy | [Eng](https://doc.stocksharp.com/html/4e2b91c9-624d-4a4c-b71f-41de89ad032b.htm) | [Ru](https://doc.stocksharp.ru/html/4e2b91c9-624d-4a4c-b71f-41de89ad032b.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/FinViz_logo.png) |FinViz | [Eng](https://doc.stocksharp.com/html/c0e87965-c0de-47c5-9e45-242c9c5b72cc.htm) | [Ru](https://doc.stocksharp.ru/html/c0e87965-c0de-47c5-9e45-242c9c5b72cc.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/FXCM_logo.png) |FXCM | [Eng](https://doc.stocksharp.com/html/92073cd8-8e10-498e-8de9-47d3f77d278a.htm) | [Ru](https://doc.stocksharp.ru/html/92073cd8-8e10-498e-8de9-47d3f77d278a.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/GainCapital_logo.png) |GAIN Capital | [Eng](https://doc.stocksharp.com/html/97482eee-4ffd-4a3c-bb34-bee4cd399d40.htm) | [Ru](https://doc.stocksharp.ru/html/97482eee-4ffd-4a3c-bb34-bee4cd399d40.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/LMAX_logo.png) |LMAX | [Eng](https://doc.stocksharp.com/html/4f50724b-00de-4ed4-b043-7dacb6277c98.htm) | [Ru](https://doc.stocksharp.ru/html/4f50724b-00de-4ed4-b043-7dacb6277c98.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/MBTrading_logo.png) |MB Trading | [Eng](https://doc.stocksharp.com/html/da4d8797-6ce6-4947-af51-7c568d17e29e.htm) | [Ru](https://doc.stocksharp.ru/html/da4d8797-6ce6-4947-af51-7c568d17e29e.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Oanda_logo.png) |Oanda | [Eng](https://doc.stocksharp.com/html/c2162c96-d12f-4107-ac96-0238b793f466.htm) | [Ru](https://doc.stocksharp.ru/html/c2162c96-d12f-4107-ac96-0238b793f466.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/TrueFX_logo.png) |TrueFX | [Eng](https://doc.stocksharp.com/html/e37be6cb-638e-445e-b0df-f40ecec74343.htm) | [Ru](https://doc.stocksharp.ru/html/e37be6cb-638e-445e-b0df-f40ecec74343.htm)|

## Cryptocurrencies
|logo | name | Documentation Eng| Documentation Ru| 
|:---:|:----:|:----------------:|:---------------:|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Bibox_logo.png) |Bibox | [Eng](https://doc.stocksharp.com/html/8f22e760-96c8-493a-aef0-569cbc42a9da.htm) | [Ru](https://doc.stocksharp.ru/html/8f22e760-96c8-493a-aef0-569cbc42a9da.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Binance_logo.png) |Binance | [Eng](https://doc.stocksharp.com/html/9bf6d7aa-a3b8-42ba-a889-de1b2f7847f2.htm) | [Ru](https://doc.stocksharp.ru/html/9bf6d7aa-a3b8-42ba-a889-de1b2f7847f2.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Bitbank_logo.png) |Bitbank | [Eng](https://doc.stocksharp.com/html/02e5e6b3-4436-4c1d-ae39-776c6d398cd3.htm) | [Ru](https://doc.stocksharp.ru/html/02e5e6b3-4436-4c1d-ae39-776c6d398cd3.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Bitexbook_logo.png) |Bitexbook | [Eng](https://doc.stocksharp.com/html/4f6d317f-8788-48cb-b8d7-5e621481181c.htm) | [Ru](https://doc.stocksharp.ru/html/4f6d317f-8788-48cb-b8d7-5e621481181c.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Bitfinex_logo.png) |Bitfinex | [Eng](https://doc.stocksharp.com/html/f49aab57-c1fd-4558-9241-5b42a2e619d7.htm) | [Ru](https://doc.stocksharp.ru/html/f49aab57-c1fd-4558-9241-5b42a2e619d7.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Bithumb_logo.png) |Bithumb | [Eng](https://doc.stocksharp.com/html/4f431ab1-64bc-4cef-8387-c14c931f17b7.htm) | [Ru](https://doc.stocksharp.ru/html/4f431ab1-64bc-4cef-8387-c14c931f17b7.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/BitMax_logo.png) |BitMax | [Eng](https://doc.stocksharp.com/html/740cbac0-53a1-47b9-92c2-397fd7e9c97f.htm) | [Ru](https://doc.stocksharp.ru/html/740cbac0-53a1-47b9-92c2-397fd7e9c97f.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/BitMEX_logo.png) |BitMEX | [Eng](https://doc.stocksharp.com/html/81f33924-b166-42c1-8ced-f7e9468f86b5.htm) | [Ru](https://doc.stocksharp.ru/html/81f33924-b166-42c1-8ced-f7e9468f86b5.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/BitStamp_logo.png) |BitStamp | [Eng](https://doc.stocksharp.com/html/345fa341-661d-4992-a9a6-9c89af399feb.htm) | [Ru](https://doc.stocksharp.ru/html/345fa341-661d-4992-a9a6-9c89af399feb.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Bittrex_logo.png) |Bittrex | [Eng](https://doc.stocksharp.com/html/0d71e2d9-6f13-435d-8697-58d41978d46b.htm) | [Ru](https://doc.stocksharp.ru/html/0d71e2d9-6f13-435d-8697-58d41978d46b.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/BitZ_logo.png) |BitZ | [Eng](https://doc.stocksharp.com/html/66d9f9b3-1251-414a-ab9b-18060f2f62c6.htm) | [Ru](https://doc.stocksharp.ru/html/66d9f9b3-1251-414a-ab9b-18060f2f62c6.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/BTC-E_logo.png) |BTC-E | [Eng](https://doc.stocksharp.com/html/5d162089-902a-4a4e-885f-f38ff94fbe58.htm) | [Ru](https://doc.stocksharp.ru/html/5d162089-902a-4a4e-885f-f38ff94fbe58.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/BW_logo.png) |BW | [Eng](https://doc.stocksharp.com/html/0f66a2b6-8ad1-4331-99c8-7543be3fabb0.htm) | [Ru](https://doc.stocksharp.ru/html/0f66a2b6-8ad1-4331-99c8-7543be3fabb0.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/CEX.IO_logo.png) |CEX.IO | [Eng](https://doc.stocksharp.com/html/418c4e3a-6129-4147-a3b6-a27701c15814.htm) | [Ru](https://doc.stocksharp.ru/html/418c4e3a-6129-4147-a3b6-a27701c15814.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Coinbase_logo.png) |Coinbase | [Eng](https://doc.stocksharp.com/html/e4f12540-0650-4cf8-80a4-216b1acf37f3.htm) | [Ru](https://doc.stocksharp.ru/html/e4f12540-0650-4cf8-80a4-216b1acf37f3.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/CoinBene_logo.png) |CoinBene | [Eng](https://doc.stocksharp.com/html/1e7027ce-03c3-4b35-bdfd-abcceed7d249.htm) | [Ru](https://doc.stocksharp.ru/html/1e7027ce-03c3-4b35-bdfd-abcceed7d249.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/CoinCap_logo.png) |CoinCap | [Eng](https://doc.stocksharp.com/html/588ae84d-4ac1-4f08-a235-a194292e66ff.htm) | [Ru](https://doc.stocksharp.ru/html/588ae84d-4ac1-4f08-a235-a194292e66ff.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Coincheck_logo.png) |Coincheck | [Eng](https://doc.stocksharp.com/html/5bd2fb14-4391-4782-8031-55a8391d302c.htm) | [Ru](https://doc.stocksharp.ru/html/5bd2fb14-4391-4782-8031-55a8391d302c.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/CoinEx_logo.png) |CoinEx | [Eng](https://doc.stocksharp.com/html/2de49aa5-33f4-43e0-a36a-3312b274daa2.htm) | [Ru](https://doc.stocksharp.ru/html/2de49aa5-33f4-43e0-a36a-3312b274daa2.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/CoinExchange_logo.png) |CoinExchange | [Eng](https://doc.stocksharp.com/html/956025d9-6a39-4bf5-a143-903c7540ff58.htm) | [Ru](https://doc.stocksharp.ru/html/956025d9-6a39-4bf5-a143-903c7540ff58.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Coinigy_logo.png) |Coinigy  | [Eng](https://doc.stocksharp.com/html/93ab9c0b-d310-4285-b132-cdace95134dd.htm) | [Ru](https://doc.stocksharp.ru/html/93ab9c0b-d310-4285-b132-cdace95134dd.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/CoinHub_logo.png) |CoinHub | [Eng](https://doc.stocksharp.com/html/6795eb25-c7be-456f-a8f4-893115f407c7.htm) | [Ru](https://doc.stocksharp.ru/html/6795eb25-c7be-456f-a8f4-893115f407c7.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Cryptopia_logo.png) |Cryptopia | [Eng](https://doc.stocksharp.com/html/d4fa79a6-59fd-43a9-ac61-b9b8933618c6.htm) | [Ru](https://doc.stocksharp.ru/html/d4fa79a6-59fd-43a9-ac61-b9b8933618c6.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Deribit_logo.png) |Deribit | [Eng](https://doc.stocksharp.com/html/0b4fde09-8808-4ef8-89df-39aebcdd64a1.htm) | [Ru](https://doc.stocksharp.ru/html/0b4fde09-8808-4ef8-89df-39aebcdd64a1.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/DigiFinex_logo.png) |DigiFinex | [Eng](https://doc.stocksharp.com/html/5097b5fb-42fa-4418-9afc-de5b6ff69df6.htm) | [Ru](https://doc.stocksharp.ru/html/5097b5fb-42fa-4418-9afc-de5b6ff69df6.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/EXMO_logo.png) |EXMO | [Eng](https://doc.stocksharp.com/html/601781f1-fae7-4c82-adb4-a5e7d9394cfb.htm) | [Ru](https://doc.stocksharp.ru/html/601781f1-fae7-4c82-adb4-a5e7d9394cfb.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/FatBTC_logo.png) |FatBTC | [Eng](https://doc.stocksharp.com/html/d342e0f7-6bd2-4627-831a-ad669082e2a4.htm) | [Ru](https://doc.stocksharp.ru/html/d342e0f7-6bd2-4627-831a-ad669082e2a4.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/GDAX_logo.png) |GDAX | [Eng](https://doc.stocksharp.com/html/d941f537-602b-400f-91d4-d4b2b37b9767.htm) | [Ru](https://doc.stocksharp.ru/html/d941f537-602b-400f-91d4-d4b2b37b9767.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/GOPAX_logo.png) |GOPAX | [Eng](https://doc.stocksharp.com/html/2a1fb6a1-6d22-482a-a367-9fa0cfea1ae3.htm) | [Ru](https://doc.stocksharp.ru/html/2a1fb6a1-6d22-482a-a367-9fa0cfea1ae3.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/HitBTC_logo.png) |HitBTC | [Eng](https://doc.stocksharp.com/html/17ebcf40-aa25-4717-83cd-d275209ff524.htm) | [Ru](https://doc.stocksharp.ru/html/17ebcf40-aa25-4717-83cd-d275209ff524.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Hotbit_logo.png) |Hotbit | [Eng](https://doc.stocksharp.com/html/9a4eb0e5-5312-4a67-9ffb-7a7760cc54fe.htm) | [Ru](https://doc.stocksharp.ru/html/9a4eb0e5-5312-4a67-9ffb-7a7760cc54fe.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Huobi_logo.png) |Huobi | [Eng](https://doc.stocksharp.com/html/16eaa615-aaf7-42a5-9de7-01762a650758.htm) | [Ru](https://doc.stocksharp.ru/html/16eaa615-aaf7-42a5-9de7-01762a650758.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/IDAX_logo.png) |IDAX | [Eng](https://doc.stocksharp.com/html/886bcc99-7011-4c3c-85fe-d0fc646ee00f.htm) | [Ru](https://doc.stocksharp.ru/html/886bcc99-7011-4c3c-85fe-d0fc646ee00f.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Kraken_logo.png) |Kraken | [Eng](https://doc.stocksharp.com/html/cabadfac-eff9-48ab-ada6-c5856778cf68.htm) | [Ru](https://doc.stocksharp.ru/html/cabadfac-eff9-48ab-ada6-c5856778cf68.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/KuCoin_logo.png) |KuCoin | [Eng](https://doc.stocksharp.com/html/d2379d21-9bea-4199-8432-c25f73cb1594.htm) | [Ru](https://doc.stocksharp.ru/html/d2379d21-9bea-4199-8432-c25f73cb1594.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/LATOKEN_logo.png) |LATOKEN | [Eng](https://doc.stocksharp.com/html/e99810b8-4b14-42f5-a987-7bb905142d18.htm) | [Ru](https://doc.stocksharp.ru/html/e99810b8-4b14-42f5-a987-7bb905142d18.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/LBank_logo.png) |LBank | [Eng](https://doc.stocksharp.com/html/3a5c4a94-ab01-49a5-ab41-82c85479d147.htm) | [Ru](https://doc.stocksharp.ru/html/3a5c4a94-ab01-49a5-ab41-82c85479d147.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Liqui_logo.png) |Liqui | [Eng](https://doc.stocksharp.com/html/70ecf683-4336-4848-a05d-8edbcc730af4.htm) | [Ru](https://doc.stocksharp.ru/html/70ecf683-4336-4848-a05d-8edbcc730af4.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Livecoin_logo.png) |Livecoin | [Eng](https://doc.stocksharp.com/html/8e093fe9-7b2d-47f3-a95a-ece0e0033ecc.htm) | [Ru](https://doc.stocksharp.ru/html/8e093fe9-7b2d-47f3-a95a-ece0e0033ecc.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/OKCoin_logo.png) |OKCoin | [Eng](https://doc.stocksharp.com/html/f9ec5eb4-4237-4a2a-9663-757d3d4c0689.htm) | [Ru](https://doc.stocksharp.ru/html/f9ec5eb4-4237-4a2a-9663-757d3d4c0689.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/OKEx_logo.png) |OKEx | [Eng](https://doc.stocksharp.com/html/554b35f0-4110-4da0-ba30-4c450e8a996b.htm) | [Ru](https://doc.stocksharp.ru/html/554b35f0-4110-4da0-ba30-4c450e8a996b.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Poloniex_logo.png) |Poloniex | [Eng](https://doc.stocksharp.com/html/18667328-deec-46dd-8c27-fce52733e5ce.htm) | [Ru](https://doc.stocksharp.ru/html/18667328-deec-46dd-8c27-fce52733e5ce.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/QuoineX_logo.png) |QuoineX | [Eng](https://doc.stocksharp.com/html/713a3769-b80c-4342-89d2-031a492372df.htm) | [Ru](https://doc.stocksharp.ru/html/713a3769-b80c-4342-89d2-031a492372df.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/TradeOgre_logo.png) |TradeOgre | [Eng](https://doc.stocksharp.com/html/989447cf-bc05-4f6a-a736-b3a916ee1ffc.htm) | [Ru](https://doc.stocksharp.ru/html/989447cf-bc05-4f6a-a736-b3a916ee1ffc.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Upbit_logo.png) |Upbit | [Eng](https://doc.stocksharp.com/html/c8df9244-9329-4e0d-bb80-58d0cbd0760b.htm) | [Ru](https://doc.stocksharp.ru/html/c8df9244-9329-4e0d-bb80-58d0cbd0760b.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/YoBit_logo.png) |YoBit | [Eng](https://doc.stocksharp.com/html/ea1dbc04-3c29-4fbe-91b0-260d1ded31ae.htm) | [Ru](https://doc.stocksharp.ru/html/ea1dbc04-3c29-4fbe-91b0-260d1ded31ae.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/Zaif_logo.png) |Zaif | [Eng](https://doc.stocksharp.com/html/6d22d370-721c-452b-85af-6fc9c31115ac.htm) | [Ru](https://doc.stocksharp.ru/html/6d22d370-721c-452b-85af-6fc9c31115ac.htm)|
|![](https://github.com/Zalutskiy/StockSharp/blob/master/Media/logos/ZB_logo.png) |ZB | [Eng](https://doc.stocksharp.com/html/3502cfdf-e496-4567-ad4e-c7b195569b60.htm) | [Ru](https://doc.stocksharp.ru/html/3502cfdf-e496-4567-ad4e-c7b195569b60.htm)|

## Development stage

Current stage of all components - [RELEASE_STAGES.md](../master/_ReleaseNotes/RELEASE_STAGES.md).
Release notes - [RELEASE_NOTES.md](../master/_ReleaseNotes/CHANGE_LOG_API.md).

## License

StockSharp code is licensed under the [Apache License 2.0](../master/LICENSE).

  [1]: https://stocksharp.com
  [2]: https://doc.stocksharp.com
  [3]: https://github.com/Zalutskiy/StockSharp/releases
  [4]: https://stocksharp.com/edu/
  [5]: https://stocksharp.com/forum/
  [6]: https://stocksharp.com/broker/
  [7]: https://stocksharp.com/support/
  [8]: https://stocksharp.com/products/designer/
  [9]: https://stocksharp.com/products/hydra/
  [10]: https://stocksharp.com/products/terminal/
  [11]: https://stocksharp.com/products/shell/
  [12]: https://stocksharp.com/products/api/

