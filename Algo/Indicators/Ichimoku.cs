namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Ишимоку.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/oscillators/ichimoku
	/// </remarks>
	[DisplayName("Ichimoku")]
	[DescriptionLoc(LocalizedStrings.Str763Key)]
	public class Ichimoku : BaseComplexIndicator
	{
		/// <summary>
		/// Создать <see cref="Ichimoku"/>.
		/// </summary>
		public Ichimoku()
			: this(new IchimokuLine { Length = 9 }, new IchimokuLine { Length = 26 })
		{
		}

		/// <summary>
		/// Создать <see cref="Ichimoku"/>.
		/// </summary>
		/// <param name="tenkan">Линия Tenkan.</param>
		/// <param name="kijun">Линия Kijun.</param>
		public Ichimoku(IchimokuLine tenkan, IchimokuLine kijun)
		{
			if (tenkan == null)
				throw new ArgumentNullException("tenkan");

			if (kijun == null)
				throw new ArgumentNullException("kijun");

			InnerIndicators.Add(Tenkan = tenkan);
			InnerIndicators.Add(Kijun = kijun);
			InnerIndicators.Add(SenkouA = new IchimokuSenkouALine(Tenkan, Kijun));
			InnerIndicators.Add(SenkouB = new IchimokuSenkouBLine(Kijun) { Length = 52 });
			InnerIndicators.Add(Chinkou = new IchimokuChinkouLine { Length = kijun.Length });
		}

		/// <summary>
		/// Линия Tenkan.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Tenkan")]
		[DescriptionLoc(LocalizedStrings.Str764Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public IchimokuLine Tenkan { get; private set; }

		/// <summary>
		/// Линия Kijun.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Kijun")]
		[DescriptionLoc(LocalizedStrings.Str765Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public IchimokuLine Kijun { get; private set; }

		/// <summary>
		/// Линия Senkou Span A.
		/// </summary>
		[ExpandableObject]
		[DisplayName("SenkouA")]
		[DescriptionLoc(LocalizedStrings.Str766Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public IchimokuSenkouALine SenkouA { get; private set; }

		/// <summary>
		/// Линия Senkou Span B.
		/// </summary>
		[ExpandableObject]
		[DisplayName("SenkouB")]
		[DescriptionLoc(LocalizedStrings.Str767Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public IchimokuSenkouBLine SenkouB { get; private set; }

		/// <summary>
		/// Линия Chinkou.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Chinkou")]
		[DescriptionLoc(LocalizedStrings.Str768Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public IchimokuChinkouLine Chinkou { get; private set; }
	}
}