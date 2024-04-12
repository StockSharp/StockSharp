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
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Gator oscillator.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/gator_oscillator.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.GatorKey,
		Description = LocalizedStrings.GatorOscillatorKey)]
	[Doc("topics/api/indicators/list_of_indicators/gator_oscillator.html")]
	public class GatorOscillator : BaseComplexIndicator
	{
		private readonly Alligator _alligator;

		/// <summary>
		/// Initializes a new instance of the <see cref="GatorOscillator"/>.
		/// </summary>
		public GatorOscillator()
		{
			_alligator = new Alligator();
			AddInner(Histogram1 = new(_alligator.Jaw, _alligator.Lips, false));
			AddInner(Histogram2 = new(_alligator.Lips, _alligator.Teeth, true));
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

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

		/// <summary>
		/// Top histogram.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.UpKey,
			Description = LocalizedStrings.TopHistogramKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public GatorHistogram Histogram1 { get; }

		/// <summary>
		/// Lower histogram.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DownKey,
			Description = LocalizedStrings.LowHistogramKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public GatorHistogram Histogram2 { get; }

		/// <inheritdoc />
		protected override bool CalcIsFormed() => _alligator.IsFormed;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			_alligator.Process(input);

			return base.OnProcess(input);
		}
	}
}
