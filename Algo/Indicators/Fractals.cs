namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Fractals.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/fractals.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FractalsKey,
		Description = LocalizedStrings.FractalsKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/fractals.html")]
	public class Fractals : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Fractals"/>.
		/// </summary>
		public Fractals()
			: this(5, new(true) { Name = nameof(Up) }, new(false) { Name = nameof(Down) })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Fractals"/>.
		/// </summary>
		/// <param name="length">Period length.</param>
		/// <param name="up">Fractal up.</param>
		/// <param name="down">Fractal down.</param>
		public Fractals(int length, FractalPart up, FractalPart down)
			: base(up, down)
		{
			Up = up;
			Down = down;
			Length = length;
		}

		/// <summary>
		/// Period length.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.PeriodKey,
			Description = LocalizedStrings.PeriodLengthKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public int Length
		{
			get => Up.Length;
			set
			{
				Up.Length = Down.Length = value;
				Reset();
			}
		}

		/// <summary>
		/// Fractal up.
		/// </summary>
		//[TypeConverter(typeof(ExpandableObjectConverter))]
		[Browsable(false)]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.UpKey,
			Description = LocalizedStrings.FractalUpKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public FractalPart Up { get; }

		/// <summary>
		/// Fractal down.
		/// </summary>
		//[TypeConverter(typeof(ExpandableObjectConverter))]
		[Browsable(false)]
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DownKey,
			Description = LocalizedStrings.FractalDownKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public FractalPart Down { get; }

		/// <inheritdoc />
		public override string ToString() => base.ToString() + " " + Length;
	}
}