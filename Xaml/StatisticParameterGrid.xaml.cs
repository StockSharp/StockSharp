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
	/// The visual panel to display parameters <see cref="IStatisticParameter"/>.
	/// </summary>
	public partial class StatisticParameterGrid
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StatisticParameterGrid"/>.
		/// </summary>
		public StatisticParameterGrid()
		{
			InitializeComponent();
			ItemsSource = Parameters = new ObservableCollection<IStatisticParameter>();

			GroupingColumns.Add(CategoryColumn);
		}

		/// <summary>
		/// The parameters to be displayed.
		/// </summary>
		public IList<IStatisticParameter> Parameters { get; }

		private StatisticManager _statisticManager;

		/// <summary>
		/// The strategy for which statistical parameters <see cref="Algo.Strategies.Strategy.StatisticManager"/> should be displayed.
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
		/// To reset current settings of statistics.
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