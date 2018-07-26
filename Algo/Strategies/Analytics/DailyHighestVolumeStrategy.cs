namespace StockSharp.Algo.Strategies.Analytics
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// The analytic strategy, calculating distribution of the biggest volume by hours.
	/// </summary>
	public class DailyHighestVolumeStrategy : BaseAnalyticsStrategy
	{
		private class GridRow : NotifiableObject
		{
			public TimeSpan Time { get; set; }

			private decimal _volume;

			public decimal Volume
			{
				get => _volume;
				set
				{
					_volume = value;
					NotifyChanged(nameof(Volume));
				}
			}
		}

		private readonly StrategyParam<TimeSpan> _timeFrame;

		/// <summary>
		/// Time-frame.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str1242Key,
			Description = LocalizedStrings.Str1243Key,
			GroupName = LocalizedStrings.AnalyticsKey,
			Order = 0)]
		public TimeSpan TimeFrame
		{
			get => _timeFrame.Value;
			set => _timeFrame.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DailyHighestVolumeStrategy"/>.
		/// </summary>
		public DailyHighestVolumeStrategy()
		{
			_timeFrame = this.Param(nameof(TimeFrame), TimeSpan.FromMinutes(5));
		}

		/// <summary>
		/// To analyze.
		/// </summary>
		protected override void OnAnalyze()
		{
			// clear prev values
			Panel.ClearControls();

			ThreadSafeObservableCollection<GridRow> gridSeries = null;
			IAnalyticsChart chart = null;

			switch (ResultType)
			{
				case AnalyticsResultTypes.Grid:
				{
					var grid = Panel.CreateGrid(LocalizedStrings.Str3280);

					grid.AddColumn(nameof(GridRow.Time), LocalizedStrings.Time).Width = 150;
					var volumeColumn = grid.AddColumn(nameof(GridRow.Volume), LocalizedStrings.Volume);
					volumeColumn.Width = 100;

					var gridSource = new ObservableCollectionEx<GridRow>();
					grid.ItemsSource = gridSource;
					gridSeries = new ThreadSafeObservableCollection<GridRow>(gridSource);

					grid.SetSort(volumeColumn, ListSortDirection.Descending);
					break;
				}
				case AnalyticsResultTypes.Bubble:
					chart = Panel.CreateBubbleChart(LocalizedStrings.Str3200);
					break;
				case AnalyticsResultTypes.Heatmap:
					chart = Panel.CreateHeatmap(LocalizedStrings.Str3200);
					break;
				case AnalyticsResultTypes.Histogram:
					chart = Panel.CreateHistogramChart(LocalizedStrings.Str3200);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			// get candle storage
			var storage = StorateRegistry.GetCandleStorage(typeof(TimeFrameCandle), Security, TimeFrame, format: StorageFormat);
			
			// get available dates for the specified period
			var dates = storage.GetDates(From, To).ToArray();

			if (dates.Length == 0)
			{
				this.AddWarningLog(LocalizedStrings.Str2913);
			}
			else
			{
				var rows = new Dictionary<TimeSpan, GridRow>();

				foreach (var loadDate in dates)
				{
					// check if stopped
					if (ProcessState != ProcessStates.Started)
						break;

					// load candles
					var candles = storage.Load(loadDate);

					// groupping candles by open time
					var groupedCandles = candles.GroupBy(c => c.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1)));

					foreach (var group in groupedCandles.OrderBy(g => g.Key))
					{
						// check if stopped
						if (ProcessState != ProcessStates.Started)
							break;

						var time = group.Key;

						// calc total volume for the specified time frame
						var sumVol = group.Sum(c => c.TotalVolume);

						var row = rows.TryGetValue(time);
						if (row == null)
						{
							// new volume level
							rows.Add(time, row = new GridRow { Time = time, Volume = sumVol });

							// draw on chart
							chart?.Append(DateTime.Today + time, sumVol, sumVol / 1000);

							// draw on table
							gridSeries?.Add(row);
						}
						else
						{
							// update existing volume level
							row.Volume += sumVol;

							// update chart
							chart?.Update(DateTime.Today + time, row.Volume, row.Volume / 1000);
						}
					}
				
					//// scale chart
					//chart?.ZoomExtents();
				}
			}

			// notify the script stopped
			Stop();
		}
	}
}