namespace StockSharp.Studio.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Media;

	using Abt.Controls.SciChart;
	using Abt.Controls.SciChart.Model.DataSeries;
	using Abt.Controls.SciChart.Numerics;
	using Abt.Controls.SciChart.Visuals;
	using Abt.Controls.SciChart.Visuals.Axes;
	using Abt.Controls.SciChart.Visuals.RenderableSeries;

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