namespace StockSharp.Algo.Indicators
{
	using Ecng.Common;

	/// <summary>
	/// Часть <see cref="Fractals"/>.
	/// </summary>
	public class FractalPart : BaseIndicator
	{
		/// <summary>
		/// Создать <see cref="FractalPart"/>.
		/// </summary>
		public FractalPart()
		{
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return input.To<ShiftedIndicatorValue>();
		}
	}
}