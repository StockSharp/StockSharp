namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Gator осцилятор.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/gator
	/// </remarks>
	[DisplayName("Gator")]
	[DescriptionLoc(LocalizedStrings.Str850Key)]
	public class GatorOscillator : BaseComplexIndicator
	{
		private readonly Alligator _alligator;

		/// <summary>
		/// Создать <see cref="GatorOscillator"/>.
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
		/// Создать <see cref="GatorOscillator"/>.
		/// </summary>
		/// <param name="alligator">Аллигатор.</param>
		/// <param name="histogram1">Верхняя гистограмма.</param>
		/// <param name="histogram2">Нижняя гистограмма.</param>
		public GatorOscillator(Alligator alligator, GatorHistogram histogram1, GatorHistogram histogram2)
			: base(histogram1, histogram2)
		{
			if (alligator == null)
				throw new ArgumentNullException("alligator");

			_alligator = alligator;
			Histogram1 = histogram1;
			Histogram2 = histogram2;
		}

		/// <summary>
		/// Верхняя гистограмма.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Histogram1")]
		[DescriptionLoc(LocalizedStrings.Str851Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public GatorHistogram Histogram1 { get; private set; }

		/// <summary>
		/// Нижняя гистограмма.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Histogram2")]
		[DescriptionLoc(LocalizedStrings.Str852Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public GatorHistogram Histogram2 { get; private set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get
			{
				return _alligator.IsFormed;
			}
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			_alligator.Process(input);

			return base.OnProcess(input);
		}
	}
}