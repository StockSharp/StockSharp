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
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Ichimoku.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/ichimoku.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IchimokuKey,
		Description = LocalizedStrings.IchimokuKey)]
	[Doc("topics/api/indicators/list_of_indicators/ichimoku.html")]
	public class Ichimoku : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Ichimoku"/>.
		/// </summary>
		public Ichimoku()
			: this(new() { Length = 9 }, new() { Length = 26 })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Ichimoku"/>.
		/// </summary>
		/// <param name="tenkan">Tenkan line.</param>
		/// <param name="kijun">Kijun line.</param>
		public Ichimoku(IchimokuLine tenkan, IchimokuLine kijun)
		{
			AddInner(Tenkan = tenkan ?? throw new ArgumentNullException(nameof(tenkan)));
			AddInner(Kijun = kijun ?? throw new ArgumentNullException(nameof(kijun)));
			AddInner(SenkouA = new IchimokuSenkouALine(Tenkan, Kijun));
			AddInner(SenkouB = new IchimokuSenkouBLine(Kijun) { Length = 52 });
			AddInner(Chinkou = new IchimokuChinkouLine { Length = kijun.Length });
		}

		/// <summary>
		/// Tenkan line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TenkanKey,
			Description = LocalizedStrings.TenkanLineKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public IchimokuLine Tenkan { get; }

		/// <summary>
		/// Kijun line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.KijunKey,
			Description = LocalizedStrings.KijunLineKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public IchimokuLine Kijun { get; }

		/// <summary>
		/// Senkou (A) line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SenkouAKey,
			Description = LocalizedStrings.SenkouADescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public IchimokuSenkouALine SenkouA { get; }

		/// <summary>
		/// Senkou (B) line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SenkouBKey,
			Description = LocalizedStrings.SenkouBDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public IchimokuSenkouBLine SenkouB { get; }

		/// <summary>
		/// Chinkou line.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ChinkouKey,
			Description = LocalizedStrings.ChinkouLineKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public IchimokuChinkouLine Chinkou { get; }

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $" T={Tenkan.Length} K={Kijun.Length} A={SenkouA.Length} B={SenkouB.Length} C={Chinkou.Length}";
	}
}
