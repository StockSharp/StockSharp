#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: HullMovingAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel.DataAnnotations;

	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Hull Moving Average.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/hma.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.HMAKey,
		Description = LocalizedStrings.HullMovingAverageKey)]
	[Doc("topics/api/indicators/list_of_indicators/hma.html")]
	public class HullMovingAverage : LengthIndicator<decimal>
	{
		private readonly WeightedMovingAverage _wmaSlow = new();
		private readonly WeightedMovingAverage _wmaFast = new();
		private readonly WeightedMovingAverage _wmaResult = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="HullMovingAverage"/>.
		/// </summary>
		public HullMovingAverage()
		{
			Length = 10;
			SqrtPeriod = 0;
		}

		private int _sqrtPeriod;

		/// <summary>
		/// Period of resulting average. If equal to 0, period of resulting average is equal to the square root of HMA period. By default equal to 0.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SqrtKey,
			Description = LocalizedStrings.PeriodResAvgDescKey,
			GroupName = LocalizedStrings.GeneralKey)]
		public int SqrtPeriod
		{
			get => _sqrtPeriod;
			set
			{
				_sqrtPeriod = value;
				_wmaResult.Length = value == 0 ? (int)Math.Sqrt(Length) : value;
			}
		}

		/// <inheritdoc />
		protected override bool CalcIsFormed() => _wmaResult.IsFormed;

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();

			_wmaSlow.Length = Length;
			_wmaFast.Length = Length / 2;
			_wmaResult.Length = SqrtPeriod == 0 ? (int)Math.Sqrt(Length) : SqrtPeriod;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			_wmaSlow.Process(input);
			_wmaFast.Process(input);

			if (_wmaFast.IsFormed && _wmaSlow.IsFormed)
			{
				var diff = 2 * _wmaFast.GetCurrentValue() - _wmaSlow.GetCurrentValue();
				_wmaResult.Process(diff);
			}

			return new DecimalIndicatorValue(this, _wmaResult.GetCurrentValue());
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);
			SqrtPeriod = storage.GetValue<int>(nameof(SqrtPeriod));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);
			storage.SetValue(nameof(SqrtPeriod), SqrtPeriod);
		}
	}
}