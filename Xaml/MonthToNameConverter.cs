namespace StockSharp.Xaml
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	sealed class MonthToNameConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return culture.DateTimeFormat.GetMonthName((int)value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}