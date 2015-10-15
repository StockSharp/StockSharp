namespace StockSharp.Algo.Indicators
{
	using Ecng.Common;

	/// <summary>
	/// Part <see cref="Fractals"/>.
	/// </summary>
	public class FractalPart : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="FractalPart"/>.
		/// </summary>
		public FractalPart()
		{
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return input.To<ShiftedIndicatorValue>();
		}
	}
}