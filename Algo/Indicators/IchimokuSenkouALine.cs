namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	/// <summary>
	/// Senkou Span A line.
	/// </summary>
	public class IchimokuSenkouALine : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IchimokuSenkouALine"/>.
		/// </summary>
		/// <param name="tenkan">Tenkan line.</param>
		/// <param name="kijun">Kijun line.</param>
		public IchimokuSenkouALine(IchimokuLine tenkan, IchimokuLine kijun)
		{
			if (tenkan == null)
				throw new ArgumentNullException("tenkan");

			if (kijun == null)
				throw new ArgumentNullException("kijun");

			Tenkan = tenkan;
			Kijun = kijun;
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed
		{
			get { return Buffer.Count >= Kijun.Length; }
		}

		/// <summary>
		/// Tenkan line.
		/// </summary>
		[Browsable(false)]
		public IchimokuLine Tenkan { get; private set; }

		/// <summary>
		/// Kijun line.
		/// </summary>
		[Browsable(false)]
		public IchimokuLine Kijun { get; private set; }

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			decimal? result = null;

			if (Tenkan.IsFormed && Kijun.IsFormed)
			{
				if (input.IsFinal)
					Buffer.Add((Tenkan.GetCurrentValue() + Kijun.GetCurrentValue()) / 2);

				if (IsFormed)
					result = Buffer[0];

				if (Buffer.Count > Kijun.Length && input.IsFinal)
				{
					Buffer.RemoveAt(0);
				}
			}

			return result == null ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, result.Value);
		}
	}
}