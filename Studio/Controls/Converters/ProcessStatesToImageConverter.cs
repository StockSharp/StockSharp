#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Controls.Converters.ControlsPublic
File: ProcessStatesToImageConverter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Controls.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Media.Imaging;

	using StockSharp.Algo;

	public class ProcessStatesToImageConverter : IValueConverter
	{
		public BitmapImage StartedImage { get; set; }

		public BitmapImage StoppingImage { get; set; }

		public BitmapImage StoppedImage { get; set; }

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			switch ((ProcessStates)value)
			{
				case ProcessStates.Started:
					return StartedImage;

				case ProcessStates.Stopping:
					return StoppingImage;

				case ProcessStates.Stopped:
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
