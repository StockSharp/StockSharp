namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Envelope.
	/// </summary>
	[DisplayName("Envelope")]
	[Description("Envelope.")]
	public class Envelope : BaseComplexIndicator
	{
		/// <summary>
		/// Создать <see cref="Envelope"/>.
		/// </summary>
		public Envelope()
			: this(new SimpleMovingAverage())
		{
		}

		/// <summary>
		/// Создать <see cref="Envelope"/>.
		/// </summary>
		public Envelope(LengthIndicator<decimal> ma)
		{
			InnerIndicators.Add(Middle = ma);
			InnerIndicators.Add(Upper = (LengthIndicator<decimal>)ma.Clone());
			InnerIndicators.Add(Lower = (LengthIndicator<decimal>)ma.Clone());

			Upper.Name = "Upper";
			Lower.Name = "Lower";
		}

		/// <summary>
		/// Средняя линия.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> Middle { get; private set; }

		/// <summary>
		/// Верхняя линия.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> Upper { get; private set; }

		/// <summary>
		/// Нижняя линия.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> Lower { get; private set; }

		/// <summary>
		/// Длина периода. По-умолчанию длина равна 1.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str778Key)]
		[DescriptionLoc(LocalizedStrings.Str779Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public virtual int Length
		{
			get { return Middle.Length; }
			set
			{
				Middle.Length = Upper.Length = Lower.Length = value;
				Reset();
			}
		}

		private decimal _shift = 0.25m;

		/// <summary>
		/// Ширина сдвига. Задается в процентах от 0 до 1. По-умолчанию равно 0.25.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str783Key)]
		[DescriptionLoc(LocalizedStrings.Str784Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Shift
		{
			get { return _shift; }
			set
			{
				if (value < 0)
					throw new ArgumentNullException("value");

				_shift = value;
				Reset();
			}
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return Middle.IsFormed; }
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = (ComplexIndicatorValue)base.OnProcess(input);

			var upper = value.InnerValues[Upper];
			value.InnerValues[Upper] = upper.SetValue(this, upper.GetValue<decimal>() * (1 + Shift));

			var lower = value.InnerValues[Lower];
			value.InnerValues[Lower] = lower.SetValue(this, lower.GetValue<decimal>() * (1 - Shift));

			return value;
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Shift = settings.GetValue<decimal>("Shift");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="settings">Хранилище настроек.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Shift", Shift);
		}
	}
}