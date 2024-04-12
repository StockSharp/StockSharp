#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: MeanDeviation.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;

	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Average deviation.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/mean_deviation.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MeanDevKey,
		Description = LocalizedStrings.AverageDeviationKey)]
	[Doc("topics/api/indicators/list_of_indicators/mean_deviation.html")]
	public class MeanDeviation : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MeanDeviation"/>.
		/// </summary>
		public MeanDeviation()
		{
			Sma = new SimpleMovingAverage();
			Length = 5;
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.MinusOnePlusOne;

		/// <summary>
		/// Moving Average.
		/// </summary>
		[Browsable(false)]
		public SimpleMovingAverage Sma { get; }

		/// <inheritdoc />
		protected override bool CalcIsFormed() => Sma.IsFormed;

		/// <inheritdoc />
		public override void Reset()
		{
			Sma.Length = Length;
			base.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var val = input.GetValue<decimal>();

			if (input.IsFinal)
				Buffer.PushBack(val);

			var smaValue = Sma.Process(input).GetValue<decimal>();

			if (Buffer.Count > Length)
				Buffer.PopFront();

			// считаем значение отклонения
			var md = input.IsFinal
				? Buffer.Sum(t => Math.Abs(t - smaValue))
				: Buffer.Skip(IsFormed ? 1 : 0).Sum(t => Math.Abs(t - smaValue)) + Math.Abs(val - smaValue);

			return new DecimalIndicatorValue(this, md / Length);
		}
	}
}