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

			Reset();
		}

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

		/// <inheritdoc />
		public override int Length
		{
			get => Kijun?.Length ?? 1;
			set => Kijun.Length = value;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			decimal? result = null;

			if (Tenkan.IsFormed && Kijun.IsFormed)
			{
				if (IsFormed || (input.IsFinal && Buffer.Count == (Length - 1)))
					result = Buffer[0];

				if (input.IsFinal)
					Buffer.PushBack((Tenkan.GetCurrentValue() + Kijun.GetCurrentValue()) / 2);
			}

			return result == null ? new DecimalIndicatorValue(this) : new DecimalIndicatorValue(this, result.Value);
		}
	}
}