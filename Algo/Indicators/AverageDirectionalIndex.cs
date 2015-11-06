namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Welles Wilder Average Directional Index.
	/// </summary>
	[DisplayName("ADX")]
	[DescriptionLoc(LocalizedStrings.Str757Key)]
	public class AverageDirectionalIndex : BaseComplexIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AverageDirectionalIndex"/>.
		/// </summary>
		public AverageDirectionalIndex()
			: this(new DirectionalIndex { Length = 14 }, new WilderMovingAverage { Length = 14 })
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AverageDirectionalIndex"/>.
		/// </summary>
		/// <param name="dx">Welles Wilder Directional Movement Index.</param>
		/// <param name="movingAverage">Moving Average.</param>
		public AverageDirectionalIndex(DirectionalIndex dx, LengthIndicator<decimal> movingAverage)
		{
			if (dx == null)
				throw new ArgumentNullException(nameof(dx));

			if (movingAverage == null)
				throw new ArgumentNullException(nameof(movingAverage));

			InnerIndicators.Add(Dx = dx);
			InnerIndicators.Add(MovingAverage = movingAverage);
			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <summary>
		/// Welles Wilder Directional Movement Index.
		/// </summary>
		[Browsable(false)]
		public DirectionalIndex Dx { get; }

		/// <summary>
		/// Moving Average.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> MovingAverage { get; }

		/// <summary>
		/// Period length.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str736Key)]
		[DescriptionLoc(LocalizedStrings.Str737Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public virtual int Length
		{
			get { return MovingAverage.Length; }
			set
			{
				MovingAverage.Length = Dx.Length = value;
				Reset();
			}
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Length = settings.GetValue<int>("Length");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Length", Length);
		}
	}
}