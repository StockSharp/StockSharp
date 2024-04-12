#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: VolumeIndicator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel.DataAnnotations;

	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Candle volume.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/volume.html
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeKey,
		Description = LocalizedStrings.CandleVolumeKey)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[IndicatorOut(typeof(CandleIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/volume.html")]
	public class VolumeIndicator : BaseIndicator
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeIndicator"/>.
		/// </summary>
		public VolumeIndicator()
		{
		}

		/// <inheritdoc />
		public override IndicatorMeasures Measure => IndicatorMeasures.Volume;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return new DecimalIndicatorValue(this, input.GetValue<ICandleMessage>().TotalVolume)
			{
				IsFinal = input.IsFinal,
			};
		}
	}
}
