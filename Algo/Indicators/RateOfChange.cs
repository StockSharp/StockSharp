namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Скорость изменения.
	/// </summary>
	[DisplayName("ROC")]
	[DescriptionLoc(LocalizedStrings.Str732Key)]
	public class RateOfChange : Momentum
	{
		/// <summary>
		/// Создать <see cref="RateOfChange"/>.
		/// </summary>
		public RateOfChange()
		{
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var result = base.OnProcess(input);

			if (Buffer.Count > 0 && Buffer[0] != 0)
				return new DecimalIndicatorValue(this, result.GetValue<decimal>() / Buffer[0] * 100);
			
			return new DecimalIndicatorValue(this);
		}
	}
}