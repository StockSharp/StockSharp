namespace StockSharp.Xaml
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	using StockSharp.Localization;

	/// <summary>
	/// The WPF converter for <see cref="Quote"/>, which transforms the quote object into a string for visual displaying on the form.
	/// </summary>
	[ValueConversion(typeof(TimeSpan), typeof(string))]
	public class QuoteConverter : IValueConverter
	{
		/// <summary>
		/// To convert the quote into a string.
		/// </summary>
		/// <param name="value">The value produced by the binding source.</param>
		/// <param name="targetType">The type of the binding target property.</param>
		/// <param name="parameter">The converter parameter to use.</param>
		/// <param name="culture">The culture to use in the converter.</param>
		/// <returns>The string converted.</returns>
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return  LocalizedStrings.Str1567;
			else
			{
				var quote = (Quote)value;
				return LocalizedStrings.Str1568Params.Put(quote.Price, quote.Volume);
			}
		}

		/// <summary>
		/// To convert a quote from a string.
		/// </summary>
		/// <param name="value">The value produced by the binding source.</param>
		/// <param name="targetType">The type of the binding target property.</param>
		/// <param name="parameter">The converter parameter to use.</param>
		/// <param name="culture">The culture to use in the converter.</param>
		/// <returns>The quote converted.</returns>
		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}