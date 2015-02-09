namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Net;
	using System.Windows;
	using System.Windows.Data;

	using Ecng.Common;

	using Xceed.Wpf.Toolkit.PropertyGrid;
	using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

	/// <summary>
	/// Редактор для коллекции <see cref="EndPoint"/>.
	/// </summary>
	public partial class EndPointListEditor : ITypeEditor
	{
		/// <summary>
		/// Создать <see cref="EndPointListEditor"/>.
		/// </summary>
		public EndPointListEditor()
		{
			InitializeComponent();
			//Address.Mask = @"[а-яА-Яa-zA-Z0-9\.\-\,]+:?\d+";
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="EndPoints"/>.
		/// </summary>
		public static readonly DependencyProperty EndPointsProperty =
			DependencyProperty.Register("EndPoints", typeof(IEnumerable<EndPoint>), typeof(EndPointListEditor), new PropertyMetadata(Enumerable.Empty<EndPoint>()));

		/// <summary>
		/// Адреса.
		/// </summary>
		public IEnumerable<EndPoint> EndPoints
		{
			get { return (IEnumerable<EndPoint>)GetValue(EndPointsProperty); }
			set { SetValue(EndPointsProperty, value); }
		}

		FrameworkElement ITypeEditor.ResolveEditor(PropertyItem propertyItem)
		{
			BindingOperations.SetBinding(this, EndPointsProperty, new Binding("Value")
			{
				Source = propertyItem,
				ValidatesOnExceptions = true,
				ValidatesOnDataErrors = true,
				Mode = BindingMode.TwoWay
			});

			return this;
		}
	}

	class EndPointListConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var endPoints = value as IEnumerable<EndPoint>;
			return endPoints == null ? null : endPoints.Select(e => e.To<string>()).Join(",");
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.To<string>().Split(",").Select(s => s.To<EndPoint>()).ToArray();
		}
	}
}