namespace StockSharp.Algo.Strategies.Analytics
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows.Media;

	using Abt.Controls.SciChart;
	using Abt.Controls.SciChart.Model.DataSeries;
	using Abt.Controls.SciChart.Numerics;
	using Abt.Controls.SciChart.Visuals.Axes;
	using Abt.Controls.SciChart.Visuals.RenderableSeries;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Collections;
	using Ecng.Xaml;
	using Ecng.Xaml.Grids;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Аналитическая стратегия, расчитывающая распределение наибольшего объема по часам.
	/// </summary>
	public class DailyHighestVolumeStrategy : BaseAnalyticsStrategy
	{
		private class GridRow : NotifiableObject
		{
			public TimeSpan Time { get; set; }

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
		/// Тайм-фрейм.
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
		/// Создать <see cref="DailyHighestVolumeStrategy"/>.
		/// </summary>
		public DailyHighestVolumeStrategy()
		{
			_timeFrame = this.Param("TimeFrame", TimeSpan.FromMinutes(5));
		}

		/// <summary>
		/// Анализировать.
		/// </summary>
		protected override void OnAnalyze()
		{
			var chart = Chart;
			var grid = Grid;

			var chartSeries = new XyzDataSeries<DateTime, double, double>();
			ThreadSafeObservableCollection<GridRow> gridSeries = null;

			chart.GuiSync(() =>
			{
				// очищаем данные с предыдущего запуска скрипта
				chart.RenderableSeries.Clear();
				grid.Columns.Clear();

				chart.RenderableSeries.Add(new FastBubbleRenderableSeries
				{
					ResamplingMode = ResamplingMode.Auto,
					BubbleColor = Colors.Chocolate,
					ZScaleFactor = 0.1,
					AutoZRange = true,
					DataSeries = chartSeries
				});

				chart.XAxis = new DateTimeAxis { GrowBy = new DoubleRange(0.0, 0.1) };
				chart.YAxis = new NumericAxis { GrowBy = new DoubleRange(0.1, 0.1) };

				grid.AddTextColumn("Time", LocalizedStrings.Str219).Width = 150;
				var volumeColumn = grid.AddTextColumn("Volume", LocalizedStrings.Volume);
				volumeColumn.Width = 100;

				var gridSource = new ObservableCollectionEx<GridRow>();
				grid.ItemsSource = gridSource;
				gridSeries = new ThreadSafeObservableCollection<GridRow>(gridSource);

				grid.SetSort(volumeColumn, ListSortDirection.Descending);
			});

			// получаем хранилище свечек
			var storage = StorateRegistry.GetCandleStorage(typeof(TimeFrameCandle), Security, TimeFrame, format: StorageFormat);
			
			// получаем набор доступных дат за указанный период
			var dates = storage.GetDates(From, To).ToArray();

			var rows = new Dictionary<TimeSpan, GridRow>();

			foreach (var loadDate in dates)
			{
				// проверяем флаг остановки
				if (ProcessState != ProcessStates.Started)
					break;

				// загружаем свечки
				var candles = storage.Load(loadDate);

				// группируем свечки по часовой отметке времени
				var groupedCandles = candles.GroupBy(c => c.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1)));

				foreach (var group in groupedCandles.OrderBy(g => g.Key))
				{
					// проверяем флаг остановки
					if (ProcessState != ProcessStates.Started)
						break;

					var time = group.Key;

					// получаем суммарный объем в пределах часовой отметки
					var sumVol = group.Sum(c => c.TotalVolume);

					var row = rows.TryGetValue(time);
					if (row == null)
					{
						// пришел новый уровень - добавляем новую запись
						rows.Add(time, row = new GridRow { Time = time, Volume = sumVol });

						// выводим на график
						chartSeries.Append(DateTime.Today + time, (double)sumVol, (double)sumVol / 1000);

						// выводит в таблицу
						gridSeries.Add(row);
					}
					else
					{
						// увеличиваем суммарный объем
						row.Volume += sumVol;

						// обновляем график
						chartSeries.Update(DateTime.Today + time, (double)row.Volume, (double)row.Volume / 1000);
					}
				}
				
				chart.GuiAsync(() =>
				{
					// обновление сортировки в таблице
					grid.RefreshSort();

					// автомасштабирование графика
					chart.ZoomExtents();
				});
			}

			// оповещаем программу об окончании выполнения скрипта
			base.Stop();
		}
	}
}