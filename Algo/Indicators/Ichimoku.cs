#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: Ichimoku.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Ichimoku.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/oscillators/ichimoku.
	/// </remarks>
	[DisplayNameLoc(LocalizedStrings.Str763Key)]
	[DescriptionLoc(LocalizedStrings.Str763Key, true)]
	public class Ichimoku : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Ichimoku"/>.
		/// </summary>
		public Ichimoku()
			: this(new IchimokuLine { Length = 9 }, new IchimokuLine { Length = 26 })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Ichimoku"/>.
		/// </summary>
		/// <param name="tenkan">Tenkan line.</param>
		/// <param name="kijun">Kijun line.</param>
		public Ichimoku(IchimokuLine tenkan, IchimokuLine kijun)
		{
			if (tenkan == null)
				throw new ArgumentNullException(nameof(tenkan));

			if (kijun == null)
				throw new ArgumentNullException(nameof(kijun));

			InnerIndicators.Add(Tenkan = tenkan);
			InnerIndicators.Add(Kijun = kijun);
			InnerIndicators.Add(SenkouA = new IchimokuSenkouALine(Tenkan, Kijun));
			InnerIndicators.Add(SenkouB = new IchimokuSenkouBLine(Kijun) { Length = 52 });
			InnerIndicators.Add(Chinkou = new IchimokuChinkouLine { Length = kijun.Length });
		}

		/// <summary>
		/// Tenkan line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("Tenkan")]
		[DescriptionLoc(LocalizedStrings.Str764Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public IchimokuLine Tenkan { get; }

		/// <summary>
		/// Kijun line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("Kijun")]
		[DescriptionLoc(LocalizedStrings.Str765Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public IchimokuLine Kijun { get; }

		/// <summary>
		/// Senkou (A) line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("SenkouA")]
		[DescriptionLoc(LocalizedStrings.Str766Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public IchimokuSenkouALine SenkouA { get; }

		/// <summary>
		/// Senkou (B) line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("SenkouB")]
		[DescriptionLoc(LocalizedStrings.Str767Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public IchimokuSenkouBLine SenkouB { get; }

		/// <summary>
		/// Chinkou line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName("Chinkou")]
		[DescriptionLoc(LocalizedStrings.Str768Key, true)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public IchimokuChinkouLine Chinkou { get; }
	}
}