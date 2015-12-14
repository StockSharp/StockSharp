#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.Converters.ControlsPublic
File: StatisticsStorageConverter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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