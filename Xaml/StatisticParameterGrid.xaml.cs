namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Globalization;
	using System.Windows.Data;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Statistics;

	/// <summary>
	/// Визуальная панель для отображения параметров <see cref="IStatisticParameter"/>.
	/// </summary>
	public partial class StatisticParameterGrid
	{
		/// <summary>
		/// Создать <see cref="StatisticParameterGrid"/>.
		/// </summary>
		public StatisticParameterGrid()
		{
			InitializeComponent();
			ItemsSource = Parameters = new ObservableCollection<IStatisticParameter>();

			GroupingColumns.Add(CategoryColumn);
		}

		/// <summary>
		/// Параметры, которые необходимо отображать.
		/// </summary>
		public IList<IStatisticParameter> Parameters { get; private set; }

		private StatisticManager _statisticManager;

		/// <summary>
		/// Стратегия, статистические параметры <see cref="Algo.Strategies.Strategy.StatisticManager"/> которой необходимо отображать.
		/// </summary>
		public StatisticManager StatisticManager
		{
			get { return _statisticManager; }
			set
			{
				if (value == StatisticManager)
					return;

				_statisticManager = value;
				Reset();
			}
		}

		/// <summary>
		/// Сбросить текущие значения параметров статистики.
		/// </summary>
		public void Reset()
		{
			Parameters.Clear();

			if (StatisticManager != null)
				Parameters.AddRange(StatisticManager.Parameters);
		}
	}

	sealed class CellValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			return value.GetType().IsNumeric() 
				? "{0:0.##}".Put(value) 
				: value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}