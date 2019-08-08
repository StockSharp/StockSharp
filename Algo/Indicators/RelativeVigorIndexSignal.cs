#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: RelativeVigorIndexSignal.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	/// <summary>
	/// The signaling part of indicator <see cref="RelativeVigorIndex"/>.
	/// </summary>
	[Browsable(false)]
	public class RelativeVigorIndexSignal : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RelativeVigorIndexSignal"/>.
		/// </summary>
		public RelativeVigorIndexSignal()
		{
			Length = 4;
		}

		/// <inheritdoc />
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