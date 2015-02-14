namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;
	using StockSharp.Localization;

	/// <summary>
	/// Сглаженное скользящее среднее.
	/// </summary>
	[DisplayName("SMMA")]
	[DescriptionLoc(LocalizedStrings.Str819Key)]
	public class SmoothedMovingAverage : LengthIndicator<decimal>
	{
		private decimal _prevFinalValue;

		/// <summary>
		/// Создать <see cref="SmoothedMovingAverage"/>.
		/// </summary>
		public SmoothedMovingAverage()
			: base(typeof(decimal))
		{
			Length = 32;
		}

		/// <summary>
		/// Сбросить состояние индикатора на первоначальное. Метод вызывается каждый раз, когда меняются первоначальные настройки (например, длина периода).
		/// </summary>
		public override void Reset()
		{
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

			if (!IsFormed)
			{
				if (input.IsFinal)
				{
					Buffer.Add(newValue);

					_prevFinalValue = Buffer.Sum() / Length;

					return new DecimalIndicatorValue(this, _prevFinalValue);
				}

				return new DecimalIndicatorValue(this, (Buffer.Skip(1).Sum() + newValue) / Length);
			}

			var curValue = (_prevFinalValue * (Length - 1) + newValue) / Length;

			if (input.IsFinal)
				_prevFinalValue = curValue;

			return new DecimalIndicatorValue(this, curValue);
		}
	}
}