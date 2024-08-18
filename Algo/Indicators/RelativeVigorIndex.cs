namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Relative Vigor Index.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/rvi.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RVIKey,
		Description = LocalizedStrings.RelativeVigorIndexKey)]
	[Doc("topics/api/indicators/list_of_indicators/rvi.html")]
	public class RelativeVigorIndex : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RelativeVigorIndex"/>.
		/// </summary>
		public RelativeVigorIndex()
			: this(new(), new())
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

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

		/// <summary>
		/// Average indicator part.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.AverageKey,
			Description = LocalizedStrings.AveragePartKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public RelativeVigorIndexAverage Average { get; }

		/// <summary>
		/// Signaling part of indicator.
		/// </summary>
		[TypeConverter(typeof(ExpandableObjectConverter))]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SignalKey,
			Description = LocalizedStrings.SignalPartKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public RelativeVigorIndexSignal Signal { get; }

		/// <inheritdoc />
		public override string ToString() => base.ToString() + $" A={Average.Length} S={Signal.Length}";
	}
}
