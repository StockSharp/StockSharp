namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Индекс среднего направления движения Welles Wilder.
	/// </summary>
	[DisplayName("ADX")]
	[DescriptionLoc(LocalizedStrings.Str757Key)]
	public class AverageDirectionalIndex : BaseComplexIndicator
	{
		/// <summary>
		/// Создать <see cref="AverageDirectionalIndex"/>.
		/// </summary>
		public AverageDirectionalIndex()
			: this(new DirectionalIndex { Length = 14 }, new WilderMovingAverage { Length = 14 })
		{
		}

		/// <summary>
		/// Создать <see cref="AverageDirectionalIndex"/>.
		/// </summary>
		/// <param name="dx">Индекса направленного движения Welles Wilder.</param>
		/// <param name="movingAverage">Скользящая средняя.</param>
		public AverageDirectionalIndex(DirectionalIndex dx, LengthIndicator<decimal> movingAverage)
		{
			if (dx == null)
				throw new ArgumentNullException("dx");

			if (movingAverage == null)
				throw new ArgumentNullException("movingAverage");

			InnerIndicators.Add(Dx = dx);
			InnerIndicators.Add(MovingAverage = movingAverage);
			Mode = ComplexIndicatorModes.Sequence;
		}

		/// <summary>
		/// Индекса направленного движения Welles Wilder.
		/// </summary>
		[Browsable(false)]
		public DirectionalIndex Dx { get; private set; }

		/// <summary>
		/// Скользящая средняя.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> MovingAverage { get; private set; }

		/// <summary>
		/// Длина периода.
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
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Length = settings.GetValue<int>("Length");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Length", Length);
		}
	}
}