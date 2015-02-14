namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;
	using System;
	using StockSharp.Localization;

	/// <summary>
	/// Динамическая средняя переменного индекса (Variable Index Dynamic Average).
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/Vidya.ashx
	/// http://www.mql5.com/en/code/75 
	/// </remarks>
	[DisplayName("Vidya")]
	[DescriptionLoc(LocalizedStrings.Str755Key)]
	public class Vidya : LengthIndicator<decimal>
	{
		private decimal _multiplier = 1;
		private decimal _prevFinalValue;

		private readonly ChandeMomentumOscillator _cmo;

		/// <summary>
		/// Создать индикатор<see cref="Vidya"/>.
		/// </summary>
		public Vidya()
		{
			_cmo = new ChandeMomentumOscillator();
			Length = 15;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
			_cmo.Length = Length;
			_multiplier = 2m / (Length + 1);
			_prevFinalValue = 0;

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

			// Вычисляем  СMO
			var cmoValue = _cmo.Process(input).GetValue<decimal>();

			// Вычисляем Vidya
			if (!IsFormed)
			{
				if (!input.IsFinal)
					return new DecimalIndicatorValue(this, ((Buffer.Skip(1).Sum() + newValue) / Length));

				Buffer.Add(newValue);

				_prevFinalValue = Buffer.Sum() / Length;

				return new DecimalIndicatorValue(this, _prevFinalValue);
			}

			var curValue = (newValue - _prevFinalValue) * _multiplier * Math.Abs(cmoValue / 100m) + _prevFinalValue;
				
			if (input.IsFinal)
				_prevFinalValue = curValue;

			return new DecimalIndicatorValue(this, curValue);
		}
	}
}