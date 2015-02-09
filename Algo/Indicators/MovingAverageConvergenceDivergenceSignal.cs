namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Схождение/расхождение скользящих средних с сигнальной линией.
	/// </summary>
	[DisplayName("MACD Signal")]
	[DescriptionLoc(LocalizedStrings.Str803Key)]
	public class MovingAverageConvergenceDivergenceSignal : BaseComplexIndicator
	{
		/// <summary>
		/// Создать <see cref="MovingAverageConvergenceDivergenceSignal"/>.
		/// </summary>
		public MovingAverageConvergenceDivergenceSignal()
			: this(new MovingAverageConvergenceDivergence(), new ExponentialMovingAverage { Length = 9 })
		{
		}

		/// <summary>
		/// Создать <see cref="MovingAverageConvergenceDivergenceSignal"/>.
		/// </summary>
		/// <param name="macd">Схождение/расхождение скользящих средних.</param>
		/// <param name="signalMa">Сигнальная скользящая средняя.</param>
		public MovingAverageConvergenceDivergenceSignal(MovingAverageConvergenceDivergence macd, ExponentialMovingAverage signalMa)
			: base(macd, signalMa)
		{
			Macd = macd;
			SignalMa = signalMa;
			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <summary>
		/// Схождение/расхождение скользящих средних.
		/// </summary>
		[ExpandableObject]
		[DisplayName("MACD")]
		[DescriptionLoc(LocalizedStrings.Str797Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public MovingAverageConvergenceDivergence Macd { get; private set; }

		/// <summary>
		/// Сигнальная скользящая средняя.
		/// </summary>
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str804Key)]
		[DescriptionLoc(LocalizedStrings.Str805Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public ExponentialMovingAverage SignalMa { get; private set; }
	}
}