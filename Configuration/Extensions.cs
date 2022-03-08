#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Configuration.ConfigurationPublic
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Configuration
{
	using System;
	using System.Linq;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Configuration;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class Extensions
	{
		//private static readonly Type[] _customCandles = Array.Empty<Type>();

		//static Extensions()
		//{
		//	var section = RootSection;

		//	if (section == null)
		//		return;

		//	_customIndicators = SafeAdd<IndicatorElement, IndicatorType>(section.CustomIndicators, elem => new IndicatorType(elem.Type.To<Type>(), elem.Painter.To<Type>()));
		//	_customCandles = SafeAdd<CandleElement, Type>(section.CustomCandles, elem => elem.Type.To<Type>());
		//}

		/// <summary>
		/// Instance of the root section <see cref="StockSharpSection"/>.
		/// </summary>
		public static StockSharpSection RootSection => ConfigManager.InnerConfig.Sections.OfType<StockSharpSection>().FirstOrDefault();

		//private static Type[] _candles;

		///// <summary>
		///// Get all candles.
		///// </summary>
		///// <returns>All candles.</returns>
		//public static IEnumerable<Type> GetCandles()
		//{
		//	return _candles ?? (_candles = typeof(Candle).Assembly
		//		.GetTypes()
		//		.Where(t => !t.IsAbstract && t.IsCandle())
		//		.Concat(_customCandles)
		//		.ToArray());
		//}

		/// <summary>
		/// </summary>
		public static long GetProductId()
		{
			var prodIdAttr = Assembly.GetEntryAssembly().GetAttribute<ProductIdAttribute>();

			if (prodIdAttr is null)
				throw new InvalidOperationException($"{nameof(ProductIdAttribute)} is missing.");

			return prodIdAttr.ProductId;
		}
	}
}