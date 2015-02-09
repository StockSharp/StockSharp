namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using Ecng.Collections;
	using StockSharp.Localization;

	/// <summary>
	/// Взвешанная скользящая средняя.
	/// </summary>
	[DisplayName("WMA")]
	[DescriptionLoc(LocalizedStrings.Str824Key)]
	public class WeightedMovingAverage : LengthIndicator<decimal>
	{
		private decimal _denominator = 1;

		/// <summary>
		/// Создать <see cref="WeightedMovingAverage"/>.
		/// </summary>
		public WeightedMovingAverage()
			: base(typeof(decimal))
		{
			Length = 32;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_denominator = 0;

			for (var i = 1; i <= Length; i++)
				_denominator += i;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

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

			var w = 1;
			return new DecimalIndicatorValue(this, buff.Sum(v => w++ * v) / _denominator);
		}
	}
}