namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Convergence/divergence of moving averages with signal line.
	/// </summary>
	[DisplayName("MACD Signal")]
	[DescriptionLoc(LocalizedStrings.Str803Key)]
	public class MovingAverageConvergenceDivergenceSignal : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceSignal"/>.
		/// </summary>
		public MovingAverageConvergenceDivergenceSignal()
			: this(new MovingAverageConvergenceDivergence(), new ExponentialMovingAverage { Length = 9 })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MovingAverageConvergenceDivergenceSignal"/>.
		/// </summary>
		/// <param name="macd">Convergence/divergence of moving averages.</param>
		/// <param name="signalMa">Signalling Voving Average.</param>
		public MovingAverageConvergenceDivergenceSignal(MovingAverageConvergenceDivergence macd, ExponentialMovingAverage signalMa)
			: base(macd, signalMa)
		{
			Macd = macd;
			SignalMa = signalMa;
			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <summary>
		/// Convergence/divergence of moving averages.
		/// </summary>
		[ExpandableObject]
		[DisplayName("MACD")]
		[DescriptionLoc(LocalizedStrings.Str797Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public MovingAverageConvergenceDivergence Macd { get; private set; }

		/// <summary>
		/// Signalling Voving Average.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str804Key)]
		[DescriptionLoc(LocalizedStrings.Str805Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage SignalMa { get; private set; }
	}
}