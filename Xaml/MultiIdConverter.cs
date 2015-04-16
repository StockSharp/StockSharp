namespace StockSharp.Xaml
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	using Ecng.Common;

	class MultiIdConverter : IMultiValueConverter
	{
		object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var longId = values[0].To<long?>();
			var strId = values[1].To<string>();

			var str = longId.To<string>();

			if (str == null)
				return strId;

			if (strId != null)
				return str + "/" + strId;

			return str;
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}