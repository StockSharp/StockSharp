namespace StockSharp.Studio.Controls.Converters
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	using Ecng.Common;
	using Ecng.Serialization;

	public class StatisticsStorageConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var param = (string)parameter;
			var storage = (SettingsStorage)value;

			if (storage == null || param.IsEmptyOrWhiteSpace())
				return null;

			var paramStorage = storage.GetValue<SettingsStorage>(param);

			if (paramStorage == null || paramStorage.Count == 0)
				return null;

			return paramStorage.GetValue<decimal>("Value");
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}