#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: IchimokuSenkouALine.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	/// <summary>
	/// Senkou (A) line.
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
			Tenkan = tenkan ?? throw new ArgumentNullException(nameof(tenkan));
			Kijun = kijun ?? throw new ArgumentNullException(nameof(kijun));
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => Buffer.Count >= Kijun.Length;

		/// <summary>
		/// Tenkan line.
		/// </summary>
		[Browsable(false)]
		public IchimokuLine Tenkan { get; }

		/// <summary>
		/// Kijun line.
		/// </summary>
		[Browsable(false)]
		public IchimokuLine Kijun { get; }

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