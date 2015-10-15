namespace StockSharp.Algo.Indicators
{
	/// <summary>
	/// The signaling part of indicator <see cref="RelativeVigorIndex"/>.
	/// </summary>
	public class RelativeVigorIndexSignal : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RelativeVigorIndexSignal"/>.
		/// </summary>
		public RelativeVigorIndexSignal()
		{
			Length = 4;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			if (input.IsFinal)
			{
				Buffer.Add(newValue);

				if (Buffer.Count > Length)
					Buffer.RemoveAt(0);
			}

			if (IsFormed)
			{
				return input.IsFinal
					? new DecimalIndicatorValue(this, (Buffer[0] + 2 * Buffer[1] + 2 * Buffer[2] + Buffer[3]) / 6m)
					: new DecimalIndicatorValue(this, (Buffer[1] + 2 * Buffer[2] + 2 * Buffer[3] + newValue) / 6m);
			}

			return new DecimalIndicatorValue(this);
		}
	}
}