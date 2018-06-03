#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: BollingerBand.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Serialization;

	/// <summary>
	/// Bollinger band.
	/// </summary>
	public class BollingerBand : BaseIndicator
	{
		private readonly LengthIndicator<decimal> _ma;
		private readonly StandardDeviation _dev;

		/// <summary>
		/// Initializes a new instance of the <see cref="BollingerBand"/>.
		/// </summary>
		/// <param name="ma">Moving Average.</param>
		/// <param name="dev">Standard deviation.</param>
		public BollingerBand(LengthIndicator<decimal> ma, StandardDeviation dev)
		{
			_ma = ma ?? throw new ArgumentNullException(nameof(ma));
			_dev = dev ?? throw new ArgumentNullException(nameof(dev));
		}

		/// <summary>
		/// Channel width.
		/// </summary>
		public decimal Width { get; set; }

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			return new DecimalIndicatorValue(this, _ma.GetCurrentValue() + (Width * _dev.GetCurrentValue()));
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Load(SettingsStorage settings)
		{
			base.Load(settings);
			Width = settings.GetValue<decimal>(nameof(Width));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue(nameof(Width), Width);
		}
	}
}