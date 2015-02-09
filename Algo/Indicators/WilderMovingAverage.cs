namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using Ecng.Collections;
	using StockSharp.Localization;

	/// <summary>
	/// Скользящая средняя Welles Wilder.
	/// </summary>
	[DisplayName("WilderMA")]
	[DescriptionLoc(LocalizedStrings.Str825Key)]
	public class WilderMovingAverage : LengthIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="WilderMovingAverage"/>.
		/// </summary>
		public WilderMovingAverage()
			: base(typeof(decimal))
		{
			Length = 32;
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

			return new DecimalIndicatorValue(this, (this.GetCurrentValue() * (buff.Count - 1) + newValue) / buff.Count);
		}
	}
}