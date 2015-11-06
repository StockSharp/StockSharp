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
			if (ma == null)
				throw new ArgumentNullException(nameof(ma));

			if (dev == null)
				throw new ArgumentNullException(nameof(dev));

			_ma = ma;
			_dev = dev;
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
			Width = settings.GetValue<decimal>("Width");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="settings">Settings storage.</param>
		public override void Save(SettingsStorage settings)
		{
			base.Save(settings);
			settings.SetValue("Width", Width);
		}
	}
}