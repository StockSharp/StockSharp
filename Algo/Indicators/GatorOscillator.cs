#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: GatorOscillator.cs
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
	/// Gator oscillator.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/gator.
	/// </remarks>
	[DisplayName("Gator")]
	[DescriptionLoc(LocalizedStrings.Str850Key)]
	public class GatorOscillator : BaseComplexIndicator
	{
		private readonly Alligator _alligator;

		/// <summary>
		/// Initializes a new instance of the <see cref="GatorOscillator"/>.
		/// </summary>
		public GatorOscillator()
		{
			_alligator = new Alligator();
			Histogram1 = new GatorHistogram(_alligator.Jaw, _alligator.Lips, false);
			Histogram2 = new GatorHistogram(_alligator.Lips, _alligator.Teeth, true);
			InnerIndicators.Add(Histogram1);
			InnerIndicators.Add(Histogram2);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GatorOscillator"/>.
		/// </summary>
		/// <param name="alligator">Alligator.</param>
		/// <param name="histogram1">Top histogram.</param>
		/// <param name="histogram2">Lower histogram.</param>
		public GatorOscillator(Alligator alligator, GatorHistogram histogram1, GatorHistogram histogram2)
			: base(histogram1, histogram2)
		{
			_alligator = alligator ?? throw new ArgumentNullException(nameof(alligator));
			Histogram1 = histogram1;
			Histogram2 = histogram2;
		}

		/// <summary>
		/// Top histogram.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName(LocalizedStrings.Str3564Key)]
		[DescriptionLoc(LocalizedStrings.Str851Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public GatorHistogram Histogram1 { get; }

		/// <summary>
		/// Lower histogram.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[DisplayName(LocalizedStrings.Str3565Key)]
		[DescriptionLoc(LocalizedStrings.Str852Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public GatorHistogram Histogram2 { get; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _alligator.IsFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			_alligator.Process(input);

			return base.OnProcess(input);
		}
	}
}