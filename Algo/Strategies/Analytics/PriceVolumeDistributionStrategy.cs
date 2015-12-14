#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Analytics.Algo
File: PriceVolumeDistributionStrategy.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Strategies.Analytics
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows.Media;

	using Ecng.Xaml.Charting;
	using Ecng.Xaml.Charting.Model.DataSeries;
	using Ecng.Xaml.Charting.Numerics;
	using Ecng.Xaml.Charting.Visuals.Axes;
	using Ecng.Xaml.Charting.Visuals.RenderableSeries;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Xaml;
	using Ecng.Xaml.Grids;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The analytic strategy, calculating distribution of the volume by price levels.
	/// </summary>
	public class PriceVolumeDistributionStrategy : BaseAnalyticsStrategy
	{
		private class GridRow : NotifiableObject
		{
			public decimal Price { get; set; }

			private decimal _volume;

			public decimal Volume
			{
				get { return _volume; }
				set
				{
					_volume = value;
					NotifyChanged("Price");
				}
			}
		}

		private readonly StrategyParam<TimeSpan> _timeFrame;

		/// <summary>
		/// Time-frame.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str1242Key)]
		[DescriptionLoc(LocalizedStrings.Str1243Key)]
		[CategoryLoc(LocalizedStrings.Str1221Key)]
		[PropertyOrder(2)]
		public TimeSpan TimeFrame
		{
			get { return _timeFrame.Value; }
			set { _timeFrame.Value = value; }
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="PriceVolumeDistributionStrategy"/>.
		/// </summary>
		public PriceVolumeDistributionStrategy()
		{
			_timeFrame = this.Param("TimeFrame", TimeSpan.FromMinutes(5));
		}

		/// <summary>
		/// To analyze.
		/// </summary>
		protected override void OnAnalyze()
		{
			var chart = Chart;
			var grid = Grid;

			var chartSeries = new XyDataSeries<double, double>();
			ThreadSafeObservableCollection<GridRow> gridSeries = null;

			chart.GuiSync(() =>
			{
				// clear prev values
				chart.RenderableSeries.Clear();
				grid.Columns.Clear();

				chart.RenderableSeries.Add(new FastColumnRenderableSeries
				{
					ResamplingMode = ResamplingMode.None,
					DataPointWidth = 1,
					SeriesColor = Colors.Chocolate,
					DataSeries = chartSeries
				});

				chart.XAxis = new NumericAxis { AxisTitle = LocalizedStrings.Price };
				chart.YAxis = new NumericAxis { AxisTitle = LocalizedStrings.Volume, GrowBy = new DoubleRange(0, 0.1) };

				grid.AddTextColumn("Price", LocalizedStrings.Price).Width = 150;
				var volumeColumn = grid.AddTextColumn("Volume", LocalizedStrings.Volume);
				volumeColumn.Width = 100;

				var gridSource = new ObservableCollectionEx<GridRow>();
				grid.ItemsSource = gridSource;
				gridSeries = new ThreadSafeObservableCollection<GridRow>(gridSource);

				grid.SetSort(volumeColumn, ListSortDirection.Descending);
			});

			// get candle storage
			var storage = StorateRegistry.GetCandleStorage(typeof(TimeFrameCandle), Security, TimeFrame, format: StorageFormat);

			// get available dates for the specified period
			var dates = storage.GetDates(From, To).ToArray();

			var rows = new Dictionary<decimal, GridRow>();

			foreach (var loadDate in dates)
			{
				// check if stopped
				if (ProcessState != ProcessStates.Started)
					break;

				// load candles
				var candles = storage.Load(loadDate);

				// groupping candles by candle's middle price
				var groupedCandles = candles.GroupBy(c => c.LowPrice + c.GetLength() / 2);

				foreach (var group in groupedCandles.OrderBy(g => g.Key))
				{
					// check if stopped
					if (ProcessState != ProcessStates.Started)
						break;

					var price = group.Key;

					// calc total volume for the specified time frame
					var sumVol = group.Sum(c => c.TotalVolume);

					var row = rows.TryGetValue(price);
					if (row == null)
					{
						// new price level
						rows.Add(price, row = new GridRow { Price = price, Volume = sumVol });

						// draw on chart
						chartSeries.Append((double)price, (double)sumVol);

						// draw on table
						gridSeries.Add(row);
					}
					else
					{
						// update existing price level
						row.Volume += sumVol;

						// update chart
						chartSeries.Update((double)price, (double)row.Volume);
					}
				}

				chart.GuiAsync(() =>
				{
					// update grid sorting
					grid.RefreshSort();

					// scale chart
					chart.ZoomExtents();
				});
			}

			// notify the script stopped
			Stop();
		}
	}
}