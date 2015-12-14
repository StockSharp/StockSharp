#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Core.Commands.CorePublic
File: ChartDrawCommand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Core.Commands
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Xaml.Charting;

	public class ChartDrawCommand : BaseStudioCommand
	{
		public ChartDrawCommand(IEnumerable<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>> values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			Values = values;
		}

		public ChartDrawCommand(DateTimeOffset time, IDictionary<IChartElement, object> values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			Values = new List<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>>
			{
				new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(time, values)
			};
		}

		public ChartDrawCommand(DateTimeOffset time, IChartElement element, object value)
		{
			if (element == null)
				throw new ArgumentNullException(nameof(element));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			Values = new List<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>>
			{
				new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(time, new Dictionary<IChartElement, object> { { element, value } })
			};
		}

		public IEnumerable<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>> Values { get; private set; }
	}
}