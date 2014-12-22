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