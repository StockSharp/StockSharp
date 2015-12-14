#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: RequestMarketDataCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;

	public class RequestMarketDataCommand : BaseStudioCommand
	{
		public Security Security { get; private set; }
		public MarketDataTypes Type { get; private set; }

		public RequestMarketDataCommand(Security security, MarketDataTypes type)
		{
			Security = security;
			Type = type;
		}
	}

	public class RefuseMarketDataCommand : BaseStudioCommand
	{
		public Security Security { get; private set; }
		public MarketDataTypes Type { get; private set; }

		public RefuseMarketDataCommand(Security security, MarketDataTypes type)
		{
			Security = security;
			Type = type;
		}
	}

	public class SubscribeCandleElementCommand : BaseStudioCommand
	{
		public ChartCandleElement Element { get; private set; }

		public CandleSeries CandleSeries { get; private set; }

		public SubscribeCandleElementCommand(ChartCandleElement element, CandleSeries candleSeries)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			if (candleSeries == null)
				throw new ArgumentNullException(nameof(candleSeries));

			Element = element;
			CandleSeries = candleSeries;
		}
	}

	public class UnSubscribeCandleElementCommand : BaseStudioCommand
	{
		public ChartCandleElement Element { get; private set; }

		public UnSubscribeCandleElementCommand(ChartCandleElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			Element = element;
		}
	}

	public class SubscribeIndicatorElementCommand : BaseStudioCommand
	{
		public ChartIndicatorElement Element { get; private set; }

		public CandleSeries CandleSeries { get; private set; }

		public IIndicator Indicator { get; private set; }

		public SubscribeIndicatorElementCommand(ChartIndicatorElement element, CandleSeries candleSeries, IIndicator indicator)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			if (candleSeries == null)
				throw new ArgumentNullException(nameof(candleSeries));

			if (indicator == null)
				throw new ArgumentNullException(nameof(indicator));

			Element = element;
			CandleSeries = candleSeries;
			Indicator = indicator;
		}
	}

	public class UnSubscribeIndicatorElementCommand : BaseStudioCommand
	{
		public ChartIndicatorElement Element { get; private set; }

		public UnSubscribeIndicatorElementCommand(ChartIndicatorElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			Element = element;
		}
	}

	public class SubscribeOrderElementCommand : BaseStudioCommand
	{
		public ChartOrderElement Element { get; private set; }

		public Security Security { get; private set; }

		public SubscribeOrderElementCommand(ChartOrderElement element, Security security)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			Element = element;
			Security = security;
		}
	}

	public class UnSubscribeOrderElementCommand : BaseStudioCommand
	{
		public ChartOrderElement Element { get; private set; }

		public UnSubscribeOrderElementCommand(ChartOrderElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			Element = element;
		}
	}

	public class SubscribeTradeElementCommand : BaseStudioCommand
	{
		public ChartTradeElement Element { get; private set; }

		public Security Security { get; private set; }

		public SubscribeTradeElementCommand(ChartTradeElement element, Security security)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			Element = element;
			Security = security;
		}
	}

	public class UnSubscribeTradeElementCommand : BaseStudioCommand
	{
		public ChartTradeElement Element { get; private set; }

		public UnSubscribeTradeElementCommand(ChartTradeElement element)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			Element = element;
		}
	}
}
