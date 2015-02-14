namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using StockSharp.Localization;

	/// <summary>
	/// Среднее отклонение.
	/// </summary>
	[DisplayName("MeanDeviation")]
	[DescriptionLoc(LocalizedStrings.Str744Key)]
	public class MeanDeviation : LengthIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="MeanDeviation"/>.
		/// </summary>
		public MeanDeviation()
			: base(typeof(decimal))
		{
			Sma = new SimpleMovingAverage();
			Length = 5;
		}

		/// <summary>
		/// Скользящая средняя.
		/// </summary>
		[Browsable(false)]
		public SimpleMovingAverage Sma { get; private set; }

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return Sma.IsFormed; }
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			Sma.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var val = input.GetValue<decimal>();

			if (input.IsFinal)
				Buffer.Add(val);

			var smaValue = Sma.Process(input).GetValue<decimal>();

			if (Buffer.Count > Length)
				Buffer.RemoveAt(0);

			// считаем значение отклонения
			var md = input.IsFinal
				? Buffer.Sum(t => Math.Abs(t - smaValue))
				: Buffer.Skip(IsFormed ? 1 : 0).Sum(t => Math.Abs(t - smaValue)) + Math.Abs(val - smaValue);

			return new DecimalIndicatorValue(this, md / Length);
		}
	}
}