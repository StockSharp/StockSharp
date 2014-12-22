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
