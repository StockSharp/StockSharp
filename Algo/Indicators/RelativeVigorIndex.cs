#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: RelativeVigorIndex.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Relative Vigor Index.
	/// </summary>
	[DisplayName("RVI")]
	[DescriptionLoc(LocalizedStrings.Str771Key)]
	public class RelativeVigorIndex : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RelativeVigorIndex"/>.
		/// </summary>
		public RelativeVigorIndex()
			: this(new RelativeVigorIndexAverage(), new RelativeVigorIndexSignal())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RelativeVigorIndex"/>.
		/// </summary>
		/// <param name="average">Average indicator part.</param>
		/// <param name="signal">Signaling part of indicator.</param>
		public RelativeVigorIndex(RelativeVigorIndexAverage average, RelativeVigorIndexSignal signal)
			: base(average, signal)
		{
			Average = average;
			Signal = signal;

			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <summary>
		/// Average indicator part.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.AverageKey)]
		[DescriptionLoc(LocalizedStrings.Str772Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RelativeVigorIndexAverage Average { get; }

		/// <summary>
		/// Signaling part of indicator.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayNameLoc(LocalizedStrings.SignalKey)]
		[DescriptionLoc(LocalizedStrings.Str773Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RelativeVigorIndexSignal Signal { get; }
	}
}