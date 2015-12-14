#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Strategies.StrategiesPublic
File: NewAnalyticsStrategy.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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