namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Стохастический Осциллятор.
	/// </summary>
	[DisplayName("Stochastic Oscillator")]
	[Description("Stochastic Oscillator")]
	public class StochasticOscillator : BaseComplexIndicator
	{
		/// <summary>
		/// Создать <see cref="StochasticOscillator"/>.
		/// </summary>
		public StochasticOscillator()
		{
			InnerIndicators.Add(K = new StochasticK());
			InnerIndicators.Add(D = new SimpleMovingAverage { Length = 3 });

			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <summary>
		/// %K.
		/// </summary>
		[ExpandableObject]
		[DisplayName("%K")]
		[Description("%K")]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public StochasticK K { get; private set; }

		/// <summary>
		/// %D.
		/// </summary>
		[ExpandableObject]
		[DisplayName("%D")]
		[Description("%D")]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public SimpleMovingAverage D { get; private set; }
	}
}