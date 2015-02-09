namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Стандартное отклонение.
	/// </summary>
	[DisplayName("StdDev")]
	[DescriptionLoc(LocalizedStrings.Str820Key)]
	public class StandardDeviation : LengthIndicator<decimal>
	{
		private readonly SimpleMovingAverage _sma;

		/// <summary>
		/// Создать <see cref="StandardDeviation"/>.
		/// </summary>
		public StandardDeviation()
			: base(typeof(decimal))
		{
			_sma = new SimpleMovingAverage();
			Length = 10;
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed { get { return _sma.IsFormed; } }

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_sma.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();
			var smaValue = _sma.Process(input).GetValue<decimal>();

			if (input.IsFinal)
			{
				Buffer.Add(newValue);

				if (Buffer.Count > Length)
					Buffer.RemoveAt(0);
			}

			var buff = Buffer;
			if (!input.IsFinal)
			{
				buff = new List<decimal>();
				buff.AddRange(Buffer.Skip(1));
				buff.Add(newValue);
			}

			//считаем значение отклонения в последней точке
			var std = buff.Select(t1 => t1 - smaValue).Select(t => t * t).Sum();

			return new DecimalIndicatorValue(this, (decimal)Math.Sqrt((double)(std / Length)));
		}
	}
}