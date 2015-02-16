namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Reflection;
	using System.Windows.Data;

	using Ecng.Common;
	using Ecng.Reflection;
	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// WPF-конвертер для <see cref="IExtendableEntity"/>, который преобразует запись из <see cref="IExtendableEntity.ExtensionInfo"/> в сторку для визуального отображения на форме.
	/// </summary>
	[ValueConversion(typeof(TimeSpan), typeof(string))]
	public class ExtensionInfoConverter : IValueConverter
	{
		private readonly static Dictionary<Type, IDictionary<string, DdeTableColumn>> _columns = new Dictionary<Type, IDictionary<string, DdeTableColumn>>();

		static ExtensionInfoConverter()
		{
			_columns.Add(typeof(Security), GetColumnsDictionary(typeof(DdeSecurityColumns)));
			_columns.Add(typeof(Trade), GetColumnsDictionary(typeof(DdeTradeColumns)));
			_columns.Add(typeof(MyTrade), GetColumnsDictionary(typeof(DdeMyTradeColumns)));
			_columns.Add(typeof(Order), GetColumnsDictionary(typeof(DdeOrderColumns)));
			_columns.Add(typeof(DdeStopOrderColumns), GetColumnsDictionary(typeof(DdeStopOrderColumns)));
			_columns.Add(typeof(Quote), GetColumnsDictionary(typeof(DdeQuoteColumns)));
			_columns.Add(typeof(Position), GetColumnsDictionary(typeof(DdeEquityPositionColumns)));
			_columns.Add(typeof(DdeDerivativePositionColumns), GetColumnsDictionary(typeof(DdeDerivativePositionColumns)));
		}

		private static IDictionary<string, DdeTableColumn> GetColumnsDictionary(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return type.GetMembers<PropertyInfo>().ToDictionary(p => p.Name, p => p.GetValue<VoidType, DdeTableColumn>(null));
		}

		/// <summary>
		/// Сконвертировать запись в строку. 
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
				throw new ArgumentNullException("value");

			if (parameter == null)
				return null;

			var entity = (IExtendableEntity)value;
			if (entity.ExtensionInfo == null)
				return null;

			var columns = _columns.TryGetValue(GetKey(entity));
			if(columns == null)
				throw new ArgumentException(LocalizedStrings.Str1708Params.Put(entity.GetType()), "value");

			var column = columns.TryGetValue((string)parameter);
			if (column == null)
				throw new ArgumentException(LocalizedStrings.Str1709Params.Put(parameter, entity.GetType()), "parameter");

			return entity.ExtensionInfo.TryGetValue(column);
		}

		private static Type GetKey(IExtendableEntity entity)
		{
			var order = entity as Order;
			if (order != null && order.Type == OrderTypes.Conditional)
				return typeof(DdeStopOrderColumns);

			var position = entity as Position;
			if (position != null && (position.Security.Type == SecurityTypes.Future || position.Security.Type == SecurityTypes.Forward || position.Security.Type == SecurityTypes.Option))
				return typeof(DdeDerivativePositionColumns);

			return entity.GetType();
		}

		/// <summary>
		/// Сконвертировать запись из строки. 
		/// </summary>
		/// <returns>
		/// Сконвертированная запись.
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