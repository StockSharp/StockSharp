namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Полосы Боллинджера.
	/// </summary>
	[DisplayName("Bollinger")]
	[DescriptionLoc(LocalizedStrings.Str777Key)]
	public class BollingerBands : BaseComplexIndicator
	{
		private readonly StandardDeviation _dev = new StandardDeviation();

		/// <summary>
		/// Создать <see cref="BollingerBands"/>.
		/// </summary>
		public BollingerBands()
			: this(new SimpleMovingAverage())
		{
		}

		/// <summary>
		/// Создать <see cref="BollingerBands"/>.
		/// </summary>
		/// <param name="ma">Скользящая средняя.</param>
		public BollingerBands(LengthIndicator<decimal> ma)
		{
			InnerIndicators.Add(MovingAverage = ma);
			InnerIndicators.Add(UpBand = new BollingerBand(MovingAverage, _dev) { Name = "UpBand" });
			InnerIndicators.Add(LowBand = new BollingerBand(MovingAverage, _dev) { Name = "LowBand" });
			Width = 2;
		}

		/// <summary>
		/// Средняя линия.
		/// </summary>
		[Browsable(false)]
		public LengthIndicator<decimal> MovingAverage { get; private set; }

		/// <summary>
		/// Верхняя полоса+.
		/// </summary>
		[Browsable(false)]
		public BollingerBand UpBand { get; private set; }

		/// <summary>
		/// Нижняя полоса-.
		/// </summary>
		[Browsable(false)]
		public BollingerBand LowBand { get; private set; }

		/// <summary>
		/// Длина периода. По-умолчанию длина равна 1.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str778Key)]
		[DescriptionLoc(LocalizedStrings.Str779Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public virtual int Length
		{
			get { return MovingAverage.Length; }
			set
			{
				_dev.Length = MovingAverage.Length = value;
				Reset();
			}
		}

		/// <summary>
		/// Ширина канала Полос Боллинджера. Значение по умолчанию равно 2.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str780Key)]
		[DescriptionLoc(LocalizedStrings.Str781Key)]
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		public decimal Width
		{
			get { return UpBand.Width; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str782);

				UpBand.Width = value;
				LowBand.Width = -value;
 
				Reset();
			}
		}
		
		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_dev.Reset();
			//MovingAverage.Reset();
			//UpBand.Reset();
			//LowBand.Reset();
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return MovingAverage.IsFormed; }
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			_dev.Process(input);
			var maValue = MovingAverage.Process(input);
			var value = new ComplexIndicatorValue(this);
			value.InnerValues.Add(MovingAverage, maValue);
			value.InnerValues.Add(UpBand, UpBand.Process(input));
			value.InnerValues.Add(LowBand, LowBand.Process(input));
			return value;
		}
	}
}