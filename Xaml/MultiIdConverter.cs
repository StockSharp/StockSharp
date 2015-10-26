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
			var longId = values[0].WpfCast<string>();
			var strId = values[1].WpfCast<string>();

			if (longId.IsEmpty())
			{
				if (strId.IsEmpty())
					return Binding.DoNothing;

				return strId;
			}

			if (strId.IsEmpty())
				return longId;
			
			return longId + "/" + strId;
		}

		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}