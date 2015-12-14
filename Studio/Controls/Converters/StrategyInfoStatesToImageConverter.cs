#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.Converters.ControlsPublic
File: StrategyInfoStatesToImageConverter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Media.Imaging;

	using StockSharp.Studio.Core;

	public class StrategyInfoStatesToImageConverter : IValueConverter
	{
		public BitmapImage RunnedImage { get; set; }

		public BitmapImage StoppedImage { get; set; }

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			switch ((StrategyInfoStates)value)
			{
				case StrategyInfoStates.Runned:
					return RunnedImage;

				case StrategyInfoStates.Stopped:
					return StoppedImage;

				default:
					return DependencyProperty.UnsetValue;
			}
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}