namespace StockSharp.Studio
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	using Ecng.Common;

	using StockSharp.Studio.Core;

	class ObjectToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var type = parameter as Type;

			return (type == null && value != null) || (value != null && ((IContentWindow)value).Control.GetType() == type)
				? Visibility.Visible
				: Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	class StrategyToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var strategy = value as StrategyContainer;
			var type = parameter as string;

			if(strategy == null)
				return Visibility.Collapsed;

			switch (strategy.StrategyInfo.Type)
			{
				case StrategyInfoTypes.SourceCode:
				case StrategyInfoTypes.Diagram:
				case StrategyInfoTypes.Assembly:
					return type == null || type == strategy.StrategyInfo.Type.To<string>() ? Visibility.Visible : Visibility.Collapsed;

				case StrategyInfoTypes.Analytics:
				case StrategyInfoTypes.Terminal:
					return type == strategy.StrategyInfo.Type.To<string>() ? Visibility.Visible : Visibility.Collapsed;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	class StrategyInfoTypeToVisibilityConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var info = values[0] as StrategyInfo;
			var strategy = values[1] as StrategyContainer;
			var type = parameter as string;

			if (info == null || type == null || strategy != null)
				return Visibility.Collapsed;

			return info.Type.ToString() == type ? Visibility.Visible : Visibility.Collapsed;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	class SessionTypeToGalleryVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var type = (SessionType)value;

			return type != SessionType.Optimization
				? Visibility.Visible
				: Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	public class BoolToStringConverter : IValueConverter
	{
		public string FalseValue { get; set; }

		public string TrueValue { get; set; }

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool)value ? TrueValue : FalseValue;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((string)value == TrueValue);
		}
	}
}
