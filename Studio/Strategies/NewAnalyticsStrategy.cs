namespace StockSharp.Studio.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Media;

	using Ecng.Xaml.Charting;
	using Ecng.Xaml.Charting.Model.DataSeries;
	using Ecng.Xaml.Charting.Numerics;
	using Ecng.Xaml.Charting.Visuals;
	using Ecng.Xaml.Charting.Visuals.Axes;
	using Ecng.Xaml.Charting.Visuals.RenderableSeries;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;
	using Ecng.Xaml;
	using Ecng.Xaml.Grids;

	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Analytics;

	public class NewAnalyticsStrategy : BaseAnalyticsStrategy
	{
		/// <summary>
		/// Создать <see cref="NewAnalyticsStrategy"/>.
		/// </summary>
		public NewAnalyticsStrategy()
		{
		}

		/// <summary>
		/// Анализировать.
		/// </summary>
		protected override void OnAnalyze()
		{
			// оповещаем программу об окончании выполнения скрипта
			base.Stop();
		}
	}
}