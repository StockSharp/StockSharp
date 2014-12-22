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
				throw new ArgumentNullException("values");

			Values = values;
		}

		public ChartDrawCommand(DateTimeOffset time, IDictionary<IChartElement, object> values)
		{
			if (values == null)
				throw new ArgumentNullException("values");

			Values = new List<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>>
			{
				new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(time, values)
			};
		}

		public ChartDrawCommand(DateTimeOffset time, IChartElement element, object value)
		{
			if (element == null)
				throw new ArgumentNullException("element");

			if (value == null)
				throw new ArgumentNullException("value");

			Values = new List<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>>
			{
				new RefPair<DateTimeOffset, IDictionary<IChartElement, object>>(time, new Dictionary<IChartElement, object> { { element, value } })
			};
		}

		public IEnumerable<RefPair<DateTimeOffset, IDictionary<IChartElement, object>>> Values { get; private set; }
	}
}