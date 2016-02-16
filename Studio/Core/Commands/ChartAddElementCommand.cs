#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: ChartAddElementCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

namespace StockSharp.Studio.Core.Commands
{
	using System;
	using StockSharp.Xaml.Charting;
	using StockSharp.Algo.Candles;

	public class ChartAddElementCommand : BaseStudioCommand
	{
		public ChartArea Area { get; }

		public IChartElement Element { get; }

		public CandleSeries Series {get;}

		public ChartAddElementCommand(ChartArea area, IChartElement element, CandleSeries series = null)
		{
			if (area == null)
				throw new ArgumentNullException(nameof(area));

			if (element == null)
				throw new ArgumentNullException(nameof(element));

			Area = area;
			Element = element;
			Series = series;
		}
	}
}