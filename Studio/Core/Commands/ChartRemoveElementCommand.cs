#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: ChartRemoveElementCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;

	using StockSharp.Xaml.Charting;

	public class ChartRemoveElementCommand : BaseStudioCommand
	{
		public ChartArea Area { get; private set; }

		public IChartElement Element { get; set; }

		public ChartRemoveElementCommand(ChartArea area, IChartElement element)
		{
			if (area == null)
				throw new ArgumentNullException(nameof(area));

			if (element == null)
				throw new ArgumentNullException(nameof(element));

			Area = area;
			Element = element;
		}
	}
}