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

	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Xaml;
	using Ecng.Xaml.Grids;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Аналитическая стратегия, расчитывающая распределение объема по ценовым уровням.
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
		/// Создать <see cref="PriceVolumeDistributionStrategy"/>.
		/// </summary>
		public PriceVolumeDistributionStrategy()
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

			var chartSeries = new XyDataSeries<double, double>();
			ThreadSafeObservableCollection<GridRow> gridSeries = null;

			chart.GuiSync(() =>
			{
				// очищаем данные с предыдущего запуска скрипта
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

			// получаем хранилище свечек
			var storage = StorateRegistry.GetCandleStorage(typeof(TimeFrameCandle), Security, TimeFrame, format: StorageFormat);

			// получаем набор доступных дат за указанный период
			var dates = storage.GetDates(From, To).ToArray();

			var rows = new Dictionary<decimal, GridRow>();

			foreach (var loadDate in dates)
			{
				// проверяем флаг остановки
				if (ProcessState != ProcessStates.Started)
					break;

				// загружаем свечки
				var candles = storage.Load(loadDate);

				// группируем свечки по цене (середина свечи)
				var groupedCandles = candles.GroupBy(c => c.LowPrice + c.GetLength() / 2);

				foreach (var group in groupedCandles.OrderBy(g => g.Key))
				{
					// проверяем флаг остановки
					if (ProcessState != ProcessStates.Started)
						break;

					var price = group.Key;

					// получаем суммарный объем в пределах ценового уровня за день
					var sumVol = group.Sum(c => c.TotalVolume);

					var row = rows.TryGetValue(price);
					if (row == null)
					{
						// пришел новый уровень - добавляем новую запись
						rows.Add(price, row = new GridRow { Price = price, Volume = sumVol });

						// выводим на график
						chartSeries.Append((double)price, (double)sumVol);

						// выводит в таблицу
						gridSeries.Add(row);
					}
					else
					{
						// увеличиваем суммарный объем
						row.Volume += sumVol;

						// обновляем график
						chartSeries.Update((double)price, (double)row.Volume);
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