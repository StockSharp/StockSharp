namespace StockSharp.Algo.Indicators
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Standard deviation.
	/// </summary>
	[DisplayName("StdDev")]
	[DescriptionLoc(LocalizedStrings.Str820Key)]
	public class StandardDeviation : LengthIndicator<decimal>
	{
		private readonly SimpleMovingAverage _sma;

		/// <summary>
		/// Initializes a new instance of the <see cref="StandardDeviation"/>.
		/// </summary>
		public StandardDeviation()
		{
			_sma = new SimpleMovingAverage();
			Length = 10;
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed { get { return _sma.IsFormed; } }

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_sma.Length = Length;
			base.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
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