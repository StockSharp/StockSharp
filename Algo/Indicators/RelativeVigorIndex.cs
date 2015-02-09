namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Индекс Относительной Энергии.
	/// </summary>
	[DisplayName("RVI")]
	[DescriptionLoc(LocalizedStrings.Str771Key)]
	public class RelativeVigorIndex : BaseComplexIndicator
	{
		/// <summary>
		/// Создать <see cref="RelativeVigorIndex"/>.
		/// </summary>
		public RelativeVigorIndex()
			: this(new RelativeVigorIndexAverage(), new RelativeVigorIndexSignal())
		{
		}

		/// <summary>
		/// Создать <see cref="RelativeVigorIndex"/>.
		/// </summary>
		/// <param name="average">Средневзвешанная часть индикатора.</param>
		/// <param name="signal">Сигнальная часть индикатора.</param>
		public RelativeVigorIndex(RelativeVigorIndexAverage average, RelativeVigorIndexSignal signal)
			: base(average, signal)
		{
			Average = average;
			Signal = signal;

			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <summary>
		/// Средневзвешанная часть индикатора.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Average")]
		[DescriptionLoc(LocalizedStrings.Str772Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RelativeVigorIndexAverage Average { get; private set; }

		/// <summary>
		/// Сигнальная часть индикатора.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Signal")]
		[DescriptionLoc(LocalizedStrings.Str773Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public RelativeVigorIndexSignal Signal { get; private set; }
	}
}