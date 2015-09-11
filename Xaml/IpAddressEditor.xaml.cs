namespace StockSharp.Xaml
{
	using System;
	using System.Globalization;
	using System.Net;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;

	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Localization;

	/// <summary>
	/// Editor for <see cref="IPAddress"/>.
	/// </summary>
	public partial class IpAddressEditor
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IpAddressEditor"/>.
		/// </summary>
		public IpAddressEditor()
		{
			InitializeComponent();
			AddressCtrl.Mask = @"(\s*|[0-9]{1,3}[.][0-9]{1,3}[.][0-9]{1,3}[.][0-9]{1,3})";
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="IpAddressEditor.Address"/>.
		/// </summary>
		public static readonly DependencyProperty AddressProperty =
			DependencyProperty.Register("Address", typeof(IPAddress), typeof(IpAddressEditor), new PropertyMetadata(default(IPAddress)));

		/// <summary>
		/// Address.
		/// </summary>
		public IPAddress Address
		{
			get { return (IPAddress)GetValue(AddressProperty); }
			set { SetValue(AddressProperty, value); }
		}
	}

	class IpAddressValidationRule : ValidationRule
	{
		public bool Multi { get; set; }

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (value == null)
				return new ValidationResult(false, LocalizedStrings.Str1459);

			try
			{
				if (Multi)
					value.To<string>().Split(",").ForEach(v => v.To<IPAddress>());
				else
					value.To<IPAddress>();

				return ValidationResult.ValidResult;
			}
			catch (Exception)
			{
				return new ValidationResult(false, LocalizedStrings.Str1459);
			}
		}
	}

	class IpAddressConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.To<string>();
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value.To<IPAddress>();
		}
	}
}