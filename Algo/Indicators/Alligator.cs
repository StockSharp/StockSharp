namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	using StockSharp.Localization;

	/// <summary>
	/// Аллигатор.
	/// </summary>
	/// <remarks>
	/// http://ta.mql4.com/indicators/bills/alligator
	/// </remarks>
	[DisplayName("Alligator")]
	[DescriptionLoc(LocalizedStrings.Str837Key)]
	public class Alligator : BaseComplexIndicator
	{
		/// <summary>
		/// Создать <see cref="Alligator"/>.
		/// </summary>
		public Alligator()
			: this(new AlligatorLine { Length = 13, Shift = 8 }, new AlligatorLine { Length = 8, Shift = 5 }, new AlligatorLine { Length = 5, Shift = 3 })
		{
		}

		/// <summary>
		/// Создать <see cref="Alligator"/>.
		/// </summary>
		/// <param name="jaw">Челюсть.</param>
		/// <param name="teeth">Зубы.</param>
		/// <param name="lips">Губы.</param>
		public Alligator(AlligatorLine jaw, AlligatorLine teeth, AlligatorLine lips)
			: base(jaw, teeth, lips)
		{
			Jaw = jaw;
			Teeth = teeth;
			Lips = lips;
		}

		/// <summary>
		/// Челюсть.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Jaw")]
		[DescriptionLoc(LocalizedStrings.Str838Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public AlligatorLine Jaw { get; private set; }

		/// <summary>
		/// Зубы.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Teeth")]
		[DescriptionLoc(LocalizedStrings.Str839Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public AlligatorLine Teeth { get; private set; }

		/// <summary>
		/// Губы.
		/// </summary>
		[ExpandableObject]
		[DisplayName("Lips")]
		[DescriptionLoc(LocalizedStrings.Str840Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public AlligatorLine Lips { get; private set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return Jaw.IsFormed; } }
	}
}
