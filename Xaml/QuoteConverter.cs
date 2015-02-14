namespace StockSharp.Xaml
{
	using System;
	using System.Globalization;
	using System.Windows.Data;

	using Ecng.Common;

	using StockSharp.BusinessEntities;

	using StockSharp.Localization;

	/// <summary>
	/// WPF-конвертер для <see cref="Quote"/>, который преобразует объект котировки в строку для визуального отображения на форме.
	/// </summary>
	[ValueConversion(typeof(TimeSpan), typeof(string))]
	public class QuoteConverter : IValueConverter
	{
		/// <summary>
		/// Сконвертировать котировку в строку. 
		/// </summary>
		/// <returns>
		/// Сконвертированная строка.
		/// </returns>
		/// <param name="value">The value produced by the binding source.</param>
		/// <param name="targetType">The type of the binding target property.</param>
		/// <param name="parameter">The converter parameter to use.</param>
		/// <param name="culture">The culture to use in the converter.</param>
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
		/// Сконвертировать котировку из строки. 
		/// </summary>
		/// <returns>
		/// Сконвертированная котировка.
		/// </returns>
		/// <param name="value">The value produced by the binding source.</param>
		/// <param name="targetType">The type of the binding target property.</param>
		/// <param name="parameter">The converter parameter to use.</param>
		/// <param name="culture">The culture to use in the converter.</param>
		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}