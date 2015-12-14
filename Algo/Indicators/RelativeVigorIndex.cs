#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: RelativeVigorIndex.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

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
		/// <param name="signal">Signalling part of indicator.</param>
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
		[ExpandableObject]
		[DisplayName("Average")]
		[DescriptionLoc(LocalizedStrings.Str772Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RelativeVigorIndexAverage Average { get; private set; }

		/// <summary>
		/// Signalling part of indicator.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Signal")]
		[DescriptionLoc(LocalizedStrings.Str773Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RelativeVigorIndexSignal Signal { get; private set; }
	}
}